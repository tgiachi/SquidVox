using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Xna.Framework.Graphics;

namespace SquidVox.Voxel.GameObjects;

/// <summary>
/// Object pool policy for billboard vertex lists.
/// </summary>
internal class BillboardVertexListPolicy : IPooledObjectPolicy<List<VertexPositionColorTexture>>
{
    /// <summary>
    /// Creates a new list instance.
    /// </summary>
    /// <returns>A new list with pre-allocated capacity.</returns>
    public List<VertexPositionColorTexture> Create()
    {
        return new List<VertexPositionColorTexture>(4096);
    }

    /// <summary>
    /// Returns an object to the pool.
    /// </summary>
    /// <param name="obj">The object to return.</param>
    /// <returns>True if the object can be returned to the pool.</returns>
    public bool Return(List<VertexPositionColorTexture> obj)
    {
        obj.Clear();
        return true;
    }
}