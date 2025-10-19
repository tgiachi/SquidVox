using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SquidVox.Core.Enums;
using SquidVox.Core.Interfaces.Rendering;
using SquidVox.Core.Interfaces.Services;

namespace SquidVox.World3d.Rendering;

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

    public void Render(SpriteBatch spriteBatch)
    {
        _sceneManager.Render(spriteBatch);
    }

    public void Update(GameTime gameTime)
    {
        _sceneManager.Update(gameTime);
    }

    /// <summary>
    /// Initializes a new instance of the SceneRenderLayer class.
    /// </summary>
    /// <param name="sceneManager">The scene manager to render.</param>
    public SceneRenderLayer(ISceneManager sceneManager)
    {
        _sceneManager = sceneManager;
    }

}
