using DryIoc;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using SquidVox.Core.Context;
using SquidVox.Core.GameObjects;
using SquidVox.Voxel.Data;
using SquidVox.Voxel.Interfaces;
using SquidVox.Voxel.Services;
using SquidVox.Voxel.Types;

namespace SquidVox.World3d.GameObjects;

/// <summary>
/// Renders a single chunk with basic meshing.
/// </summary>
public sealed class ChunkComponent : Base3dGameObject, IDisposable
{
    private static readonly Dictionary<BlockSide, (int X, int Y, int Z)> NeighborOffsets = new()
    {
        { BlockSide.Top, (0, 1, 0) },
        { BlockSide.Bottom, (0, -1, 0) },
        { BlockSide.North, (0, 0, -1) },
        { BlockSide.South, (0, 0, 1) },
        { BlockSide.East, (1, 0, 0) },
        { BlockSide.West, (-1, 0, 0) }
    };

    private readonly GraphicsDevice _graphicsDevice;
    private readonly BlockManagerService _blockManagerService;
    private readonly BasicEffect _effect;
    private readonly ILogger _logger = Log.ForContext<ChunkComponent>();

    private VertexBuffer? _vertexBuffer;
    private IndexBuffer? _indexBuffer;
    private bool _geometryInvalidated = true;
    private int _primitiveCount;
    private ChunkEntity? _chunk;

