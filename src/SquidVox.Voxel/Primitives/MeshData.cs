using Microsoft.Xna.Framework.Graphics;

namespace SquidVox.Voxel.Primitives;

/// <summary>
/// Represents mesh data for a chunk, containing vertices, indices, and texture.
/// </summary>
public sealed class MeshData
{
    /// <summary>
    /// Gets or sets the vertices of the mesh.
    /// </summary>
    public ChunkVertex[] Vertices { get; set; } = [];

    /// <summary>
    /// Gets or sets the indices of the mesh.
    /// </summary>
    public int[] Indices { get; set; } = [];

    /// <summary>
    /// Gets or sets the texture of the mesh.
    /// </summary>
    public Texture2D? Texture { get; set; }

    /// <summary>
    /// Gets or sets the billboard vertices of the mesh.
    /// </summary>
    public VertexPositionColorTexture[] BillboardVertices { get; set; } = [];

    /// <summary>
    /// Gets or sets the billboard indices of the mesh.
    /// </summary>
    public int[] BillboardIndices { get; set; } = [];

    /// <summary>
    /// Gets or sets the fluid vertices of the mesh.
    /// </summary>
    public VertexPositionColorTextureDirectionTop[] FluidVertices { get; set; } = [];

    /// <summary>
    /// Gets or sets the fluid indices of the mesh.
    /// </summary>
    public int[] FluidIndices { get; set; } = [];
}
