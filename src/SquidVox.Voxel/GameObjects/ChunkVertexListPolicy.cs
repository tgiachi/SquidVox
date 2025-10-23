using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;
using SquidVox.Voxel.Primitives;

namespace SquidVox.Voxel.GameObjects;

/// <summary>
/// Object pool policy for chunk vertex lists.
/// </summary>
internal class ChunkVertexListPolicy : IPooledObjectPolicy<List<ChunkVertex>>
{
    /// <summary>
    /// Creates a new list instance.
    /// </summary>
    /// <returns>A new list with pre-allocated capacity.</returns>
    public List<ChunkVertex> Create()
    {
        return new List<ChunkVertex>(16384); // Pre-allocate capacity for typical chunk
    }

    /// <summary>
    /// Returns an object to the pool.
    /// </summary>
    /// <param name="obj">The object to return.</param>
    /// <returns>True if the object can be returned to the pool.</returns>
    public bool Return(List<ChunkVertex> obj)
    {
        obj.Clear();
        return true;
    }
}