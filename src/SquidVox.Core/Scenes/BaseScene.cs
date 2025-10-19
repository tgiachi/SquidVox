using FontStashSharp.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SquidVox.Core.Collections;
using SquidVox.Core.Extensions.Collections;
using SquidVox.Core.Interfaces.GameObjects;
using SquidVox.Core.Interfaces.Scenes;

namespace SquidVox.Core.Scenes;

/// <summary>
/// Abstract base class for scenes, providing default implementations for common functionality.
/// </summary>
public abstract class BaseScene : ISVoxScene
{
    /// <summary>
    /// Gets or sets the name of the scene.
    /// </summary>
    public virtual string Name { get; protected set; } = "Unnamed Scene";

    /// <summary>
    /// Gets or sets the position (not used for scenes, included for ISVox2dRenderable compliance).
    /// </summary>
    public virtual Vector2 Position { get; set; } = Vector2.Zero;

    /// <summary>
    /// Gets or sets the scale (not used for scenes, included for ISVox2dRenderable compliance).
    /// </summary>
    public virtual Vector2 Scale { get; set; } = Vector2.One;

    /// <summary>
    /// Gets or sets the rotation (not used for scenes, included for ISVox2dRenderable compliance).
    /// </summary>
    public virtual float Rotation { get; set; }

    /// <summary>
    /// Gets or sets the size (not used for scenes, included for ISVox2dRenderable compliance).
    /// </summary>
    public virtual Vector2 Size { get; set; } = Vector2.Zero;

    /// <summary>
    /// Gets the collection of game objects in this scene.
    /// </summary>
    public SvoxGameObjectCollection<ISVox2dDrawableGameObject> Components { get; }

    /// <summary>
    /// Gets or sets whether this scene has input focus.
    /// </summary>
    public virtual bool HasFocus { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the scene is loaded.
    /// </summary>
    protected bool IsLoaded { get; set; }

    /// <summary>
    /// Initializes a new instance of the Scene class.
    /// </summary>
    protected BaseScene()
    {
        Components = [];
    }

    /// <summary>
    /// Initializes a new instance of the Scene class with a specified capacity.
    /// </summary>
    /// <param name="capacity">Initial capacity for the components collection.</param>
    protected BaseScene(int capacity)
    {
        Components = new SvoxGameObjectCollection<ISVox2dDrawableGameObject>(capacity);
    }

    /// <summary>
    /// Called when the scene is loaded and becomes active.
    /// Override this to initialize scene-specific resources.
    /// </summary>
    public virtual void Load()
    {
        IsLoaded = true;
        OnLoad();
    }

    /// <summary>
    /// Called when the scene is unloaded and becomes inactive.
    /// Override this to cleanup scene-specific resources.
    /// </summary>
    public virtual void Unload()
    {
        IsLoaded = false;
        OnUnload();
        Components.Clear();
    }

    /// <summary>
    /// Updates the scene and all its components.
    /// </summary>
    /// <param name="gameTime">Game timing information.</param>
    public virtual void Update(Microsoft.Xna.Framework.GameTime gameTime)
    {
        if (!IsLoaded)
        {
            return;
        }

        OnUpdate(gameTime);
        Components.UpdateAll(gameTime);
    }

    /// <summary>
    /// Renders the scene and all its components.
    /// </summary>
    /// <param name="spriteBatch">SpriteBatch for rendering textures.</param>
    public virtual void Render(SpriteBatch spriteBatch)
    {
        if (!IsLoaded)
        {
            return;
        }

        OnRenderBegin(spriteBatch);
        Components.RenderAll(spriteBatch);
        OnRenderEnd(spriteBatch);
    }

    /// <summary>
    /// Handles keyboard input when the scene has focus.
    /// </summary>
    /// <param name="keyboardState">The current keyboard state.</param>
    /// <param name="gameTime">Game timing information.</param>
    public virtual void HandleKeyboard(KeyboardState keyboardState, GameTime gameTime)
    {
        if (!HasFocus || !IsLoaded)
        {
            return;
        }

        OnHandleKeyboard(keyboardState, gameTime);
        Components.HandleKeyboardInput(keyboardState, gameTime);
    }

    /// <summary>
    /// Handles mouse input when the scene has focus.
    /// </summary>
    /// <param name="mouseState">The current mouse state.</param>
    /// <param name="gameTime">Game timing information.</param>
    public virtual void HandleMouse(MouseState mouseState, GameTime gameTime)
    {
        if (!HasFocus || !IsLoaded)
        {
            return;
        }

        OnHandleMouse(mouseState, gameTime);
        Components.HandleMouseInput(mouseState, gameTime);
    }

    /// <summary>
    /// Called during Load() for scene-specific initialization.
    /// Override this instead of Load() to add custom initialization logic.
    /// </summary>
    protected virtual void OnLoad()
    {
    }

    /// <summary>
    /// Called during Unload() for scene-specific cleanup.
    /// Override this instead of Unload() to add custom cleanup logic.
    /// </summary>
    protected virtual void OnUnload()
    {
    }

    /// <summary>
    /// Called during Update() before components are updated.
    /// Override this to add custom update logic.
    /// </summary>
    /// <param name="gameTime">Game timing information.</param>
    protected virtual void OnUpdate(Microsoft.Xna.Framework.GameTime gameTime)
    {
    }

    /// <summary>
    /// Called during Render() before components are rendered.
    /// Override this to add custom rendering logic that should appear behind components.
    /// </summary>
    /// <param name="spriteBatch">SpriteBatch for rendering textures.</param>
    protected virtual void OnRenderBegin(SpriteBatch spriteBatch)
    {
    }

    /// <summary>
    /// Called during Render() after components are rendered.
    /// Override this to add custom rendering logic that should appear in front of components.
    /// </summary>
    /// <param name="spriteBatch">SpriteBatch for rendering textures.</param>
    protected virtual void OnRenderEnd(SpriteBatch spriteBatch)
    {
    }

    /// <summary>
    /// Called during HandleKeyboard() before components handle keyboard input.
    /// Override this to add custom keyboard handling logic.
    /// </summary>
    /// <param name="keyboardState">The current keyboard state.</param>
    /// <param name="gameTime">Game timing information.</param>
    protected virtual void OnHandleKeyboard(KeyboardState keyboardState, Microsoft.Xna.Framework.GameTime gameTime)
    {
    }

    /// <summary>
    /// Called during HandleMouse() before components handle mouse input.
    /// Override this to add custom mouse handling logic.
    /// </summary>
    /// <param name="mouseState">The current mouse state.</param>
    /// <param name="gameTime">Game timing information.</param>
    protected virtual void OnHandleMouse(MouseState mouseState, Microsoft.Xna.Framework.GameTime gameTime)
    {
    }
}
