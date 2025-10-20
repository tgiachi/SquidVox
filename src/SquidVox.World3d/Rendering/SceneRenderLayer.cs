using DryIoc;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SquidVox.Core.Enums;
using SquidVox.Core.Interfaces.GameObjects;
using SquidVox.Core.Interfaces.Rendering;
using SquidVox.Core.Interfaces.Services;
using SquidVox.World3d.Context;

namespace SquidVox.World3d.Rendering;

/// <summary>
/// Render layer for the scene manager.
/// Renders the current scene and transitions at the World2D layer priority.
/// </summary>
public class SceneRenderLayer : IRenderableLayer
{
    private readonly ISceneManager _sceneManager;

    public bool HasFocus { get; set; }

    /// <summary>
    /// Gets the rendering layer priority.
    /// </summary>
    public RenderLayer Layer => RenderLayer.World2D;

    /// <summary>
    /// Gets or sets whether this layer is enabled for rendering.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///
    /// </summary>
    public void Render(SpriteBatch spriteBatch)
    {
        _sceneManager.Render(spriteBatch);
    }

    /// <summary>
    ///
    /// </summary>
    public void Update(GameTime gameTime)
    {
        _sceneManager.Update(gameTime);
    }

    /// <summary>
    /// Handles keyboard input for the scene.
    /// </summary>
    /// <param name="keyboardState">The current keyboard state.</param>
    /// <param name="gameTime">Game timing information.</param>
    public void HandleKeyboard(KeyboardState keyboardState, GameTime gameTime)
    {
        // Propagate to scene manager if it supports input
        if (_sceneManager is ISVoxInputReceiver inputReceiver && inputReceiver.HasFocus)
        {
            inputReceiver.HandleKeyboard(keyboardState, gameTime);
        }
    }

    /// <summary>
    /// Handles mouse input for the scene.
    /// </summary>
    /// <param name="mouseState">The current mouse state.</param>
    /// <param name="gameTime">Game timing information.</param>
    public void HandleMouse(MouseState mouseState, GameTime gameTime)
    {
        // Propagate to scene manager if it supports input
        if (_sceneManager is ISVoxInputReceiver inputReceiver && inputReceiver.HasFocus)
        {
            inputReceiver.HandleMouse(mouseState, gameTime);
        }
    }

    /// <summary>
    /// Initializes a new instance of the SceneRenderLayer class.
    /// </summary>
    /// <param name="container">The IoC container to resolve dependencies.</param>
    public SceneRenderLayer(IContainer container)
    {
        _sceneManager = container.Resolve<ISceneManager>();
    }
}
