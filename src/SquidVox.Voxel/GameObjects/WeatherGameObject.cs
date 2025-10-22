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
/// Renders volumetric rain streaks inside a configurable 3D volume with adjustable intensity.
/// </summary>
public class WeatherGameObject : Base3dGameObject, IDisposable
{
    private const int MaxDrops = 2048;

    private readonly ILogger _logger = Log.ForContext<WeatherGameObject>();
    private readonly GraphicsDevice _graphicsDevice;
    private readonly Effect _rainEffect;
    private readonly CameraGameObject _camera;
    private readonly Random _random = new();
    private readonly RainDrop[] _drops = new RainDrop[MaxDrops];
    private readonly RainVertex[] _vertices = new RainVertex[MaxDrops * 4];
    private readonly short[] _indices = new short[MaxDrops * 6];
    private VertexBuffer? _vertexBuffer;
    private IndexBuffer? _indexBuffer;
    private int _activeDropCount;
    private float _rainIntensity = 0.4f;
    private float _animationTime;
    private Vector3 _areaSize = new(140f, 90f, 140f);
    private float _minSpeed = 22f;
    private float _maxSpeed = 38f;
    private float _minLength = 6f;
    private float _maxLength = 14f;
    private float _dropWidth = 0.1f;

    /// <summary>
    /// Initializes a new instance of the WeatherGameObject class.
    /// </summary>
    /// <param name="camera">The active camera controlling view and projection matrices.</param>
    public WeatherGameObject(CameraGameObject camera)
    {
        _camera = camera ?? throw new ArgumentNullException(nameof(camera));
        _graphicsDevice = SquidVoxGraphicContext.GraphicsDevice;

        var assetManager = SquidVoxGraphicContext.Container.Resolve<IAssetManagerService>();
        _rainEffect = assetManager.GetEffect("Effects/Rain") ?? throw new InvalidOperationException("Rain effect not loaded.");

        InitBuffers();
        UpdateDropDensity();

        _logger.Information("WeatherGameObject initialized with {DropCount} rain streaks.", _activeDropCount);
    }

    /// <summary>
    /// Gets or sets the rain intensity from 0.0 (clear) to 1.0 (torrential).
    /// </summary>
    public float RainIntensity
    {
        get => _rainIntensity;
        set
        {
            var clamped = MathHelper.Clamp(value, 0f, 1f);
            if (Math.Abs(_rainIntensity - clamped) < 0.0001f)
            {
                return;
            }

            _rainIntensity = clamped;
            UpdateDropDensity();
        }
    }

    /// <summary>
    /// Gets or sets the dimensions of the rain simulation volume.
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

