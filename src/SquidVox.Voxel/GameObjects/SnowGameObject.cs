using System;
using DryIoc;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using SquidVox.Core.Context;
using SquidVox.Core.GameObjects;
using SquidVox.Core.Interfaces.Services;

namespace SquidVox.Voxel.GameObjects;

/// <summary>
/// Renders volumetric snow flakes with configurable intensity and wind drift.
/// </summary>
public class SnowGameObject : Base3dGameObject, IDisposable
{
    private const int MaxFlakes = 2048;

    private readonly ILogger _logger = Log.ForContext<SnowGameObject>();
    private readonly GraphicsDevice _graphicsDevice;
    private readonly Effect _snowEffect;
    private readonly CameraGameObject _camera;
    private readonly Random _random = new();
    private readonly SnowFlake[] _flakes = new SnowFlake[MaxFlakes];
    private readonly SnowVertex[] _vertices = new SnowVertex[MaxFlakes * 4];
    private readonly short[] _indices = new short[MaxFlakes * 6];
    private VertexBuffer? _vertexBuffer;
    private IndexBuffer? _indexBuffer;
    private int _activeFlakeCount;
    private float _snowIntensity = 0.2f;
    private float _animationTime;
    private Vector3 _areaSize = new(140f, 90f, 140f);
    private float _minSpeed = 3f;
    private float _maxSpeed = 9f;
    private float _minSize = 0.35f;
    private float _maxSize = 0.9f;
    private Vector3 _windDirection = new(0.4f, 0f, 0.2f);

    /// <summary>
    /// Initializes a new instance of the SnowGameObject class.
    /// </summary>
    /// <param name="camera">The active camera controlling view and projection matrices.</param>
    public SnowGameObject(CameraGameObject camera)
    {
        _camera = camera ?? throw new ArgumentNullException(nameof(camera));
        _graphicsDevice = SquidVoxEngineContext.GraphicsDevice;

        var assetManager = SquidVoxEngineContext.Container.Resolve<IAssetManagerService>();
        _snowEffect = assetManager.GetEffect("Effects/Snow") ?? throw new InvalidOperationException("Snow effect not loaded.");

        InitBuffers();
        UpdateFlakeDensity();

        Name = "Snow";
        _logger.Information("SnowGameObject initialized with {FlakeCount} flakes.", _activeFlakeCount);
    }

    /// <summary>
    /// Gets or sets the snow intensity from 0.0 (clear) to 1.0 (blizzard).
    /// </summary>
    public float SnowIntensity
    {
        get => _snowIntensity;
        set
        {
            var clamped = MathHelper.Clamp(value, 0f, 1f);
            if (Math.Abs(_snowIntensity - clamped) < 0.0001f)
            {
                return;
            }

            _snowIntensity = clamped;
            UpdateFlakeDensity();
        }
    }

    /// <summary>
    /// Gets or sets the dimensions of the snow simulation volume.
    /// </summary>
    public Vector3 AreaSize
    {
        get => _areaSize;
        set
        {
            _areaSize = new Vector3(
                MathHelper.Max(10f, value.X),
                MathHelper.Max(10f, value.Y),
                MathHelper.Max(10f, value.Z)
            );

            ReinitializeFlakes();
        }
    }

    /// <summary>
    /// Gets or sets the wind direction applied to flakes per second.
    /// </summary>
    public Vector3 WindDirection
    {
        get => _windDirection;
        set => _windDirection = value;
    }

    /// <summary>
    /// Releases allocated GPU resources.
    /// </summary>
    public void Dispose()
    {
        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
        GC.SuppressFinalize(this);
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        var delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _animationTime += delta;

        if (_activeFlakeCount == 0)
        {
            return;
        }

        for (int i = 0; i < _activeFlakeCount; i++)
        {
            ref var flake = ref _flakes[i];

            var drift = _windDirection * delta;
            float flutter = MathF.Sin(_animationTime * 1.1f + flake.Position.X * 0.3f + flake.Position.Z * 0.2f);
            var sway = new Vector3(flutter * 0.6f, 0f, flutter * 0.4f) * delta;

            flake.Position += flake.Velocity * delta + drift + sway;

            if (flake.Position.Y < 0f)
            {
                RespawnFlake(ref flake);
            }
        }
    }

