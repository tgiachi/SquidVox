using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SquidVox.Core.Collections;
using SquidVox.Core.Interfaces.GameObjects;

namespace SquidVox.Core.GameObjects;

/// <summary>
/// Abstract base class for 3D game objects, providing default implementations for common functionality.
/// </summary>
public abstract class Base3dGameObject : ISVox3dDrawableGameObject, ISVoxInputReceiver
{
    private readonly SvoxGameObjectCollection<ISVox3dDrawableGameObject> _children = [];

    /// <summary>
    /// Gets or sets the name of the game object.
    /// </summary>
    public virtual string Name { get; set; } = "Unnamed 3D GameObject";

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
    /// Gets or sets the position of the game object (local position relative to parent).
    /// </summary>
    public virtual Vector3 Position { get; set; } = Vector3.Zero;

    /// <summary>
    /// Gets or sets the scale of the game object.
    /// </summary>
    public virtual Vector3 Scale { get; set; } = Vector3.One;

    /// <summary>
    /// Gets or sets the rotation of the game object as a quaternion.
    /// </summary>
    public virtual Quaternion Rotation { get; set; } = Quaternion.Identity;

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
    public Vector3 GetAbsolutePosition()
    {
        if (Parent == null)
        {
            return Position;
        }

        if (Parent is Base3dGameObject parentGameObject)
        {
            return Position + parentGameObject.GetAbsolutePosition();
        }

        if (Parent is ISVox3dRenderable renderable3d)
        {
            return Position + renderable3d.Position;
        }

        return Position;
    }

    /// <summary>
    /// Gets the absolute (world) scale of the game object.
    /// This considers the scale of all parent objects in the hierarchy.
    /// </summary>
    /// <returns>The absolute scale in world space.</returns>
    public Vector3 GetAbsoluteScale()
    {
        if (Parent == null)
        {
            return Scale;
        }

        if (Parent is Base3dGameObject parentGameObject)
        {
            return Scale * parentGameObject.GetAbsoluteScale();
        }

        if (Parent is ISVox3dRenderable renderable3d)
        {
            return Scale * renderable3d.Scale;
        }

        return Scale;
    }

    /// <summary>
    /// Gets the absolute (world) rotation of the game object.
    /// This considers the rotation of all parent objects in the hierarchy.
    /// </summary>
    /// <returns>The absolute rotation in world space.</returns>
    public Quaternion GetAbsoluteRotation()
    {
        if (Parent == null)
        {
            return Rotation;
        }

        if (Parent is Base3dGameObject parentGameObject)
        {
            return Rotation * parentGameObject.GetAbsoluteRotation();
        }

        if (Parent is ISVox3dRenderable renderable3d)
        {
            return Rotation * renderable3d.Rotation;
        }

        return Rotation;
    }

    /// <summary>
    /// Adds a child game object to this object's hierarchy.
    /// </summary>
    /// <param name="child">The child to add.</param>
    /// <exception cref="ArgumentException">Thrown when child is not an ISVox3dDrawableGameObject.</exception>
    public virtual void AddChild(ISVoxObject child)
    {
        ArgumentNullException.ThrowIfNull(child);

        if (child is not ISVox3dDrawableGameObject drawableChild)
        {
            throw new ArgumentException(
                $"Base3dGameObject can only accept children of type ISVox3dDrawableGameObject. Received: {child.GetType().Name}",
                nameof(child)
            );
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
        if (child is not ISVox3dDrawableGameObject drawableChild)
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

        for (var i = 0; i < _children.Count; i++)
        {
            var child = _children[i];
            if (child.IsEnabled)
            {
                child.Update(gameTime);
            }
        }
    }

    /// <summary>
    /// Renders the game object and all its visible children.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device for rendering.</param>
    public virtual void Render(GraphicsDevice graphicsDevice)
    {
        if (!IsVisible)
        {
            return;
        }

        OnRender(graphicsDevice);

        for (var i = 0; i < _children.Count; i++)
        {
            var child = _children[i];
            if (child.IsVisible)
            {
                child.Render(graphicsDevice);
            }
        }
    }

    /// <summary>
    /// Handles keyboard input when the game object has focus.
    /// </summary>
    /// <param name="keyboardState">The current keyboard state.</param>
    /// <param name="gameTime">Game timing information.</param>
    public virtual void HandleKeyboard(KeyboardState keyboardState, GameTime gameTime)
    {
        if (!HasFocus || !IsEnabled)
        {
            return;
        }

        OnHandleKeyboard(keyboardState, gameTime);

        for (var i = 0; i < _children.Count; i++)
        {
            if (_children[i] is ISVoxInputReceiver inputReceiver && inputReceiver.HasFocus)
            {
                inputReceiver.HandleKeyboard(keyboardState, gameTime);
            }
        }
    }

    /// <summary>
    /// Handles mouse input for this game object and its children.
    /// </summary>
    /// <param name="mouseState">The current mouse state.</param>
    /// <param name="gameTime">Game timing information.</param>
    public virtual void HandleMouse(MouseState mouseState, GameTime gameTime)
    {
        if (!HasFocus || !IsEnabled)
        {
            return;
        }

        OnHandleMouse(mouseState, gameTime);

        for (var i = 0; i < _children.Count; i++)
        {
            if (_children[i] is ISVoxInputReceiver inputReceiver && inputReceiver.HasFocus)
            {
                inputReceiver.HandleMouse(mouseState, gameTime);
            }
        }
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
    /// <param name="graphicsDevice">The graphics device for rendering.</param>
    protected virtual void OnRender(GraphicsDevice graphicsDevice)
    {
    }

    /// <summary>
    /// Called during HandleKeyboard() before children handle keyboard input.
    /// Override this to add custom keyboard handling logic.
    /// </summary>
    /// <param name="keyboardState">The current keyboard state.</param>
    /// <param name="gameTime">Game timing information.</param>
    protected virtual void OnHandleKeyboard(KeyboardState keyboardState, GameTime gameTime)
    {
    }

    /// <summary>
    /// Called during HandleMouse() before children handle mouse input.
    /// Override this to add custom mouse handling logic.
    /// </summary>
    /// <param name="mouseState">The current mouse state.</param>
    /// <param name="gameTime">Game timing information.</param>
    protected virtual void OnHandleMouse(MouseState mouseState, GameTime gameTime)
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