    /// <summary>
    /// Gets or sets the delegate for retrieving neighboring chunks for cross-chunk face culling.
    /// </summary>
    public Func<Vector3, ChunkEntity?>? GetNeighborChunk { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether fog is enabled.
    /// </summary>
    public bool FogEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the fog color.
    /// </summary>
    public Vector3 FogColor { get; set; } = new Vector3(0.7f, 0.8f, 0.9f);

    /// <summary>
    /// Gets or sets the fog start distance.
    /// </summary>
    public float FogStart { get; set; } = 80f;

    /// <summary>
    /// Gets or sets the fog end distance.
    /// </summary>
    public float FogEnd { get; set; } = 150f;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChunkComponent"/> class.
    /// </summary>
    public ChunkComponent()
    {
        _graphicsDevice = SquidVoxGraphicContext.GraphicsDevice;
        _blockManagerService = (BlockManagerService)SquidVoxGraphicContext.Container.Resolve<IBlockManagerService>();

        _effect = new BasicEffect(_graphicsDevice)
        {
            TextureEnabled = false,
            LightingEnabled = false,
            VertexColorEnabled = true,
            FogEnabled = FogEnabled,
            FogColor = FogColor,
            FogStart = FogStart,
            FogEnd = FogEnd
        };
    }

    /// <summary>
    /// Gets the chunk currently bound to the component.
    /// </summary>
    public ChunkEntity? Chunk => _chunk;

    /// <summary>
    /// Gets a value indicating whether the chunk has mesh data.
    /// </summary>
    public bool HasMesh => _vertexBuffer != null && _indexBuffer != null;

    /// <summary>
    /// Sets the chunk to render.
    /// </summary>
    /// <param name="chunk">The chunk entity.</param>
    public void SetChunk(ChunkEntity chunk)
    {
        _chunk = chunk ?? throw new ArgumentNullException(nameof(chunk));
        Position = _chunk.Position;
        InvalidateGeometry();
    }

    /// <summary>
    /// Invalidates the geometry, forcing a rebuild on next draw.
    /// </summary>
    public void InvalidateGeometry()
    {
        _geometryInvalidated = true;
    }

    /// <summary>
    /// Builds the mesh immediately.
    /// </summary>
    public void BuildMeshImmediate()
    {
        if (_chunk == null || !_geometryInvalidated)
            return;

        var vertices = new List<VertexPositionColor>();
        var indices = new List<int>();

        for (int x = 0; x < ChunkEntity.Width; x++)
        {
            for (int y = 0; y < ChunkEntity.Height; y++)
            {
                for (int z = 0; z < ChunkEntity.Depth; z++)
                {
                    ref var cell = ref _chunk.At(x, y, z);
                    if (cell.BlockType == BlockType.Air)
                        continue;

                    var blockColor = GetBlockColor(cell.BlockType);

                    foreach (BlockSide side in Enum.GetValues<BlockSide>())
                    {
                        if (!ShouldRenderFace(x, y, z, side))
                            continue;

                        AddFace(vertices, indices, x, y, z, side, blockColor);
                    }
                }
            }
        }

        if (vertices.Count == 0)
        {
            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();
            _vertexBuffer = null;
            _indexBuffer = null;
            _primitiveCount = 0;
            _geometryInvalidated = false;
            return;
        }

        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();

        _vertexBuffer = new VertexBuffer(_graphicsDevice, typeof(VertexPositionColor), vertices.Count, BufferUsage.WriteOnly);
        _vertexBuffer.SetData(vertices.ToArray());

        _indexBuffer = new IndexBuffer(_graphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.WriteOnly);
        _indexBuffer.SetData(indices.ToArray());

        _primitiveCount = indices.Count / 3;
        _geometryInvalidated = false;

        _logger.Debug("Chunk mesh built: {Vertices} vertices, {Primitives} triangles", vertices.Count, _primitiveCount);
    }

    private bool ShouldRenderFace(int x, int y, int z, BlockSide side)
    {
        var offset = NeighborOffsets[side];
        var nx = x + offset.X;
        var ny = y + offset.Y;
        var nz = z + offset.Z;

        if (nx >= 0 && nx < ChunkEntity.Width && ny >= 0 && ny < ChunkEntity.Height && nz >= 0 && nz < ChunkEntity.Depth)
        {
            return _chunk!.At(nx, ny, nz).BlockType == BlockType.Air;
        }

        return true;
    }

    private void AddFace(List<VertexPositionColor> vertices, List<int> indices, int x, int y, int z, BlockSide side, Color color)
    {
        var baseIndex = vertices.Count;
        var pos = new Vector3(x, y, z);

        Vector3[] faceVertices = side switch
        {
            BlockSide.Top => new[]
            {
                pos + new Vector3(0, 1, 0),
                pos + new Vector3(1, 1, 0),
                pos + new Vector3(1, 1, 1),
                pos + new Vector3(0, 1, 1)
            },
            BlockSide.Bottom => new[]
            {
                pos + new Vector3(0, 0, 1),
                pos + new Vector3(1, 0, 1),
                pos + new Vector3(1, 0, 0),
                pos + new Vector3(0, 0, 0)
            },
            BlockSide.North => new[]
            {
                pos + new Vector3(1, 0, 0),
                pos + new Vector3(1, 1, 0),
                pos + new Vector3(0, 1, 0),
                pos + new Vector3(0, 0, 0)
            },
            BlockSide.South => new[]
            {
                pos + new Vector3(0, 0, 1),
                pos + new Vector3(0, 1, 1),
                pos + new Vector3(1, 1, 1),
                pos + new Vector3(1, 0, 1)
            },
            BlockSide.East => new[]
            {
                pos + new Vector3(1, 0, 1),
                pos + new Vector3(1, 1, 1),
                pos + new Vector3(1, 1, 0),
                pos + new Vector3(1, 0, 0)
            },
            BlockSide.West => new[]
            {
                pos + new Vector3(0, 0, 0),
                pos + new Vector3(0, 1, 0),
                pos + new Vector3(0, 1, 1),
                pos + new Vector3(0, 0, 1)
            },
            _ => throw new ArgumentException("Invalid block side")
        };

        foreach (var vertex in faceVertices)
        {
            vertices.Add(new VertexPositionColor(vertex, color));
        }

        indices.Add(baseIndex);
        indices.Add(baseIndex + 1);
        indices.Add(baseIndex + 2);
        indices.Add(baseIndex);
        indices.Add(baseIndex + 2);
        indices.Add(baseIndex + 3);
    }

    private Color GetBlockColor(BlockType type)
    {
        return type switch
        {
            BlockType.Grass => Color.Green,
            BlockType.Dirt => new Color(139, 69, 19),
            BlockType.Stone => Color.Gray,
            BlockType.Water => Color.Blue,
            _ => Color.White
        };
    }

    /// <summary>
    /// Draws the chunk with the specified camera matrices.
    /// </summary>
    /// <param name="gameTime">The game time.</param>
    /// <param name="view">The view matrix.</param>
    /// <param name="projection">The projection matrix.</param>
    public void DrawWithCamera(GameTime gameTime, Matrix view, Matrix projection)
    {
        if (_chunk == null)
            return;

        if (_geometryInvalidated)
            BuildMeshImmediate();

        if (_vertexBuffer == null || _indexBuffer == null || _primitiveCount == 0)
            return;

        _effect.World = Matrix.CreateTranslation(Position);
        _effect.View = view;
        _effect.Projection = projection;
        _effect.FogEnabled = FogEnabled;
        _effect.FogColor = FogColor;
        _effect.FogStart = FogStart;
        _effect.FogEnd = FogEnd;

        _graphicsDevice.SetVertexBuffer(_vertexBuffer);
        _graphicsDevice.Indices = _indexBuffer;

        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _primitiveCount);
        }
    }

    /// <summary>
    /// Updates the chunk component.
    /// </summary>
    /// <param name="gameTime">The game time.</param>
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
    }

    public void Dispose()
    {
        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
    }
}
