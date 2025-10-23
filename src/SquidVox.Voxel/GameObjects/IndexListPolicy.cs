using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;

namespace SquidVox.Voxel.GameObjects;

/// <summary>
/// Object pool policy for index lists.
/// </summary>
internal class IndexListPolicy : IPooledObjectPolicy<List<int>>
{
    /// <summary>
    /// Creates a new list instance.
    /// </summary>
    /// <returns>A new list with pre-allocated capacity.</returns>
    public List<int> Create()
    {
        return new List<int>(24576);
    }

    /// <summary>
    /// Returns an object to the pool.
    /// </summary>
    /// <param name="obj">The object to return.</param>
    /// <returns>True if the object can be returned to the pool.</returns>
    public bool Return(List<int> obj)
    {
        obj.Clear();
        return true;
    }
}