    protected override void OnDraw3d(GameTime gameTime)
    {
        if (_activeFlakeCount == 0 || _vertexBuffer == null || _indexBuffer == null)
        {
            return;
        }

        FillVertexData();

        var vertexCount = _activeFlakeCount * 4;
        _vertexBuffer.SetData(_vertices, 0, vertexCount);

        var world = Matrix.CreateScale(Scale) *
                    Matrix.CreateFromYawPitchRoll(Rotation.Y, Rotation.X, Rotation.Z) *
                    Matrix.CreateTranslation(Position);

        var right = _camera.Right;
        var up = _camera.Up;

        var worldParam = _snowEffect.Parameters["World"];
        var viewParam = _snowEffect.Parameters["View"];
        var projectionParam = _snowEffect.Parameters["Projection"];
        var timeParam = _snowEffect.Parameters["Time"];
        var intensityParam = _snowEffect.Parameters["Intensity"];
        var cameraRightParam = _snowEffect.Parameters["CameraRight"];
        var cameraUpParam = _snowEffect.Parameters["CameraUp"];

        worldParam?.SetValue(world);
        viewParam?.SetValue(_camera.View);
        projectionParam?.SetValue(_camera.Projection);
        timeParam?.SetValue(_animationTime);
        intensityParam?.SetValue(_snowIntensity);
        cameraRightParam?.SetValue(right);
        cameraUpParam?.SetValue(up);

        var oldBlend = _graphicsDevice.BlendState;
        var oldDepth = _graphicsDevice.DepthStencilState;
        var oldRaster = _graphicsDevice.RasterizerState;

        _graphicsDevice.BlendState = BlendState.AlphaBlend;
        _graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
        _graphicsDevice.RasterizerState = RasterizerState.CullNone;

        _graphicsDevice.SetVertexBuffer(_vertexBuffer);
        _graphicsDevice.Indices = _indexBuffer;

        if (_snowEffect.Techniques["Snow"] != null)
        {
            _snowEffect.CurrentTechnique = _snowEffect.Techniques["Snow"];
        }

        foreach (var pass in _snowEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawIndexedPrimitives(
                PrimitiveType.TriangleList,
                0,
                0,
                _activeFlakeCount * 2
            );
        }

        _graphicsDevice.BlendState = oldBlend;
        _graphicsDevice.DepthStencilState = oldDepth;
        _graphicsDevice.RasterizerState = oldRaster;
    }

    private void InitBuffers()
    {
        _vertexBuffer = new VertexBuffer(
            _graphicsDevice,
            SnowVertex.VertexDeclaration,
            MaxFlakes * 4,
            BufferUsage.WriteOnly
        );

        _indexBuffer = new IndexBuffer(
            _graphicsDevice,
            IndexElementSize.SixteenBits,
            MaxFlakes * 6,
            BufferUsage.WriteOnly
        );

        for (int i = 0; i < MaxFlakes; i++)
        {
            var vertexOffset = i * 4;
            var indexOffset = i * 6;

            _indices[indexOffset + 0] = (short)(vertexOffset + 0);
            _indices[indexOffset + 1] = (short)(vertexOffset + 1);
            _indices[indexOffset + 2] = (short)(vertexOffset + 2);
            _indices[indexOffset + 3] = (short)(vertexOffset + 2);
            _indices[indexOffset + 4] = (short)(vertexOffset + 1);
            _indices[indexOffset + 5] = (short)(vertexOffset + 3);
        }

        _indexBuffer.SetData(_indices);
    }

