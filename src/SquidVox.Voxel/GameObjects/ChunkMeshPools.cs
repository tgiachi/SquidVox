using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Graphics;
using SquidVox.Voxel.GameObjects.Policies;
using SquidVox.Voxel.Primitives;

namespace SquidVox.Voxel.GameObjects;

/// <summary>
/// Centralized object pools for chunk mesh data to reduce GC pressure.
/// </summary>
public static class ChunkMeshPools
{
    /// <summary>
    /// Pool for solid block chunk vertices.
    /// </summary>
    public static readonly ObjectPool<List<ChunkVertex>> ChunkVertexPool =
        ObjectPool.Create(new ChunkVertexListPolicy());

    /// <summary>
    /// Pool for billboard vertices (e.g., grass, flowers).
    /// </summary>
    public static readonly ObjectPool<List<VertexPositionColorTexture>> BillboardVertexPool =
        ObjectPool.Create(new BillboardVertexListPolicy());

    /// <summary>
    /// Pool for item billboard vertices.
    /// </summary>
    public static readonly ObjectPool<List<VertexBillboardItem>> ItemVertexPool =
        ObjectPool.Create(new ItemBillboardVertexListPolicy());

    /// <summary>
    /// Pool for index buffers.
    /// </summary>
    public static readonly ObjectPool<List<int>> IndexPool =
        ObjectPool.Create(new IndexListPolicy());

    /// <summary>
    /// Pool for fluid vertices (e.g., water, lava).
    /// </summary>
    public static readonly ObjectPool<List<VertexPositionColorTextureDirectionTop>> FluidVertexPool =
        ObjectPool.Create(new FluidVertexListPolicy());
}
