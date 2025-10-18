using FontStashSharp.Interfaces;
using SquidVox.Core.Enums;
using SquidVox.Core.Interfaces.Rendering;
using SquidVox.Core.Interfaces.Services;
using TrippyGL;

namespace SquidVox.World.Rendering;

/// <summary>
/// Render layer for the scene manager.
/// Renders the current scene and transitions at the World2D layer priority.
/// </summary>
public class SceneRenderLayer : IRenderableLayer
{
    private readonly ISceneManager _sceneManager;

    /// <summary>
    /// Gets the rendering layer priority.
    /// </summary>
    public RenderLayer Layer => RenderLayer.World2D;

    /// <summary>
    /// Gets or sets whether this layer is enabled for rendering.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the SceneRenderLayer class.
    /// </summary>
    /// <param name="sceneManager">The scene manager to render.</param>
    public SceneRenderLayer(ISceneManager sceneManager)
    {
        _sceneManager = sceneManager;
    }

    /// <summary>
    /// Renders the current scene or transition.
    /// </summary>
    /// <param name="textureBatcher">TextureBatcher for rendering textures.</param>
    /// <param name="fontRenderer">Font renderer for drawing text.</param>
    public void Render(TextureBatcher textureBatcher, IFontStashRenderer fontRenderer)
    {
        _sceneManager.Render(textureBatcher, fontRenderer);
    }
}
