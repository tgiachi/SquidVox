using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SquidVox.Core.Interfaces.GameObjects;

namespace SquidVox.Tests.TestHelpers;

/// <summary>
/// Mock game object for testing purposes.
/// </summary>
public class MockGameObject : ISVox2dDrawableGameObject
{
    public string Name { get; set; } = string.Empty;
    public int ZIndex { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsVisible { get; set; } = true;
    public Vector2 Position { get; set; } = Vector2.Zero;
    public Vector2 Scale { get; set; } = Vector2.One;
    public float Rotation { get; set; }
    public Vector2 Size { get; set; } = Vector2.Zero;
    public IEnumerable<ISVoxObject> Children => _children;
    public ISVoxObject? Parent { get; set; }

    private readonly List<ISVoxObject> _children = new();

    public int UpdateCallCount { get; private set; }
    public int RenderCallCount { get; private set; }

    public void AddChild(ISVoxObject child)
    {
        _children.Add(child);
        child.Parent = this;
    }

    public void RemoveChild(ISVoxObject child)
    {
        _children.Remove(child);
        child.Parent = null;
    }

    public void Update(Microsoft.Xna.Framework.GameTime gameTime)
    {
        UpdateCallCount++;
    }

    public void Render(SpriteBatch spriteBatch)
    {
        RenderCallCount++;
    }
}

/// <summary>
/// Mock game object with initialization support.
/// </summary>
public class MockInitializableGameObject : MockGameObject, ISVoxInitializable
{
    public bool IsInitialized { get; private set; }

    public void Initialize()
    {
        IsInitialized = true;
    }
}

/// <summary>
/// Mock game object with input support.
/// </summary>
public class MockInputGameObject : MockGameObject, ISVoxInputReceiver
{
    public bool HasFocus { get; set; }
    public int KeyboardHandleCount { get; private set; }
    public int MouseHandleCount { get; private set; }

    public void HandleKeyboard(KeyboardState keyboardState, Microsoft.Xna.Framework.GameTime gameTime)
    {
        KeyboardHandleCount++;
    }

    public void HandleMouse(MouseState mouseState, Microsoft.Xna.Framework.GameTime gameTime)
    {
        MouseHandleCount++;
    }
}
