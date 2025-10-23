using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SquidVox.Voxel.Primitives;

/// <summary>
/// Custom vertex structure for chunk solid geometry supporting texture tiling.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ChunkVertex : IVertexType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChunkVertex"/> struct.
    /// </summary>
    /// <param name="position">Vertex position.</param>
    /// <param name="color">Lighting color packed with face direction.</param>
    /// <param name="tileCoord">Unscaled tile coordinate used for tiling.</param>
    /// <param name="tileBase">Base UV of the atlas region.</param>
    /// <param name="tileSize">Size of the atlas region.</param>
    public ChunkVertex(
        Vector3 position,
        Color color,
        Vector2 tileCoord,
        Vector2 tileBase,
        Vector2 tileSize,
        Vector3 blockCoord)
    {
        Position = position;
        Color = color;
        TileCoord = tileCoord;
        TileBase = tileBase;
        TileSize = tileSize;
        BlockCoord = blockCoord;
    }

    /// <summary>
    /// Gets or sets the position of the vertex.
    /// </summary>
    public Vector3 Position;

    /// <summary>
    /// Gets or sets the color of the vertex (RGB lighting, A = face direction index).
    /// </summary>
    public Color Color;

    /// <summary>
    /// Gets or sets the tile coordinate used for texture tiling (in block units).
    /// </summary>
    public Vector2 TileCoord;

    /// <summary>
    /// Gets or sets the base UV (minimum corner) of the atlas region.
    /// </summary>
    public Vector2 TileBase;

    /// <summary>
    /// Gets or sets the size of the atlas region.
    /// </summary>
    public Vector2 TileSize;

    /// <summary>
    /// Gets or sets the block coordinate used for dynamic light sampling.
    /// </summary>
    public Vector3 BlockCoord;

    /// <summary>
    /// Gets the vertex declaration for the chunk vertex.
    /// </summary>
    public static readonly VertexDeclaration VertexDeclaration = new(
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0),
        new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1),
        new VertexElement(32, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 2),
        new VertexElement(40, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 3)
    );

    /// <inheritdoc />
    VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
}
