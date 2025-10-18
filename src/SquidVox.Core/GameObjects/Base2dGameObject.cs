using System.Numerics;
using FontStashSharp.Interfaces;
using Silk.NET.Input;
using SquidVox.Core.Collections;
using SquidVox.Core.Data.Graphics;
using SquidVox.Core.Extensions.Collections;
using SquidVox.Core.Interfaces.GameObjects;
using TrippyGL;

namespace SquidVox.Core.GameObjects;

/// <summary>
/// Abstract base class for game objects, providing default implementations for common functionality.
/// </summary>
public abstract class Base2dGameObject : ISVox2dDrawableGameObject, ISVoxInputReceiver
{
    private readonly SvoxGameObjectCollection<ISVox2dDrawableGameObject> _children = [];

    /// <summary>
    /// Gets or sets the name of the game object.
    /// </summary>
    public virtual string Name { get; set; } = "Unnamed GameObject";

    /// <summary>
    /// Gets or sets the Z-index of the game object (for rendering order).
    /// </summary>
    public virtual int ZIndex { get; set; }

    /// <summary>
    /// Gets or sets whether the game object is enabled (affects updates and input).
    /// </summary>
    public virtual bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the game object is visible (affects rendering).
    /// </summary>
    public virtual bool IsVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets the local position of the game object (relative to parent).
    /// </summary>
    public virtual Vector2 Position { get; set; } = Vector2.Zero;

    /// <summary>
    /// Gets or sets the scale of the game object.
    /// </summary>
    public virtual Vector2 Scale { get; set; } = Vector2.One;

    /// <summary>
    /// Gets or sets the rotation of the game object in radians.
    /// </summary>
    public virtual float Rotation { get; set; }

    /// <summary>
    /// Gets the children of this game object.
    /// </summary>
    public virtual IEnumerable<ISVoxObject> Children => _children;

    /// <summary>
    /// Gets or sets the parent of this game object.
    /// </summary>
    public virtual ISVoxObject? Parent { get; set; }

    /// <summary>
    /// Gets or sets whether this game object has input focus.
    /// </summary>
    public virtual bool HasFocus { get; set; }

    /// <summary>
    /// Gets the absolute (world) position of the game object.
    /// This considers the position of all parent objects in the hierarchy.
    /// </summary>
    /// <returns>The absolute position in world space.</returns>
    public Vector2 GetAbsolutePosition()
    {
        if (Parent == null)
        {
            return Position;
        }

        // If parent is a BaseGameObject, use its GetAbsolutePosition
        if (Parent is Base2dGameObject parentGameObject)
        {
            return Position + parentGameObject.GetAbsolutePosition();
        }

        // If parent is a 2D renderable, try to get its position
        if (Parent is ISVox2dRenderable renderable2d)
        {
            return Position + renderable2d.Position;
        }

        // Parent doesn't have position, use only local position
        return Position;
    }

    /// <summary>
    /// Gets the absolute (world) scale of the game object.
    /// This considers the scale of all parent objects in the hierarchy.
    /// </summary>
    /// <returns>The absolute scale in world space.</returns>
    public Vector2 GetAbsoluteScale()
    {
        if (Parent == null)
        {
            return Scale;
        }

        // If parent is a BaseGameObject, use its GetAbsoluteScale
        if (Parent is Base2dGameObject parentGameObject)
        {
            return Scale * parentGameObject.GetAbsoluteScale();
        }

        // If parent is a 2D renderable, try to get its scale
        if (Parent is ISVox2dRenderable renderable2d)
        {
            return Scale * renderable2d.Scale;
        }

        // Parent doesn't have scale, use only local scale
        return Scale;
    }

    /// <summary>
    /// Gets the absolute (world) rotation of the game object.
    /// This considers the rotation of all parent objects in the hierarchy.
    /// </summary>
    /// <returns>The absolute rotation in world space (in radians).</returns>
    public float GetAbsoluteRotation()
    {
        if (Parent == null)
        {
            return Rotation;
        }

        // If parent is a BaseGameObject, use its GetAbsoluteRotation
        if (Parent is Base2dGameObject parentGameObject)
        {
            return Rotation + parentGameObject.GetAbsoluteRotation();
        }

        // If parent is a 2D renderable, try to get its rotation
        if (Parent is ISVox2dRenderable renderable2d)
        {
            return Rotation + renderable2d.Rotation;
        }

        // Parent doesn't have rotation, use only local rotation
        return Rotation;
    }

