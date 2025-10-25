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
    /// <summary>
    /// 
    /// </summary>
    public string Id { get; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// 
    /// </summary>
    public int ZIndex { get; set; }
    /// <summary>
    /// 
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// 
    /// </summary>
    public bool IsVisible { get; set; } = true;
    /// <summary>
    /// 
    /// </summary>
    public Vector2 Position { get; set; } = Vector2.Zero;
    /// <summary>
    /// 
    /// </summary>
    public Vector2 Scale { get; set; } = Vector2.One;
    /// <summary>
    /// 
    /// </summary>
    public float Rotation { get; set; }
    /// <summary>
    /// 
    /// </summary>
    public Vector2 Size { get; set; } = Vector2.Zero;
    /// <summary>
    /// 
    /// </summary>
    public IEnumerable<ISVoxObject> Children => _children;
    /// <summary>
    /// 
    /// </summary>
    public ISVoxObject? Parent { get; set; }

    private readonly List<ISVoxObject> _children = new();

    /// <summary>
    /// 
    /// </summary>
    public int UpdateCallCount { get; private set; }
    /// <summary>
    /// 
    /// </summary>
    public int RenderCallCount { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    public void AddChild(ISVoxObject child)
    {
        _children.Add(child);
        child.Parent = this;
    }

    /// <summary>
    /// 
    /// </summary>
    public void RemoveChild(ISVoxObject child)
    {
        _children.Remove(child);
        child.Parent = null;
    }

    /// <summary>
    /// 
    /// </summary>
    public void Update(Microsoft.Xna.Framework.GameTime gameTime)
    {
        UpdateCallCount++;
    }

    /// <summary>
    /// 
    /// </summary>
    public void Render(SpriteBatch spriteBatch)
    {
        RenderCallCount++;
    }
}


