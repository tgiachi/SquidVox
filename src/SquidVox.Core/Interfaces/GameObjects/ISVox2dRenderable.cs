using System.Numerics;
using FontStashSharp.Interfaces;
using TrippyGL;

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
    /// use fontRenderer to draw text and textureBatcher to draw textures
    /// </summary>
    /// <param name="textureBatcher"></param>
    /// <param name="fontRenderer"></param>
    void Render(TextureBatcher textureBatcher, IFontStashRenderer fontRenderer);
}
