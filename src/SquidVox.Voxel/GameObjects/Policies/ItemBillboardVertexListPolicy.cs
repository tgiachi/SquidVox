using Microsoft.Extensions.ObjectPool;
using SquidVox.Voxel.Primitives;

namespace SquidVox.Voxel.GameObjects.Policies;

/// <summary>
/// Object pool policy for item billboard vertex lists.
/// </summary>
internal class ItemBillboardVertexListPolicy : IPooledObjectPolicy<List<VertexBillboardItem>>
{
    /// <summary>
    /// Creates a new list instance.
    /// </summary>
    /// <returns>A new list with pre-allocated capacity.</returns>
    public List<VertexBillboardItem> Create()
    {
        return new List<VertexBillboardItem>(1024);
    }

    /// <summary>
    /// Returns an object to the pool.
    /// </summary>
    /// <param name="obj">The object to return.</param>
    /// <returns>True if the object can be returned to the pool.</returns>
    public bool Return(List<VertexBillboardItem> obj)
    {
        obj.Clear();
        return true;
    }
}