    /// <summary>
    /// Adds a child game object to this object's hierarchy.
    /// </summary>
    /// <param name="child">The child to add.</param>
    /// <exception cref="ArgumentException">Thrown when child is not an ISVox2dDrawableGameObject.</exception>
    public virtual void AddChild(ISVoxObject child)
    {
        ArgumentNullException.ThrowIfNull(child);

        if (child is not ISVox2dDrawableGameObject drawableChild)
        {
            throw new ArgumentException(
                $"BaseGameObject can only accept children of type ISVox2dDrawableGameObject. Received: {child.GetType().Name}",
                nameof(child));
        }

        if (_children.Contains(drawableChild))
        {
            return;
        }

        _children.Add(drawableChild);
        child.Parent = this;

        OnChildAdded(child);
    }

    /// <summary>
    /// Removes a child game object from this object's hierarchy.
    /// </summary>
    /// <param name="child">The child to remove.</param>
    public virtual void RemoveChild(ISVoxObject child)
    {
        if (child is not ISVox2dDrawableGameObject drawableChild)
        {
            return;
        }

        if (!_children.Remove(drawableChild))
        {
            return;
        }

        child.Parent = null;
        OnChildRemoved(child);
    }

    /// <summary>
    /// Updates the game object and all its children.
    /// </summary>
    /// <param name="gameTime">Game timing information.</param>
    public virtual void Update(GameTime gameTime)
    {
        if (!IsEnabled)
        {
            return;
        }

        OnUpdate(gameTime);

        // Update all enabled children using our optimized collection
        _children.UpdateAll(gameTime);
    }

    /// <summary>
    /// Renders the game object and all its visible children.
    /// </summary>
    /// <param name="textureBatcher">TextureBatcher for rendering textures.</param>
    /// <param name="fontRenderer">Font renderer for drawing text.</param>
    public virtual void Render(TextureBatcher textureBatcher, IFontStashRenderer fontRenderer)
    {
        if (!IsVisible)
        {
            return;
        }

        OnRender(textureBatcher, fontRenderer);

        // Render all visible children using our optimized collection
        _children.RenderAll(textureBatcher, fontRenderer);
    }

    /// <summary>
    /// Handles keyboard input for this game object and its children.
    /// </summary>
    /// <param name="keyboard">The keyboard device.</param>
    /// <param name="gameTime">Game timing information.</param>
    public virtual void HandleKeyboard(IKeyboard keyboard, GameTime gameTime)
    {
        if (!HasFocus || !IsEnabled)
        {
            return;
        }

        OnHandleKeyboard(keyboard, gameTime);

        // Propagate input to children using our optimized collection
        _children.HandleKeyboardInput(keyboard, gameTime);
    }

    /// <summary>
    /// Handles mouse input for this game object and its children.
    /// </summary>
    /// <param name="mouse">The mouse device.</param>
    /// <param name="gameTime">Game timing information.</param>
    public virtual void HandleMouse(IMouse mouse, GameTime gameTime)
    {
        if (!HasFocus || !IsEnabled)
        {
            return;
        }

        OnHandleMouse(mouse, gameTime);

        // Propagate input to children using our optimized collection
        _children.HandleMouseInput(mouse, gameTime);
    }

    /// <summary>
    /// Called during Update() before children are updated.
    /// Override this to add custom update logic.
    /// </summary>
    /// <param name="gameTime">Game timing information.</param>
    protected virtual void OnUpdate(GameTime gameTime)
    {
    }

    /// <summary>
    /// Called during Render() before children are rendered.
    /// Override this to add custom rendering logic.
    /// </summary>
    /// <param name="textureBatcher">TextureBatcher for rendering textures.</param>
    /// <param name="fontRenderer">Font renderer for drawing text.</param>
    protected virtual void OnRender(TextureBatcher textureBatcher, IFontStashRenderer fontRenderer)
    {
    }

    /// <summary>
    /// Called during HandleKeyboard() before children handle keyboard input.
    /// Override this to add custom keyboard handling logic.
    /// </summary>
    /// <param name="keyboard">The keyboard device.</param>
    /// <param name="gameTime">Game timing information.</param>
    protected virtual void OnHandleKeyboard(IKeyboard keyboard, GameTime gameTime)
    {
    }

    /// <summary>
    /// Called during HandleMouse() before children handle mouse input.
    /// Override this to add custom mouse handling logic.
    /// </summary>
    /// <param name="mouse">The mouse device.</param>
    /// <param name="gameTime">Game timing information.</param>
    protected virtual void OnHandleMouse(IMouse mouse, GameTime gameTime)
    {
    }

    /// <summary>
    /// Called when a child is added to this game object.
    /// Override this to add custom logic when children are added.
    /// </summary>
    /// <param name="child">The child that was added.</param>
    protected virtual void OnChildAdded(ISVoxObject child)
    {
    }

    /// <summary>
    /// Called when a child is removed from this game object.
    /// Override this to add custom logic when children are removed.
    /// </summary>
    /// <param name="child">The child that was removed.</param>
    protected virtual void OnChildRemoved(ISVoxObject child)
    {
    }
}
