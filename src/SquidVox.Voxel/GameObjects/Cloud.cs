using Microsoft.Xna.Framework;

namespace SquidVox.Voxel.GameObjects;

/// <summary>
/// Represents a single cloud instance with position and size.
/// </summary>
public struct Cloud
{
    /// <summary>
    /// Gets or sets the position of the cloud.
    /// </summary>
    public Vector3 Position { get; set; }

    /// <summary>
    /// Gets or sets the size of the cloud.
    /// </summary>
    public Vector3 Size { get; set; }

    /// <summary>
    /// Initializes a new instance of the Cloud struct.
    /// </summary>
    public Cloud(Vector3 position, Vector3 size)
    {
        Position = position;
        Size = size;
    }
}