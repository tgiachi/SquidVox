using FontStashSharp.Interfaces;
using SquidVox.Core.Enums;
using TrippyGL;

namespace SquidVox.Core.Interfaces.Rendering;

/// <summary>
/// Defines a renderable layer in the rendering pipeline.
/// Layers are rendered in order based on their RenderLayer priority.
/// </summary>
public interface IRenderableLayer
{
    /// <summary>
    /// Gets the rendering layer priority.
    /// </summary>
    RenderLayer Layer { get; }

    /// <summary>
    /// Gets or sets whether this layer is enabled for rendering.
    /// </summary>
    bool Enabled { get; set; }

    /// <summary>
    /// Renders the layer content.
    /// </summary>
    /// <param name="textureBatcher">TextureBatcher for rendering textures.</param>
    /// <param name="fontRenderer">Font renderer for drawing text.</param>
    void Render(TextureBatcher textureBatcher, IFontStashRenderer fontRenderer);
}
