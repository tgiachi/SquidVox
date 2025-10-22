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
    /// Gets or sets the rotation of the 3D object as a vector (Yaw, Pitch, Roll in radians).
    /// </summary>
    Vector3 Rotation { get; set; }

    /// <summary>
    /// Gets or sets the opacity of the 3D object (0.0 to 1.0).
    /// </summary>
    float Opacity { get; set; }

    /// <summary>
    /// Draws the 3D object.
    /// </summary>
    /// <param name="gameTime">Game timing information.</param>
    void Draw3d(GameTime gameTime);
}
