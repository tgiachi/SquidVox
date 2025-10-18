using FontStashSharp.Interfaces;
using TrippyGL;

namespace SquidVox.Core.Interfaces.GameObjects;

/// <summary>
/// Defines the contract for 2D renderable objects in the SquidVox engine.
/// </summary>
public interface ISVox2dRenderable
{
    /// <summary>
    /// use fontRenderer to draw text and textureBatcher to draw textures
    /// </summary>
    /// <param name="textureBatcher"></param>
    /// <param name="fontRenderer"></param>
    void Render(TextureBatcher textureBatcher, IFontStashRenderer fontRenderer);
}
