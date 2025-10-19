using FontStashSharp.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SquidVox.Core.Interfaces.GameObjects;

/// <summary>
/// Defines the contract for 2D renderable objects in the SquidVox engine.
/// </summary>
public interface ISVox2dRenderable
{
    /// <summary>
    /// Gets or sets the position of the 2D object (local position relative to parent).
    /// </summary>
    Vector2 Position { get; set; }

    /// <summary>
    /// Gets or sets the scale of the 2D object.
    /// </summary>
    Vector2 Scale { get; set; }

    /// <summary>
    /// Gets or sets the rotation of the 2D object in radians.
    /// </summary>
    float Rotation { get; set; }

    /// <summary>
    /// Gets or sets the size of the 2D object (used for scissor clipping).
    /// If Zero, no scissor clipping is applied.
    /// </summary>
    Vector2 Size { get; set; }

    /// <summary>
    /// use spriteBatch to draw textures
    /// </summary>
    /// <param name="spriteBatch"></param>
    void Render(SpriteBatch spriteBatch);
}
