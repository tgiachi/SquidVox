using DryIoc;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Graphics;
using Serilog;
using SquidVox.Core.Context;
using SquidVox.Core.GameObjects;
using SquidVox.Core.Interfaces.Services;
using SquidVox.Voxel.Interfaces;
using SquidVox.Voxel.Types;

namespace SquidVox.World3d.GameObjects;

/// <summary>
/// Custom vertex format for block rendering with normals, AO, and per-vertex lighting.
/// </summary>
public struct VertexPositionNormalTextureAO : IVertexType
{
    public Vector3 Position;
    public Vector3 Normal;
    public Vector4 UV; // xy = texture coords, z = AO, w = light

    public static readonly VertexDeclaration VertexDeclaration = new(
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
        new VertexElement(24, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 0)
    );

    public VertexPositionNormalTextureAO(Vector3 position, Vector3 normal, Vector4 uv)
    {
        Position = position;
        Normal = normal;
        UV = uv;
    }

    VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
}

/// <summary>
/// Renders a textured voxel block in 3D space using textures from the BlockManagerService.
/// </summary>
public sealed class Block3dComponent : Base3dGameObject, IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly IBlockManagerService _blockManagerService;
    private readonly IAssetManagerService _assetManagerService;
    private readonly Effect _effect;
    private readonly ILogger _logger = Log.ForContext<Block3dComponent>();
    private readonly CameraComponent? _camera;

    private VertexBuffer? _vertexBuffer;
    private IndexBuffer? _indexBuffer;
    private Texture2D? _currentTexture;
    private bool _geometryInvalidated = true;

    private float _rotationY;
    private Matrix _lastWorld = Matrix.Identity;
    private Matrix _lastView = Matrix.Identity;
    private Matrix _lastProjection = Matrix.Identity;

    private BlockType _blockType = BlockType.Grass;
    private bool _isBillboard;

    // Shader parameters
    private float _timer;
    private float _timeOfDay; // 0.0 to 1.0, represents time of day cycle
    private float _daylight = 1.0f; // Calculated daylight intensity
    private float _fogDistance = 100.0f;
    private float _dayNightCycleSpeed = 0.05f; // Speed of day/night cycle

    /// <summary>
    /// Initializes a new instance of the Block3dComponent class.
    /// </summary>
    /// <param name="camera">Optional camera component to use for rendering.</param>
    public Block3dComponent(CameraComponent? camera = null)
    {
        _graphicsDevice = SquidVoxGraphicContext.GraphicsDevice;
        _blockManagerService = SquidVoxGraphicContext.Container.Resolve<IBlockManagerService>();
        _assetManagerService = SquidVoxGraphicContext.Container.Resolve<IAssetManagerService>();
        _camera = camera;

        _effect = _assetManagerService.GetEffect("Effects/ChunkBlock");

        Name = "Block3D";
        UpdateBillboardStatus();
    }

    /// <summary>
    /// Gets or sets the block type to render.
    /// </summary>
    public BlockType BlockType
    {
        get => _blockType;
        init
        {
            if (_blockType != value)
            {
                _blockType = value;
                UpdateBillboardStatus();
                _geometryInvalidated = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets whether this block should render as a billboard (always facing camera).
    /// </summary>
    public bool IsBillboard
    {
        get => _isBillboard;
        set
        {
            if (_isBillboard != value)
            {
                _isBillboard = value;
                _geometryInvalidated = true;
            }
        }
    }

    /// <summary>
    /// Updates the billboard status based on the current block type.
    /// </summary>
    private void UpdateBillboardStatus()
    {
        var definition = _blockManagerService.GetBlockDefinition(_blockType);
        _isBillboard = definition?.IsBillboard ?? false;
    }

    /// <summary>
    /// Gets or sets the block size in world units.
    /// </summary>
    public float Size { get; set; } = 1f;

    /// <summary>
    /// Gets or sets whether auto-rotation is enabled.
    /// </summary>
    public bool AutoRotate { get; set; } = true;

    /// <summary>
    /// Gets or sets the rotation speed in radians per second.
    /// </summary>
    public float RotationSpeed { get; set; } = MathHelper.ToRadians(25f);

    /// <summary>
    /// Gets or sets manual rotation applied to the block.
    /// </summary>
    public Vector3 ManualRotation { get; set; } = Vector3.Zero;

    /// <summary>
    /// Gets or sets the camera position for rendering.
    /// </summary>
    public Vector3 CameraPosition { get; set; } = new(3f, 3f, 3f);

    /// <summary>
    /// Gets or sets the camera target position.
    /// </summary>
    public Vector3 CameraTarget { get; set; } = Vector3.Zero;

    /// <summary>
    /// Gets or sets the time of day (0.0 = midnight, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset).
    /// </summary>
    public float TimeOfDay
    {
        get => _timeOfDay;
        set => _timeOfDay = value % 1.0f;
    }

    /// <summary>
    /// Gets the current daylight intensity (0.0 to 1.0).
    /// </summary>
    public float Daylight => _daylight;

    /// <summary>
    /// Gets or sets the speed of the day/night cycle.
    /// </summary>
    public float DayNightCycleSpeed
    {
        get => _dayNightCycleSpeed;
        set => _dayNightCycleSpeed = value;
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        var elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (AutoRotate)
        {
            _rotationY = (_rotationY + RotationSpeed * elapsedSeconds) % MathHelper.TwoPi;
        }

        // Update time of day (0.0 to 1.0 cycle)
        _timeOfDay = (_timeOfDay + elapsedSeconds * _dayNightCycleSpeed) % 1.0f;

        // Calculate daylight intensity using sigmoid function
        _daylight = GetDaylight(_timeOfDay);

        // Update timer for shader animation (wraps at 1.0 for sky texture sampling)
        _timer = (_timer + elapsedSeconds * 0.01f) % 1.0f;

        base.OnUpdate(gameTime);
    }

    /// <summary>
    /// Calculates daylight intensity based on time of day using a sigmoid function.
    /// </summary>
    /// <param name="timeOfDay">Time of day from 0.0 to 1.0</param>
    /// <returns>Daylight intensity from 0.0 to 1.0</returns>
    private static float GetDaylight(float timeOfDay)
    {
        if (timeOfDay < 0.5f)
        {
            // Sunrise/morning: sigmoid curve centered at 0.25
            float t = (timeOfDay - 0.25f) * 100f;
            return 1.0f / (1.0f + MathF.Pow(2, -t));
        }
        else
        {
            // Sunset/night: inverse sigmoid curve centered at 0.85
            float t = (timeOfDay - 0.85f) * 100f;
            return 1.0f - 1.0f / (1.0f + MathF.Pow(2, -t));
        }
    }

    protected override void OnRender(GraphicsDevice graphicsDevice)
    {
        EnsureGeometry();

        if (_vertexBuffer == null || _indexBuffer == null)
        {
            return;
        }

        var viewport = _graphicsDevice.Viewport;
        var aspectRatio = viewport.AspectRatio <= 0 ? 1f : viewport.AspectRatio;

        // Block transformation in world space
        Matrix worldMatrix;

        if (IsBillboard)
        {
            // Billboards face the camera
            var cameraPos = _camera?.Position ?? CameraPosition;
            var cameraDirection = Vector3.Normalize(cameraPos - Position);
            var up = Vector3.Up;

            // Create billboard rotation (Y-axis only, like grass/flowers)
            var right = Vector3.Cross(up, cameraDirection);
            var forward = Vector3.Cross(right, up);

            worldMatrix = Matrix.CreateScale(Size) *
                          new Matrix(
                              right.X, right.Y, right.Z, 0,
                              up.X, up.Y, up.Z, 0,
                              forward.X, forward.Y, forward.Z, 0,
                              0, 0, 0, 1
                          ) *
                          Matrix.CreateTranslation(Position);
        }
        else
        {
            // Normal block - no rotation applied, block stays fixed
            worldMatrix = Matrix.CreateScale(Size) * Matrix.CreateTranslation(Position);
        }

        _lastWorld = worldMatrix;

        // Use camera component if provided, otherwise use default camera position
        if (_camera != null)
        {
            _lastView = _camera.View;
            _lastProjection = _camera.Projection;
        }
        else
        {
            var lookTarget = CameraTarget == Vector3.Zero ? Position : CameraTarget;
            _lastView = Matrix.CreateLookAt(CameraPosition, lookTarget, Vector3.Up);
            _lastProjection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, 0.1f, 100f);
        }

        // Combine world-view-projection matrices
        var mvpMatrix = _lastWorld * _lastView * _lastProjection;

        // Set shader parameters for ChunkBlock shader
        _effect.Parameters["mvpMatrix"]?.SetValue(mvpMatrix);
        _effect.Parameters["worldMatrix"]?.SetValue(_lastWorld);
        _effect.Parameters["camera"]?.SetValue(CameraPosition);
        _effect.Parameters["fog_distance"]?.SetValue(_fogDistance);
        _effect.Parameters["ortho"]?.SetValue(false); // Always use perspective for this block
        _effect.Parameters["timer"]?.SetValue(_timer);
        _effect.Parameters["daylight"]?.SetValue(_daylight);

        if (_currentTexture != null)
        {
            _effect.Parameters["tex"]?.SetValue(_currentTexture);
        }
        else
        {
            _logger.Warning("Current texture is null!");
        }

        // Sky texture is optional - if not set, fog won't be applied
        // _effect.Parameters["sky_tex"]?.SetValue(skyTexture);

        var previousBlendState = _graphicsDevice.BlendState;
        var previousDepthStencilState = _graphicsDevice.DepthStencilState;
        var previousRasterizerState = _graphicsDevice.RasterizerState;
        var previousSamplerState = _graphicsDevice.SamplerStates[0];

        _graphicsDevice.BlendState = BlendState.Opaque;
        _graphicsDevice.DepthStencilState = DepthStencilState.Default;
        _graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
        _graphicsDevice.SamplerStates[0] = SamplerState.PointClamp;

        _graphicsDevice.SetVertexBuffer(_vertexBuffer);
        _graphicsDevice.Indices = _indexBuffer;

        var triangleCount = IsBillboard ? 2 : 12; // 2 triangles for billboard, 12 for full cube

        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, triangleCount);
        }

        _graphicsDevice.SetVertexBuffer(null);
        _graphicsDevice.Indices = null;

        _graphicsDevice.BlendState = previousBlendState;
        _graphicsDevice.DepthStencilState = previousDepthStencilState;
        _graphicsDevice.RasterizerState = previousRasterizerState;
        _graphicsDevice.SamplerStates[0] = previousSamplerState;

        base.OnRender(graphicsDevice);
    }

    public void Dispose()
    {
        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
        _effect.Dispose();
    }

    private void EnsureGeometry()
    {
        if (!_geometryInvalidated)
        {
            return;
        }

        var halfSize = 0.5f;
        var vertices = new List<VertexPositionNormalTextureAO>();
        var indices = new List<short>();
        short baseIndex = 0;

        Texture2D? commonTexture = null;

        if (IsBillboard)
        {
            // For billboards, create a single quad facing forward (North face)
            var region = _blockManagerService.GetBlockSide(BlockType, BlockSide.North);

            if (region != null)
            {
                commonTexture = region.Texture;
                var uv = ExtractUv(region);
                var faceVertices = GetFaceVertices(BlockSide.North, halfSize, uv);

                vertices.AddRange(faceVertices);
                indices.AddRange(
                    [
                        baseIndex,
                        (short)(baseIndex + 1),
                        (short)(baseIndex + 2),
                        (short)(baseIndex + 2),
                        (short)(baseIndex + 3),
                        baseIndex
                    ]
                );
            }
        }
        else
        {
            // For normal blocks, create all 6 faces
            foreach (BlockSide side in Enum.GetValues<BlockSide>())
            {
                var region = _blockManagerService.GetBlockSide(BlockType, side);

                if (region == null)
                {
                    _logger.Warning("Texture region for block {BlockType} side {Side} not found", BlockType, side);
                    continue;
                }

                // Use the atlas texture as the common texture
                commonTexture ??= region.Texture;

                // Extract UV coordinates from the region
                var uv = ExtractUv(region);
                var faceVertices = GetFaceVertices(side, halfSize, uv);

                vertices.AddRange(faceVertices);
                indices.AddRange(
                    [
                        baseIndex,
                        (short)(baseIndex + 1),
                        (short)(baseIndex + 2),
                        (short)(baseIndex + 2),
                        (short)(baseIndex + 3),
                        baseIndex
                    ]
                );

                baseIndex += 4;
            }
        }

        if (vertices.Count == 0 || indices.Count == 0)
        {
            _logger.Warning("Unable to build geometry for block {BlockType}", BlockType);
            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();
            _vertexBuffer = null;
            _indexBuffer = null;
            _geometryInvalidated = false;
            return;
        }

        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();

        _vertexBuffer = new VertexBuffer(
            _graphicsDevice,
            typeof(VertexPositionNormalTextureAO),
            vertices.Count,
            BufferUsage.WriteOnly
        );
        _vertexBuffer.SetData(vertices.ToArray());

        _indexBuffer = new IndexBuffer(_graphicsDevice, IndexElementSize.SixteenBits, indices.Count, BufferUsage.WriteOnly);
        _indexBuffer.SetData(indices.ToArray());

        _currentTexture = commonTexture;
        _geometryInvalidated = false;
    }

    /// <summary>
    /// Gets the normal vector for a given block side.
    /// </summary>
    /// <param name="side">The block side.</param>
    /// <returns>The normal vector.</returns>
    private static Vector3 GetNormal(BlockSide side)
    {
        return side switch
        {
            BlockSide.Top => new Vector3(0, 1, 0),
            BlockSide.Bottom => new Vector3(0, -1, 0),
            BlockSide.North => new Vector3(0, 0, -1),
            BlockSide.South => new Vector3(0, 0, 1),
            BlockSide.East => new Vector3(1, 0, 0),
            BlockSide.West => new Vector3(-1, 0, 0),
            _ => Vector3.Up
        };
    }

    /// <summary>
    /// Extracts normalized UV coordinates from a texture region.
    /// </summary>
    /// <param name="region">The texture region.</param>
    /// <returns>A tuple containing the min and max UV coordinates.</returns>
    private static (Vector2 Min, Vector2 Max) ExtractUv(Texture2DRegion region)
    {
        var bounds = region.Bounds;
        var texture = region.Texture;

        var min = new Vector2(
            bounds.X / (float)texture.Width,
            bounds.Y / (float)texture.Height
        );

        var max = new Vector2(
            (bounds.X + bounds.Width) / (float)texture.Width,
            (bounds.Y + bounds.Height) / (float)texture.Height
        );

        return (min, max);
    }

    private static VertexPositionNormalTextureAO[] GetFaceVertices(BlockSide side, float halfSize, (Vector2 Min, Vector2 Max) uv)
    {
        var (min, max) = uv;
        var normal = GetNormal(side);
        // UV.z = AO (1.0 = no occlusion), UV.w = per-vertex light (0.0 = no extra light)
        const float ao = 1.0f;
        const float light = 0.0f;

        return side switch
        {
            BlockSide.Top =>
            [
                new VertexPositionNormalTextureAO(new Vector3(-halfSize, halfSize, -halfSize), normal, new Vector4(min.X, min.Y, ao, light)),
                new VertexPositionNormalTextureAO(new Vector3(halfSize, halfSize, -halfSize), normal, new Vector4(max.X, min.Y, ao, light)),
                new VertexPositionNormalTextureAO(new Vector3(halfSize, halfSize, halfSize), normal, new Vector4(max.X, max.Y, ao, light)),
                new VertexPositionNormalTextureAO(new Vector3(-halfSize, halfSize, halfSize), normal, new Vector4(min.X, max.Y, ao, light))
            ],
            BlockSide.Bottom =>
            [
                new VertexPositionNormalTextureAO(new Vector3(-halfSize, -halfSize, halfSize), normal, new Vector4(min.X, min.Y, ao, light)),
                new VertexPositionNormalTextureAO(new Vector3(halfSize, -halfSize, halfSize), normal, new Vector4(max.X, min.Y, ao, light)),
                new VertexPositionNormalTextureAO(new Vector3(halfSize, -halfSize, -halfSize), normal, new Vector4(max.X, max.Y, ao, light)),
                new VertexPositionNormalTextureAO(new Vector3(-halfSize, -halfSize, -halfSize), normal, new Vector4(min.X, max.Y, ao, light))
            ],
            BlockSide.North =>
            [
                new VertexPositionNormalTextureAO(new Vector3(-halfSize, halfSize, -halfSize), normal, new Vector4(min.X, min.Y, ao, light)),
                new VertexPositionNormalTextureAO(new Vector3(-halfSize, -halfSize, -halfSize), normal, new Vector4(min.X, max.Y, ao, light)),
                new VertexPositionNormalTextureAO(new Vector3(halfSize, -halfSize, -halfSize), normal, new Vector4(max.X, max.Y, ao, light)),
                new VertexPositionNormalTextureAO(new Vector3(halfSize, halfSize, -halfSize), normal, new Vector4(max.X, min.Y, ao, light))
            ],
            BlockSide.South =>
            [
                new VertexPositionNormalTextureAO(new Vector3(halfSize, halfSize, halfSize), normal, new Vector4(min.X, min.Y, ao, light)),
                new VertexPositionNormalTextureAO(new Vector3(halfSize, -halfSize, halfSize), normal, new Vector4(min.X, max.Y, ao, light)),
                new VertexPositionNormalTextureAO(new Vector3(-halfSize, -halfSize, halfSize), normal, new Vector4(max.X, max.Y, ao, light)),
                new VertexPositionNormalTextureAO(new Vector3(-halfSize, halfSize, halfSize), normal, new Vector4(max.X, min.Y, ao, light))
            ],
            BlockSide.East =>
            [
                new VertexPositionNormalTextureAO(new Vector3(halfSize, halfSize, -halfSize), normal, new Vector4(min.X, min.Y, ao, light)),
                new VertexPositionNormalTextureAO(new Vector3(halfSize, -halfSize, -halfSize), normal, new Vector4(min.X, max.Y, ao, light)),
                new VertexPositionNormalTextureAO(new Vector3(halfSize, -halfSize, halfSize), normal, new Vector4(max.X, max.Y, ao, light)),
                new VertexPositionNormalTextureAO(new Vector3(halfSize, halfSize, halfSize), normal, new Vector4(max.X, min.Y, ao, light))
            ],
            BlockSide.West =>
            [
                new VertexPositionNormalTextureAO(new Vector3(-halfSize, halfSize, halfSize), normal, new Vector4(min.X, min.Y, ao, light)),
                new VertexPositionNormalTextureAO(new Vector3(-halfSize, -halfSize, halfSize), normal, new Vector4(min.X, max.Y, ao, light)),
                new VertexPositionNormalTextureAO(new Vector3(-halfSize, -halfSize, -halfSize), normal, new Vector4(max.X, max.Y, ao, light)),
                new VertexPositionNormalTextureAO(new Vector3(-halfSize, halfSize, -halfSize), normal, new Vector4(max.X, min.Y, ao, light))
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(side), side, "Unsupported block side")
        };
    }
}
