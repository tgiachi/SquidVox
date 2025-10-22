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
/// Render layer for 3D game objects.
/// Renders all visible 3D game objects from a SvoxGameObjectCollection at the World3D layer priority.
/// </summary>
public class GameObject3dRenderLayer : IRenderableLayer
{
    private readonly SvoxGameObjectCollection<ISVox3dDrawableGameObject> _gameObjects;

    /// <summary>
    /// Gets the rendering layer priority.
    /// </summary>
    public RenderLayer Layer => RenderLayer.World3D;

    /// <summary>
    /// Gets or sets whether this layer is enabled for rendering.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether this layer has input focus.
    /// </summary>
    public bool HasFocus { get; set; }

    /// <summary>
    /// Initializes a new instance of the GameObject3dRenderLayer class.
    /// </summary>
    public GameObject3dRenderLayer()
    {
        _gameObjects = new SvoxGameObjectCollection<ISVox3dDrawableGameObject>();
    }

    /// <summary>
    /// Adds a 3D game object to the render layer.
    /// </summary>
    /// <param name="gameObject">The 3D game object to add.</param>
    public void AddGameObject(ISVox3dDrawableGameObject gameObject)
    {
        _gameObjects.Add(gameObject);
    }

    /// <summary>
    /// Removes a 3D game object from the render layer.
    /// </summary>
    /// <param name="gameObject">The 3D game object to remove.</param>
    /// <returns>True if the game object was removed, false otherwise.</returns>
    public bool RemoveGameObject(ISVox3dDrawableGameObject gameObject)
    {
        return _gameObjects.Remove(gameObject);
    }

    /// <summary>
    /// Gets the first game object of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of game object to find.</typeparam>
    /// <returns>The first game object of the specified type, or null if not found.</returns>
    public T? GetComponent<T>() where T : class, ISVox3dDrawableGameObject
    {
        return _gameObjects.GetFirstGameObjectOfType<T>();
    }

    /// <summary>
    /// Gets all game objects of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of game objects to find.</typeparam>
    /// <returns>An enumerable of game objects of the specified type.</returns>
    public IEnumerable<T> GetComponents<T>() where T : class, ISVox3dDrawableGameObject
    {
        return _gameObjects.GetGameObjectsOfType<T>();
    }

    /// <summary>
    /// Renders all visible 3D game objects in the layer.
    /// </summary>
    /// <param name="spriteBatch">The sprite batch (unused for 3D rendering).</param>
    public void Render(SpriteBatch spriteBatch)
    {
        _gameObjects.CheckForZIndexChanges();

        _gameObjects.DrawAll(new GameTime());
    }

    /// <summary>
    /// Updates all enabled 3D game objects in the layer.
    /// </summary>
    /// <param name="gameTime">The game time.</param>
    public void Update(GameTime gameTime)
    {
        _gameObjects.CheckForZIndexChanges();

        for (var i = 0; i < _gameObjects.Count; i++)
        {
            var gameObject = _gameObjects[i];
            if (gameObject.IsEnabled)
            {
                gameObject.Update(gameTime);
            }
        }
    }

    /// <summary>
    /// Handles keyboard input for all 3D game objects that have focus.
    /// </summary>
    /// <param name="keyboardState">The keyboard state.</param>
    /// <param name="gameTime">The game time.</param>
    public void HandleKeyboard(KeyboardState keyboardState, GameTime gameTime)
    {
        foreach (var gameObject in _gameObjects)
        {
            if (gameObject is ISVoxInputReceiver inputReceiver && inputReceiver.HasFocus)
            {
                inputReceiver.HandleKeyboard(keyboardState, gameTime);
            }
        }
    }

    /// <summary>
    /// Handles mouse input for all 3D game objects that have focus.
    /// </summary>
    /// <param name="mouseState">The mouse state.</param>
    /// <param name="gameTime">The game time.</param>
    public void HandleMouse(MouseState mouseState, GameTime gameTime)
    {
        foreach (var gameObject in _gameObjects)
        {
            if (gameObject is ISVoxInputReceiver inputReceiver && inputReceiver.HasFocus)
            {
                inputReceiver.HandleMouse(mouseState, gameTime);
            }
        }
    }

}
