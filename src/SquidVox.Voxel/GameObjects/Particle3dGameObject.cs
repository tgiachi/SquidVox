using System;
using System.Linq;
using DryIoc;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using SquidVox.Core.Context;
using SquidVox.Core.GameObjects;
using SquidVox.Core.Interfaces.Services;

namespace SquidVox.Voxel.GameObjects;

/// <summary>
/// Represents a single particle in the 3D particle system.
/// </summary>
public class Particle
{
    public Vector3 Position;
    public Vector3 Velocity;
    public float LifeTime;
    public float MaxLifeTime;
    public Color Color;
    public float Size;
    public bool IsActive;
    public Vector3 Rotation;
    public Vector3 RotationSpeed;
    public const float DefaultGravity = 15f;

    /// <summary>
    /// Updates the particle's position, rotation, and lifetime.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last update.</param>
    public void Update(float deltaTime)
    {
        if (!IsActive) return;

        Velocity.Y -= GravityScale * deltaTime;
        Position += Velocity * deltaTime;
        Rotation += RotationSpeed * deltaTime;
        LifeTime -= deltaTime;

        if (LifeTime <= 0)
        {
            IsActive = false;
        }
    }

    /// <summary>
    /// Gets the alpha value based on remaining lifetime.
    /// </summary>
    /// <returns>Alpha value from 0 to 1.</returns>
    public float GetAlpha()
    {
        return MathHelper.Clamp(LifeTime / MaxLifeTime, 0f, 1f);
    }

    /// <summary>
    /// Resets the particle with new values.
    /// </summary>
    /// <param name="position">Starting position.</param>
    /// <param name="velocity">Initial velocity.</param>
    /// <param name="lifeTime">Lifetime in seconds.</param>
    /// <param name="color">Particle color.</param>
    /// <param name="size">Particle size.</param>
    /// <param name="rotationSpeed">Rotation speed.</param>
    public float GravityScale { get; private set; } = DefaultGravity;

    public void Reset(Vector3 position, Vector3 velocity, float lifeTime, Color color, float size, Vector3 rotationSpeed, float gravityScale = DefaultGravity)
    {
        Position = position;
        Velocity = velocity;
        LifeTime = lifeTime;
        MaxLifeTime = lifeTime;
        Color = color;
        Size = size;
        IsActive = true;
        Rotation = Vector3.Zero;
        RotationSpeed = rotationSpeed;
        GravityScale = gravityScale;
    }
}

/// <summary>
/// Object pool for managing particle instances efficiently.
/// </summary>
public class ParticlePool
{
    private readonly Stack<Particle> _pool = new();
    private readonly List<Particle> _activeParticles = new();

    /// <summary>
    /// Gets the total pool size.
    /// </summary>
    public int PoolSize { get; private set; }

    /// <summary>
    /// Initializes the particle pool with the specified size.
    /// </summary>
    /// <param name="size">Number of particles to pre-allocate.</param>
    public void Initialize(int size)
    {
        PoolSize = size;
        for (int i = 0; i < size; i++)
        {
            _pool.Push(new Particle());
        }
    }

    /// <summary>
    /// Gets an available particle from the pool.
    /// </summary>
    /// <returns>A particle instance or null if pool is exhausted.</returns>
    public Particle? GetParticle()
    {
        if (_pool.Count > 0)
        {
            var particle = _pool.Pop();
            _activeParticles.Add(particle);
            return particle;
        }
        return null;
    }

    /// <summary>
    /// Returns a particle to the pool.
    /// </summary>
    /// <param name="particle">The particle to return.</param>
    public void ReturnParticle(Particle particle)
    {
        particle.IsActive = false;
        _activeParticles.Remove(particle);
        _pool.Push(particle);
    }

    /// <summary>
    /// Gets all currently active particles.
    /// </summary>
    /// <returns>Enumerable of active particles.</returns>
    public IEnumerable<Particle> GetActiveParticles() => _activeParticles;

    /// <summary>
    /// Updates all active particles.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last update.</param>
    public void Update(float deltaTime)
    {
        for (int i = _activeParticles.Count - 1; i >= 0; i--)
        {
            var particle = _activeParticles[i];
            particle.Update(deltaTime);
            if (!particle.IsActive)
            {
                ReturnParticle(particle);
            }
        }
    }

    /// <summary>
    /// Clears all active particles and returns them to the pool.
    /// </summary>
    public void Clear()
    {
        foreach (var particle in _activeParticles)
        {
            particle.IsActive = false;
            _pool.Push(particle);
        }
        _activeParticles.Clear();
    }
}

