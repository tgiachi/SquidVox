using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SquidVox.Voxel.Primitives;

/// <summary>
/// Vertex type for camera-facing billboard items.
/// </summary>
public struct VertexBillboardItem : IVertexType
{
    /// <summary>
    /// Gets or sets the vertex position representing the item center.
    /// </summary>
    public Vector3 Position;

    /// <summary>
    /// Gets or sets the vertex color multiplier.
    /// </summary>
    public Color Color;

    /// <summary>
    /// Gets or sets the texture coordinates.
    /// </summary>
    public Vector2 TextureCoordinate;

    /// <summary>
    /// Gets or sets the vertex offset from the item center in billboard space.
    /// </summary>
    public Vector2 Offset;

    /// <summary>
    /// Vertex declaration for this vertex type.
    /// </summary>
    public static readonly VertexDeclaration VertexDeclaration = new(
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0),
        new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1)
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="VertexBillboardItem"/> struct.
    /// </summary>
    public VertexBillboardItem(Vector3 position, Color color, Vector2 textureCoordinate, Vector2 offset)
    {
        Position = position;
        Color = color;
        TextureCoordinate = textureCoordinate;
        Offset = offset;
    }

    VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
}