            ReinitializeDrops();
        }
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

        if (_activeDropCount == 0)
        {
            return;
        }

        for (int i = 0; i < _activeDropCount; i++)
        {
            ref var drop = ref _drops[i];
            drop.Position.Y -= drop.Speed * delta;

            if (drop.Position.Y < 0f)
            {
                RespawnDrop(ref drop);
            }
        }
    }

    protected override void OnDraw3d(GameTime gameTime)
    {
        if (_activeDropCount == 0 || _vertexBuffer == null || _indexBuffer == null)
        {
            return;
        }

        FillVertexData();

        var vertexCount = _activeDropCount * 4;
        _vertexBuffer.SetData(_vertices, 0, vertexCount);

        var world = Matrix.CreateScale(Scale) *
                    Matrix.CreateFromYawPitchRoll(Rotation.Y, Rotation.X, Rotation.Z) *
                    Matrix.CreateTranslation(Position);

        var rainDirection = Vector3.Normalize(Vector3.Down);

        var right = _camera.Right;

        var worldParam = _rainEffect.Parameters["World"];
        var viewParam = _rainEffect.Parameters["View"];
        var projectionParam = _rainEffect.Parameters["Projection"];
        var timeParam = _rainEffect.Parameters["Time"];
        var intensityParam = _rainEffect.Parameters["Intensity"];
        var cameraRightParam = _rainEffect.Parameters["CameraRight"];
        var rainDirectionParam = _rainEffect.Parameters["RainDirection"];
        var dropWidthParam = _rainEffect.Parameters["DropWidth"];

        worldParam?.SetValue(world);
        viewParam?.SetValue(_camera.View);
        projectionParam?.SetValue(_camera.Projection);
        timeParam?.SetValue(_animationTime);
        intensityParam?.SetValue(_rainIntensity);
        cameraRightParam?.SetValue(right);
        rainDirectionParam?.SetValue(rainDirection);
        dropWidthParam?.SetValue(_dropWidth);

        var oldBlend = _graphicsDevice.BlendState;
        var oldDepth = _graphicsDevice.DepthStencilState;
        var oldRaster = _graphicsDevice.RasterizerState;

        _graphicsDevice.BlendState = BlendState.AlphaBlend;
        _graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
        _graphicsDevice.RasterizerState = RasterizerState.CullNone;

        _graphicsDevice.SetVertexBuffer(_vertexBuffer);
        _graphicsDevice.Indices = _indexBuffer;

        if (_rainEffect.Techniques["Rain"] != null)
        {
            _rainEffect.CurrentTechnique = _rainEffect.Techniques["Rain"];
        }

        foreach (var pass in _rainEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawIndexedPrimitives(
                PrimitiveType.TriangleList,
                0,
                0,
                _activeDropCount * 2
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
            RainVertex.VertexDeclaration,
            MaxDrops * 4,
            BufferUsage.WriteOnly
        );

        _indexBuffer = new IndexBuffer(
            _graphicsDevice,
            IndexElementSize.SixteenBits,
            MaxDrops * 6,
            BufferUsage.WriteOnly
        );

        for (int i = 0; i < MaxDrops; i++)
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

    private void UpdateDropDensity()
    {
        var target = (int)MathF.Round(_rainIntensity * MaxDrops);
        target = Math.Clamp(target, 0, MaxDrops);

        if (target > _activeDropCount)
        {
            for (int i = _activeDropCount; i < target; i++)
            {
                InitializeDrop(i, true);
            }
        }

        _activeDropCount = target;
    }

    private void InitializeDrop(int index, bool randomHeight)
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

        var speed = MathHelper.Lerp(_minSpeed, _maxSpeed, (float)_random.NextDouble());
        var length = MathHelper.Lerp(_minLength, _maxLength, (float)_random.NextDouble());
        var alpha = MathHelper.Lerp(0.55f, 1f, (float)_random.NextDouble());

        _drops[index] = new RainDrop
        {
            Position = position,
            Speed = speed,
            Length = length,
            Alpha = alpha
        };
    }

    private void RespawnDrop(ref RainDrop drop)
    {
        var halfWidth = _areaSize.X * 0.5f;
        var halfDepth = _areaSize.Z * 0.5f;

        drop.Position.X = MathHelper.Lerp(-halfWidth, halfWidth, (float)_random.NextDouble());
        drop.Position.Y = _areaSize.Y + MathHelper.Lerp(0f, _areaSize.Y * 0.2f, (float)_random.NextDouble());
        drop.Position.Z = MathHelper.Lerp(-halfDepth, halfDepth, (float)_random.NextDouble());
        drop.Speed = MathHelper.Lerp(_minSpeed, _maxSpeed, (float)_random.NextDouble());
        drop.Length = MathHelper.Lerp(_minLength, _maxLength, (float)_random.NextDouble());
        drop.Alpha = MathHelper.Lerp(0.55f, 1f, (float)_random.NextDouble());
    }

    private void ReinitializeDrops()
    {
        for (int i = 0; i < _activeDropCount; i++)
        {
            InitializeDrop(i, true);
        }
    }

    private void FillVertexData()
    {
        for (int i = 0; i < _activeDropCount; i++)
        {
            var drop = _drops[i];
            var vertexIndex = i * 4;

            _vertices[vertexIndex + 0] = new RainVertex
            {
                Position = drop.Position,
                Corner = new Vector2(0f, 0f),
                Length = drop.Length,
                Alpha = drop.Alpha
            };

            _vertices[vertexIndex + 1] = new RainVertex
            {
                Position = drop.Position,
                Corner = new Vector2(1f, 0f),
                Length = drop.Length,
                Alpha = drop.Alpha
            };

            _vertices[vertexIndex + 2] = new RainVertex
            {
                Position = drop.Position,
                Corner = new Vector2(0f, 1f),
                Length = drop.Length,
                Alpha = drop.Alpha
            };

            _vertices[vertexIndex + 3] = new RainVertex
            {
                Position = drop.Position,
                Corner = new Vector2(1f, 1f),
                Length = drop.Length,
                Alpha = drop.Alpha
            };
        }
    }

    private struct RainDrop
    {
        public Vector3 Position;
        public float Speed;
        public float Length;
        public float Alpha;
    }

    private struct RainVertex : IVertexType
    {
        public Vector3 Position;
        public Vector2 Corner;
        public float Length;
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