    private void UpdateFlakeDensity()
    {
        var target = (int)MathF.Round(_snowIntensity * MaxFlakes);
        target = Math.Clamp(target, 0, MaxFlakes);

        if (target > _activeFlakeCount)
        {
            for (int i = _activeFlakeCount; i < target; i++)
            {
                InitializeFlake(i, true);
            }
        }

        _activeFlakeCount = target;
    }

    private void InitializeFlake(int index, bool randomHeight)
    {
        var halfWidth = _areaSize.X * 0.5f;
        var halfDepth = _areaSize.Z * 0.5f;

        var position = new Vector3(
            MathHelper.Lerp(-halfWidth, halfWidth, (float)_random.NextDouble()),
            randomHeight
                ? MathHelper.Lerp(0f, _areaSize.Y, (float)_random.NextDouble())
                : _areaSize.Y,
            MathHelper.Lerp(-halfDepth, halfDepth, (float)_random.NextDouble())
        );

        var fallSpeed = -MathHelper.Lerp(_minSpeed, _maxSpeed, (float)_random.NextDouble());
        var size = MathHelper.Lerp(_minSize, _maxSize, (float)_random.NextDouble());
        var alpha = MathHelper.Lerp(0.4f, 0.85f, (float)_random.NextDouble());

        _flakes[index] = new SnowFlake
        {
            Position = position,
            Velocity = new Vector3(0f, fallSpeed, 0f),
            Size = size,
            Alpha = alpha
        };
    }

    private void RespawnFlake(ref SnowFlake flake)
    {
        var halfWidth = _areaSize.X * 0.5f;
        var halfDepth = _areaSize.Z * 0.5f;

        flake.Position.X = MathHelper.Lerp(-halfWidth, halfWidth, (float)_random.NextDouble());
        flake.Position.Y = _areaSize.Y + MathHelper.Lerp(0f, _areaSize.Y * 0.2f, (float)_random.NextDouble());
        flake.Position.Z = MathHelper.Lerp(-halfDepth, halfDepth, (float)_random.NextDouble());
        flake.Velocity = new Vector3(0f, -MathHelper.Lerp(_minSpeed, _maxSpeed, (float)_random.NextDouble()), 0f);
        flake.Size = MathHelper.Lerp(_minSize, _maxSize, (float)_random.NextDouble());
        flake.Alpha = MathHelper.Lerp(0.4f, 0.85f, (float)_random.NextDouble());
    }

    private void ReinitializeFlakes()
    {
        for (int i = 0; i < _activeFlakeCount; i++)
        {
            InitializeFlake(i, true);
        }
    }

    private void FillVertexData()
    {
        for (int i = 0; i < _activeFlakeCount; i++)
        {
            var flake = _flakes[i];
            var vertexIndex = i * 4;

            _vertices[vertexIndex + 0] = new SnowVertex
            {
                Position = flake.Position,
                Corner = new Vector2(0f, 0f),
                Size = flake.Size,
                Alpha = flake.Alpha
            };

            _vertices[vertexIndex + 1] = new SnowVertex
            {
                Position = flake.Position,
                Corner = new Vector2(1f, 0f),
                Size = flake.Size,
                Alpha = flake.Alpha
            };

            _vertices[vertexIndex + 2] = new SnowVertex
            {
                Position = flake.Position,
                Corner = new Vector2(0f, 1f),
                Size = flake.Size,
                Alpha = flake.Alpha
            };

            _vertices[vertexIndex + 3] = new SnowVertex
            {
                Position = flake.Position,
                Corner = new Vector2(1f, 1f),
                Size = flake.Size,
                Alpha = flake.Alpha
            };
        }
    }

    private struct SnowFlake
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public float Size;
        public float Alpha;
    }

    private struct SnowVertex : IVertexType
    {
        public Vector3 Position;
        public Vector2 Corner;
        public float Size;
        public float Alpha;

        public static readonly VertexDeclaration VertexDeclaration = new(
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(20, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 1),
            new VertexElement(24, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 2)
        );

        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
    }
}
