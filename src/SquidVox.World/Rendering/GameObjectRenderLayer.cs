using FontStashSharp.Interfaces;
using SquidVox.Core.Collections;
using SquidVox.Core.Enums;
using SquidVox.Core.Extensions.Collections;
using SquidVox.Core.Interfaces.GameObjects;
using SquidVox.Core.Interfaces.Rendering;
using TrippyGL;

namespace SquidVox.World.Rendering;

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

    /// <summary>
    /// Renders all visible game objects from the collection.
    /// Uses the optimized RenderAll extension method for best performance.
    /// </summary>
    /// <param name="textureBatcher">TextureBatcher for rendering textures.</param>
    /// <param name="fontRenderer">Font renderer for drawing text.</param>
    public void Render(TextureBatcher textureBatcher, IFontStashRenderer fontRenderer)
    {
        // Check for ZIndex changes before rendering
        _gameObjects.CheckForZIndexChanges();

        // Render all visible game objects using the optimized extension method
        _gameObjects.RenderAll(textureBatcher, fontRenderer);
    }
}
