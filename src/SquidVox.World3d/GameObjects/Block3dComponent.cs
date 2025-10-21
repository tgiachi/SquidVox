using DryIoc;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using SquidVox.Core.Context;
using SquidVox.Core.GameObjects;
using SquidVox.Voxel.Interfaces;
using SquidVox.Voxel.Types;

namespace SquidVox.World3d.GameObjects;

/// <summary>
/// Renders a textured voxel block in 3D space using textures from the BlockManagerService.
/// </summary>
public sealed class Block3dComponent : Base3dGameObject, IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly IBlockManagerService _blockManagerService;
    private readonly BasicEffect _effect;
    private readonly ILogger _logger = Log.ForContext<Block3dComponent>();

    private VertexBuffer? _vertexBuffer;
    private IndexBuffer? _indexBuffer;
    private bool _geometryInvalidated = true;

    private float _rotationY;
    private Matrix _lastWorld = Matrix.Identity;
    private Matrix _lastView = Matrix.Identity;
    private Matrix _lastProjection = Matrix.Identity;

    private BlockType _blockType = BlockType.Grass;
    private bool _isBillboard;

    /// <summary>
    /// Initializes a new instance of the Block3dComponent class.
    /// </summary>
    /// <param name="graphicsDevice">Graphics device for rendering.</param>
    /// <param name="blockManagerService">Service for block texture management.</param>
    public Block3dComponent()
    {
        _graphicsDevice = SquidVoxGraphicContext.GraphicsDevice;
        _blockManagerService = SquidVoxGraphicContext.Container.Resolve<IBlockManagerService>();

        _effect = new BasicEffect(_graphicsDevice)
        {
            TextureEnabled = true,
            LightingEnabled = false,
            VertexColorEnabled = false
        };

        Name = "Block3D";
        UpdateBillboardStatus();
    }

    /// <summary>
    /// Gets or sets the block type to render.
    /// </summary>
    public BlockType BlockType
    {
        get => _blockType;
        set
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

    protected override void OnUpdate(GameTime gameTime)
    {
        if (AutoRotate)
        {
            var elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _rotationY = (_rotationY + RotationSpeed * elapsedSeconds) % MathHelper.TwoPi;
        }

        base.OnUpdate(gameTime);
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

        Matrix worldMatrix;
        if (IsBillboard)
        {
            // For billboards, create a rotation matrix that always faces the camera
            var cameraDirection = Vector3.Normalize(CameraPosition - Position);
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
            // Normal block rotation
            worldMatrix = Matrix.CreateScale(Size)
                         * Matrix.CreateFromYawPitchRoll(_rotationY + ManualRotation.Y, ManualRotation.X, ManualRotation.Z)
                         * Matrix.CreateTranslation(Position);
        }

        _lastWorld = worldMatrix;

        var lookTarget = CameraTarget == Vector3.Zero ? Position : CameraTarget;

        _lastView = Matrix.CreateLookAt(CameraPosition, lookTarget, Vector3.Up);
        _lastProjection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, 0.1f, 100f);

        _effect.World = _lastWorld;
        _effect.View = _lastView;
        _effect.Projection = _lastProjection;

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
        var vertices = new List<VertexPositionTexture>();
        var indices = new List<short>();
        short baseIndex = 0;

        Texture2D? commonTexture = null;

        if (IsBillboard)
        {
            // For billboards, create a single quad facing forward (North face)
            var texture = _blockManagerService.GetBlockSide(BlockType, BlockSide.North);

            if (texture != null)
            {
                commonTexture = texture;
                var uv = (Min: Vector2.Zero, Max: Vector2.One);
                var faceVertices = GetFaceVertices(BlockSide.North, halfSize, uv);

                vertices.AddRange(faceVertices);
                indices.AddRange([
                    baseIndex,
                    (short)(baseIndex + 1),
                    (short)(baseIndex + 2),
                    (short)(baseIndex + 2),
                    (short)(baseIndex + 3),
                    baseIndex
                ]);
            }
        }
        else
        {
            // For normal blocks, create all 6 faces
            foreach (BlockSide side in Enum.GetValues<BlockSide>())
            {
                var texture = _blockManagerService.GetBlockSide(BlockType, side);

                if (texture == null)
                {
                    _logger.Warning("Texture for block {BlockType} side {Side} not found", BlockType, side);
                    continue;
                }

                commonTexture ??= texture;

                var uv = (Min: Vector2.Zero, Max: Vector2.One);
                var faceVertices = GetFaceVertices(side, halfSize, uv);

                vertices.AddRange(faceVertices);
                indices.AddRange([
                    baseIndex,
                    (short)(baseIndex + 1),
                    (short)(baseIndex + 2),
                    (short)(baseIndex + 2),
                    (short)(baseIndex + 3),
                    baseIndex
                ]);

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
            typeof(VertexPositionTexture),
            vertices.Count,
            BufferUsage.WriteOnly
        );
        _vertexBuffer.SetData(vertices.ToArray());

        _indexBuffer = new IndexBuffer(_graphicsDevice, IndexElementSize.SixteenBits, indices.Count, BufferUsage.WriteOnly);
        _indexBuffer.SetData(indices.ToArray());

        _effect.Texture = commonTexture;

        _geometryInvalidated = false;
    }

    private static VertexPositionTexture[] GetFaceVertices(BlockSide side, float halfSize, (Vector2 Min, Vector2 Max) uv)
    {
        var (min, max) = uv;

        return side switch
        {
            BlockSide.Top =>
            [
                new VertexPositionTexture(new Vector3(-halfSize, halfSize, -halfSize), new Vector2(min.X, min.Y)),
                new VertexPositionTexture(new Vector3(-halfSize, halfSize, halfSize), new Vector2(min.X, max.Y)),
                new VertexPositionTexture(new Vector3(halfSize, halfSize, halfSize), new Vector2(max.X, max.Y)),
                new VertexPositionTexture(new Vector3(halfSize, halfSize, -halfSize), new Vector2(max.X, min.Y))
            ],
            BlockSide.Bottom =>
            [
                new VertexPositionTexture(new Vector3(-halfSize, -halfSize, halfSize), new Vector2(min.X, min.Y)),
                new VertexPositionTexture(new Vector3(-halfSize, -halfSize, -halfSize), new Vector2(min.X, max.Y)),
                new VertexPositionTexture(new Vector3(halfSize, -halfSize, -halfSize), new Vector2(max.X, max.Y)),
                new VertexPositionTexture(new Vector3(halfSize, -halfSize, halfSize), new Vector2(max.X, min.Y))
            ],
            BlockSide.North =>
            [
                new VertexPositionTexture(new Vector3(-halfSize, halfSize, -halfSize), new Vector2(min.X, min.Y)),
                new VertexPositionTexture(new Vector3(-halfSize, -halfSize, -halfSize), new Vector2(min.X, max.Y)),
                new VertexPositionTexture(new Vector3(halfSize, -halfSize, -halfSize), new Vector2(max.X, max.Y)),
                new VertexPositionTexture(new Vector3(halfSize, halfSize, -halfSize), new Vector2(max.X, min.Y))
            ],
            BlockSide.South =>
            [
                new VertexPositionTexture(new Vector3(halfSize, halfSize, halfSize), new Vector2(min.X, min.Y)),
                new VertexPositionTexture(new Vector3(halfSize, -halfSize, halfSize), new Vector2(min.X, max.Y)),
                new VertexPositionTexture(new Vector3(-halfSize, -halfSize, halfSize), new Vector2(max.X, max.Y)),
                new VertexPositionTexture(new Vector3(-halfSize, halfSize, halfSize), new Vector2(max.X, min.Y))
            ],
            BlockSide.East =>
            [
                new VertexPositionTexture(new Vector3(halfSize, halfSize, -halfSize), new Vector2(min.X, min.Y)),
                new VertexPositionTexture(new Vector3(halfSize, -halfSize, -halfSize), new Vector2(min.X, max.Y)),
                new VertexPositionTexture(new Vector3(halfSize, -halfSize, halfSize), new Vector2(max.X, max.Y)),
                new VertexPositionTexture(new Vector3(halfSize, halfSize, halfSize), new Vector2(max.X, min.Y))
            ],
            BlockSide.West =>
            [
                new VertexPositionTexture(new Vector3(-halfSize, halfSize, halfSize), new Vector2(min.X, min.Y)),
                new VertexPositionTexture(new Vector3(-halfSize, -halfSize, halfSize), new Vector2(min.X, max.Y)),
                new VertexPositionTexture(new Vector3(-halfSize, -halfSize, -halfSize), new Vector2(max.X, max.Y)),
                new VertexPositionTexture(new Vector3(-halfSize, halfSize, -halfSize), new Vector2(max.X, min.Y))
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(side), side, "Unsupported block side")
        };
    }
}
