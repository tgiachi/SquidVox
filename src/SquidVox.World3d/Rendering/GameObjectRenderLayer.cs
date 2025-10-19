using FontStashSharp.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    /// <param name="gameObjects">The game object collection to render.</param>
    public GameObjectRenderLayer(SvoxGameObjectCollection<ISVox2dDrawableGameObject> gameObjects)
    {
        ArgumentNullException.ThrowIfNull(gameObjects);
        _gameObjects = gameObjects;
    }


    public void Render(SpriteBatch spriteBatch)
    {
        // Check for ZIndex changes before rendering
        _gameObjects.CheckForZIndexChanges();

        // Render all visible game objects using the optimized extension method
        _gameObjects.RenderAll(spriteBatch);
    }

    public void Update(GameTime gameTime)
    {
        // Check for ZIndex changes before updatating
        _gameObjects.CheckForZIndexChanges();

        _gameObjects.UpdateAll(gameTime);
    }
}
