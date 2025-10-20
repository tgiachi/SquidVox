using FontStashSharp.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SquidVox.Core.Collections;
using SquidVox.Core.Enums;
using SquidVox.Core.Extensions.Collections;
using SquidVox.Core.Interfaces.GameObjects;
using SquidVox.Core.Interfaces.Rendering;

namespace SquidVox.World3d.Rendering;

/// <summary>
/// Render layer for game objects.
/// Renders all visible game objects from a SvoxGameObjectCollection at the World2D layer priority.
/// </summary>
public class GameObjectRenderLayer : IRenderableLayer
{
    private readonly SvoxGameObjectCollection<ISVox2dDrawableGameObject> _gameObjects;

    /// <summary>
    /// Gets the rendering layer priority.
    /// </summary>
    public RenderLayer Layer => RenderLayer.World2D;

    /// <summary>
    /// Gets or sets whether this layer is enabled for rendering.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the GameObjectRenderLayer class.
    /// </summary>
    public GameObjectRenderLayer()
    {
        _gameObjects = new SvoxGameObjectCollection<ISVox2dDrawableGameObject>();
    }

    /// <summary>
    /// Adds a game object to the render layer.
    /// </summary>
    /// <param name="gameObject">The game object to add.</param>
    public void AddGameObject(ISVox2dDrawableGameObject gameObject)
    {
        _gameObjects.Add(gameObject);
    }

    /// <summary>
    /// Removes a game object from the render layer.
    /// </summary>
    /// <param name="gameObject">The game object to remove.</param>
    /// <returns>True if the game object was removed, false otherwise.</returns>
    public bool RemoveGameObject(ISVox2dDrawableGameObject gameObject)
    {
        return _gameObjects.Remove(gameObject);
    }

    /// <summary>
    ///
    /// </summary>
    public void Render(SpriteBatch spriteBatch)
    {
        // Check for ZIndex changes before rendering
        _gameObjects.CheckForZIndexChanges();

        // Render all visible game objects using the optimized extension method
        _gameObjects.RenderAll(spriteBatch);
    }

    /// <summary>
    ///
    /// </summary>
    public void Update(GameTime gameTime)
    {
        // Check for ZIndex changes before updatating
        _gameObjects.CheckForZIndexChanges();

        _gameObjects.UpdateAll(gameTime);
    }

    public void HandleKeyboard(KeyboardState keyboardState, GameTime gameTime)
    {
        // Propagate keyboard input to game objects that can receive input
        foreach (var gameObject in _gameObjects)
        {
            if (gameObject is ISVoxInputReceiver inputReceiver && inputReceiver.HasFocus)
            {
                inputReceiver.HandleKeyboard(keyboardState, gameTime);
            }
        }
    }

    public void HandleMouse(MouseState mouseState, GameTime gameTime)
    {
        // Propagate mouse input to game objects that can receive input
        foreach (var gameObject in _gameObjects)
        {
            if (gameObject is ISVoxInputReceiver inputReceiver && inputReceiver.HasFocus)
            {
                inputReceiver.HandleMouse(mouseState, gameTime);
            }
        }
    }

    /// <summary>
    /// Gets or sets whether this layer has input focus.
    /// </summary>
    public bool HasFocus { get; set; }
}
