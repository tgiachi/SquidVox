using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;
using SquidVox.Voxel.Primitives;

namespace SquidVox.Voxel.GameObjects;

/// <summary>
/// Object pool policy for fluid vertex lists.
/// </summary>
internal class FluidVertexListPolicy : IPooledObjectPolicy<List<VertexPositionColorTextureDirectionTop>>
{
    /// <summary>
    /// Creates a new list instance.
    /// </summary>
    /// <returns>A new list with pre-allocated capacity.</returns>
    public List<VertexPositionColorTextureDirectionTop> Create()
    {
        return new List<VertexPositionColorTextureDirectionTop>(16384);
    }

    /// <summary>
    /// Returns an object to the pool.
    /// </summary>
    /// <param name="obj">The object to return.</param>
    /// <returns>True if the object can be returned to the pool.</returns>
    public bool Return(List<VertexPositionColorTextureDirectionTop> obj)
    {
        obj.Clear();
        return true;
    }
}