using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SquidVox.Voxel.Primitives;

/// <summary>
/// Custom vertex format for fluid rendering with direction and top flag.
/// </summary>
public struct VertexPositionColorTextureDirectionTop : IVertexType
{
    /// <summary>
    /// Gets or sets the vertex position.
    /// </summary>
    public Vector3 Position;

    /// <summary>
    /// Gets or sets the vertex color.
    /// </summary>
    public Color Color;

    /// <summary>
    /// Gets or sets the texture coordinates.
    /// </summary>
    public Vector2 TextureCoordinate;

    /// <summary>
    /// Gets or sets the face direction (0-6).
    /// </summary>
    public float Direction;

    /// <summary>
    /// Gets or sets whether this vertex is on top surface (1.0 = top, 0.0 = not top).
    /// </summary>
    public float Top;

    /// <summary>
    /// Vertex declaration for this vertex type.
    /// </summary>
    public static readonly VertexDeclaration VertexDeclaration = new(
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0),
        new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(24, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 1),
        new VertexElement(28, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 2)
    );

    /// <summary>
    /// Initializes a new instance of the VertexPositionColorTextureDirectionTop struct.
    /// </summary>
    public VertexPositionColorTextureDirectionTop(Vector3 position, Color color, Vector2 textureCoordinate, float direction, float top)
    {
        Position = position;
        Color = color;
        TextureCoordinate = textureCoordinate;
        Direction = direction;
        Top = top;
    }

    VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
}
