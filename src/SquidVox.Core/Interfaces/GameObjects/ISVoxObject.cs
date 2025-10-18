using System.Numerics;

namespace SquidVox.Core.Interfaces.GameObjects;

/// <summary>
/// Defines the contract for game objects in the SquidVox engine.
/// </summary>
public interface ISVoxObject
{
    /// <summary>
    /// Gets or sets the name of the game object.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Gets or sets the Z-index of the game object.
    /// </summary>
    int ZIndex { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the game object is enabled.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the game object is visible.
    /// </summary>
    bool IsVisible { get; set; }

    /// <summary>
    /// Gets or sets the scale of the game object.
    /// </summary>
    Vector2 Scale { get; set; }

    /// <summary>
    /// Gets or sets the rotation of the game object in radians.
    /// </summary>
    float Rotation { get; set; }

    /// <summary>
    /// Gets the children of this game object.
    /// </summary>
    IEnumerable<ISVoxObject> Children { get; }

    /// <summary>
    /// Adds a child game object.
    /// </summary>
    /// <param name="child">The child to add.</param>
    void AddChild(ISVoxObject child);

    /// <summary>
    /// Removes a child game object.
    /// </summary>
    /// <param name="child">The child to remove.</param>
    void RemoveChild(ISVoxObject child);

    /// <summary>
    /// Gets or sets the parent of this game object.
    /// </summary>
    ISVoxObject? Parent { get; set; }
}