/// <summary>
/// 3D particle system for rendering and managing particle effects.
/// </summary>
public class Particle3dGameObject : Base3dGameObject, IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly ParticlePool _particlePool = new();
    private readonly Random _random = new();
    private BasicEffect _effect;
    private VertexBuffer _vertexBuffer;
    private IndexBuffer _indexBuffer;
    private Texture2D _particleTexture;
    private bool _isDisposed;

    private const int MaxParticles = 1000;

    protected float NextFloat(float min, float max)
    {
        return (float)(_random.NextDouble() * (max - min) + min);
    }

    protected ParticlePool ParticlePool => _particlePool;

    protected bool TrySpawnParticle(
        Vector3 position,
        Vector3 velocity,
        float lifeTime,
        float size,
        Color color,
        Vector3 rotationSpeed,
        float gravityScale)
    {
        var particle = _particlePool.GetParticle();
        if (particle == null)
        {
            return false;
        }

        particle.Reset(position, velocity, lifeTime, color, size, rotationSpeed, gravityScale);
        return true;
    }

    /// <summary>
    /// Gets or sets the view matrix for rendering.
    /// </summary>
    public Matrix View { get; set; } = Matrix.CreateLookAt(new Vector3(0, 0, 10), Vector3.Zero, Vector3.Up);

    /// <summary>
    /// Gets or sets the projection matrix for rendering.
    /// </summary>
    public Matrix Projection { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Particle3dGameObject"/> class.
    /// </summary>
    public Particle3dGameObject()
    {
        _graphicsDevice = SquidVoxEngineContext.GraphicsDevice;
        _effect = new BasicEffect(_graphicsDevice)
        {
            TextureEnabled = true,
            LightingEnabled = false,
            VertexColorEnabled = true
        };

        _particlePool.Initialize(MaxParticles);
        CreateGeometry();
        LoadTexture();

        // Default projection
        var viewport = _graphicsDevice.Viewport;
        Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, viewport.AspectRatio, 0.1f, 1000f);
    }

    private void CreateGeometry()
    {
        // Create a quad for particles
        var vertices = new VertexPositionColorTexture[]
        {
            new(new Vector3(-0.5f, -0.5f, 0), Color.White, new Vector2(0, 1)),
            new(new Vector3(0.5f, -0.5f, 0), Color.White, new Vector2(1, 1)),
            new(new Vector3(0.5f, 0.5f, 0), Color.White, new Vector2(0, 0)),
            new(new Vector3(-0.5f, 0.5f, 0), Color.White, new Vector2(1, 0))
        };

        var indices = new short[] { 0, 1, 2, 2, 3, 0 };

        _vertexBuffer = new VertexBuffer(_graphicsDevice, typeof(VertexPositionColorTexture), vertices.Length, BufferUsage.WriteOnly);
        _vertexBuffer.SetData(vertices);

        _indexBuffer = new IndexBuffer(_graphicsDevice, IndexElementSize.SixteenBits, indices.Length, BufferUsage.WriteOnly);
        _indexBuffer.SetData(indices);
    }

    private void LoadTexture()
    {
        _particleTexture = new Texture2D(_graphicsDevice, 1, 1);
        _particleTexture.SetData(new[] { Color.White });
    }

    /// <summary>
    /// Sets the particle texture using the asset manager.
    /// </summary>
    /// <param name="textureName">The texture asset name.</param>
    public void SetTexture(string textureName)
    {
        if (string.IsNullOrWhiteSpace(textureName))
        {
            return;
        }

        try
        {
            var assetManager = SquidVoxEngineContext.Container.Resolve<IAssetManagerService>();
            var texture = assetManager.GetTexture(textureName);
            if (texture == null)
            {
                Log.Warning("Particle texture '{TextureName}' not found. Keeping current texture.", textureName);
                return;
            }

            _particleTexture = texture;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to set particle texture '{TextureName}'.", textureName);
        }
    }

    /// <summary>
    /// Spawns particles at the specified position.
    /// </summary>
    /// <param name="position">World position to spawn particles.</param>
    /// <param name="count">Number of particles to spawn.</param>
    /// <param name="spread">Spread radius of particles.</param>
    /// <param name="speed">Initial speed multiplier.</param>
    /// <param name="lifeTime">Lifetime in seconds.</param>
    /// <param name="color">Particle color.</param>
    public void SpawnParticles(Vector3 position, int count, float spread = 1f, float speed = 5f, float lifeTime = 2f, Color? color = null)
    {
        var c = color ?? Color.Yellow;
        for (int i = 0; i < count; i++)
        {
            var offsetPos = position + new Vector3(
                NextFloat(-0.5f, 0.5f) * 0.8f,
                NextFloat(-0.5f, 0.5f) * 0.8f,
                NextFloat(-0.5f, 0.5f) * 0.8f
            );

            var velocity = new Vector3(
                NextFloat(-0.5f, 0.5f) * spread,
                (NextFloat(0f, 0.5f) + 0.3f) * spread,
                NextFloat(-0.5f, 0.5f) * spread
            ) * speed;

            var rotationSpeed = new Vector3(
                NextFloat(-0.5f, 0.5f) * 10f,
                NextFloat(-0.5f, 0.5f) * 10f,
                NextFloat(-0.5f, 0.5f) * 10f
            );

            TrySpawnParticle(offsetPos, velocity, lifeTime, 0.15f, c, rotationSpeed, Particle.DefaultGravity);
        }
    }

    /// <summary>
    /// Updates all active particles.
    /// </summary>
    /// <param name="gameTime">Game timing information.</param>
    protected override void OnUpdate(GameTime gameTime)
    {
        var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _particlePool.Update(deltaTime);
    }

    /// <summary>
    /// Renders all active particles.
    /// </summary>
    /// <param name="gameTime">Game timing information.</param>
    protected override void OnDraw3d(GameTime gameTime)
    {
        var activeParticles = _particlePool.GetActiveParticles();
        var particleCount = activeParticles.Count();
        if (particleCount == 0) return;

        Log.Verbose("Drawing {Count} particles", particleCount);

        // Create dynamic vertex buffer for all particles
        var totalVertices = particleCount * 4; // 4 vertices per quad
        var totalIndices = particleCount * 6; // 6 indices per quad

        var vertices = new VertexPositionColorTexture[totalVertices];
        var indices = new short[totalIndices];

        int vertexIndex = 0;
        int indexIndex = 0;
        int particleIndex = 0;

        foreach (var particle in activeParticles)
        {
            // Base quad vertices (will be transformed)
            var baseVertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0),
                new Vector3(0.5f, -0.5f, 0),
                new Vector3(0.5f, 0.5f, 0),
                new Vector3(-0.5f, 0.5f, 0)
            };

            var rotation = Matrix.CreateFromYawPitchRoll(particle.Rotation.Y, particle.Rotation.X, particle.Rotation.Z);
            var world = Matrix.CreateScale(particle.Size) * rotation * Matrix.CreateTranslation(particle.Position);

            var alpha = particle.GetAlpha();
            var colorWithAlpha = particle.Color * alpha;

            for (int i = 0; i < 4; i++)
            {
                var transformedPos = Vector3.Transform(baseVertices[i], world);
                vertices[vertexIndex + i] = new VertexPositionColorTexture(
                    transformedPos,
                    colorWithAlpha,
                    i switch
                    {
                        0 => new Vector2(0, 1),
                        1 => new Vector2(1, 1),
                        2 => new Vector2(1, 0),
                        3 => new Vector2(0, 0),
                        _ => Vector2.Zero
                    }
                );
            }

            // Indices for this quad
            var baseVertexIndex = (short)(vertexIndex);
            indices[indexIndex + 0] = (short)(baseVertexIndex + 0);
            indices[indexIndex + 1] = (short)(baseVertexIndex + 1);
            indices[indexIndex + 2] = (short)(baseVertexIndex + 2);
            indices[indexIndex + 3] = (short)(baseVertexIndex + 2);
            indices[indexIndex + 4] = (short)(baseVertexIndex + 3);
            indices[indexIndex + 5] = (short)(baseVertexIndex + 0);

            vertexIndex += 4;
            indexIndex += 6;
            particleIndex++;
        }

        // Create dynamic buffers
        var dynamicVertexBuffer = new DynamicVertexBuffer(_graphicsDevice, typeof(VertexPositionColorTexture), totalVertices, BufferUsage.WriteOnly);
        var dynamicIndexBuffer = new DynamicIndexBuffer(_graphicsDevice, IndexElementSize.SixteenBits, totalIndices, BufferUsage.WriteOnly);

        dynamicVertexBuffer.SetData(vertices);
        dynamicIndexBuffer.SetData(indices);

        _effect.View = View;
        _effect.Projection = Projection;
        _effect.Texture = _particleTexture;
        _effect.World = Matrix.Identity;

        var previousBlendState = _graphicsDevice.BlendState;
        var previousDepthStencilState = _graphicsDevice.DepthStencilState;

        _graphicsDevice.BlendState = BlendState.AlphaBlend;
        _graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;

        _graphicsDevice.SetVertexBuffer(dynamicVertexBuffer);
        _graphicsDevice.Indices = dynamicIndexBuffer;

        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, particleCount * 2);
        }

        _graphicsDevice.SetVertexBuffer(null);
        _graphicsDevice.Indices = null;

        _graphicsDevice.BlendState = previousBlendState;
        _graphicsDevice.DepthStencilState = previousDepthStencilState;

        dynamicVertexBuffer.Dispose();
        dynamicIndexBuffer.Dispose();
    }

    /// <summary>
    /// Disposes particle system resources.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
        _particleTexture?.Dispose();
        _effect?.Dispose();
        _particlePool.Clear();
    }
}
