using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SquidVox.Core.Interfaces.GameObjects;

/// <summary>
/// Defines the contract for 3D renderable objects in the SquidVox engine.
/// </summary>
public interface ISVox3dRenderable
{
    /// <summary>
    /// Gets or sets the position of the 3D object (local position relative to parent).
    /// </summary>
    Vector3 Position { get; set; }

    /// <summary>
    /// Gets or sets the scale of the 3D object.
    /// </summary>
    Vector3 Scale { get; set; }

    /// <summary>
    /// Gets or sets the rotation of the 3D object as a quaternion.
    /// </summary>
    Quaternion Rotation { get; set; }

    /// <summary>
    /// Renders the 3D object using the provided graphics device.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device for rendering.</param>
    void Render(GraphicsDevice graphicsDevice);
}
