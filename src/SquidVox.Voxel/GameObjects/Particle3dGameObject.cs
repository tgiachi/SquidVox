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
/// 3D particle system for rendering and managing particle effects.
/// </summary>
public class Particle3dGameObject : Base3dGameObject, IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly ParticlePool _particlePool = new();
    private readonly Random _sharedRandom = new();
    private BasicEffect _effect;
    private VertexBuffer _vertexBuffer;
    private IndexBuffer _indexBuffer;
    private Texture2D _particleTexture;
    private bool _isDisposed;

    private const int MaxParticles = 1000;

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
        LoadDefaultTexture();

        var viewport = _graphicsDevice.Viewport;
        Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, viewport.AspectRatio, 0.1f, 1000f);
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
    /// Sets the particle texture using the global asset manager.
    /// </summary>
    /// <param name="textureName">Texture asset name.</param>
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

            _particleTexture = texture;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to set particle texture '{TextureName}'.", textureName);
        }
    }

    private void CreateGeometry()
    {
        var vertices = new VertexPositionColorTexture[]
        {
            new(new Vector3(-0.5f, -0.5f, 0), Color.White, new Vector2(0, 1)),
            new(new Vector3(0.5f, -0.5f, 0), Color.White, new Vector2(1, 1)),
            new(new Vector3(0.5f, 0.5f, 0), Color.White, new Vector2(1, 0)),
            new(new Vector3(-0.5f, 0.5f, 0), Color.White, new Vector2(0, 0))
        };

        var indices = new short[] { 0, 1, 2, 2, 3, 0 };

        _vertexBuffer = new VertexBuffer(_graphicsDevice, typeof(VertexPositionColorTexture), vertices.Length, BufferUsage.WriteOnly);
        _vertexBuffer.SetData(vertices);

        _indexBuffer = new IndexBuffer(_graphicsDevice, IndexElementSize.SixteenBits, indices.Length, BufferUsage.WriteOnly);
        _indexBuffer.SetData(indices);
    }

    private void LoadDefaultTexture()
    {
        _particleTexture = SquidVoxEngineContext.WhitePixel;
    }

    protected float NextFloat(float min, float max)
    {
        return (float)(_sharedRandom.NextDouble() * (max - min) + min);
    }

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
    /// Spawns particles at the specified position.
    /// </summary>
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

            if (!TrySpawnParticle(offsetPos, velocity, lifeTime, 0.15f, c, rotationSpeed, Particle.DefaultGravity))
            {
                break;
            }
        }
    }

    /// <inheritdoc />
    protected override void OnUpdate(GameTime gameTime)
    {
        var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _particlePool.Update(deltaTime);
    }

    /// <inheritdoc />
    protected override void OnDraw3d(GameTime gameTime)
    {
        var activeParticles = _particlePool.GetActiveParticles();
        var particleCount = activeParticles.Count();
        if (particleCount == 0)
        {
            return;
        }

        Log.Verbose("Drawing {Count} particles", particleCount);

        var totalVertices = particleCount * 4;
        var totalIndices = particleCount * 6;

        var vertices = new VertexPositionColorTexture[totalVertices];
        var indices = new short[totalIndices];

        int vertexIndex = 0;
        int indexIndex = 0;

        foreach (var particle in activeParticles)
        {
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
                    });
            }

            var baseVertexIndex = (short)vertexIndex;
            indices[indexIndex + 0] = (short)(baseVertexIndex + 0);
            indices[indexIndex + 1] = (short)(baseVertexIndex + 1);
            indices[indexIndex + 2] = (short)(baseVertexIndex + 2);
            indices[indexIndex + 3] = (short)(baseVertexIndex + 2);
            indices[indexIndex + 4] = (short)(baseVertexIndex + 3);
            indices[indexIndex + 5] = (short)(baseVertexIndex + 0);

            vertexIndex += 4;
            indexIndex += 6;
        }

        using var dynamicVertexBuffer = new DynamicVertexBuffer(_graphicsDevice, typeof(VertexPositionColorTexture), totalVertices, BufferUsage.WriteOnly);
        using var dynamicIndexBuffer = new DynamicIndexBuffer(_graphicsDevice, IndexElementSize.SixteenBits, totalIndices, BufferUsage.WriteOnly);

        dynamicVertexBuffer.SetData(vertices);
        dynamicIndexBuffer.SetData(indices);

        _effect.World = Matrix.Identity;
        _effect.View = View;
        _effect.Projection = Projection;
        _effect.Texture = _particleTexture;

        var previousBlend = _graphicsDevice.BlendState;
        var previousDepth = _graphicsDevice.DepthStencilState;

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
        _graphicsDevice.BlendState = previousBlend;
        _graphicsDevice.DepthStencilState = previousDepth;
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
