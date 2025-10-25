using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SquidVox.Core.Interfaces.GameObjects;

namespace SquidVox.Tests.TestHelpers;

/// <summary>
/// Mock game object with input support.
/// </summary>
public class MockInputGameObject : MockGameObject, ISVoxInputReceiver
{
    /// <summary>
    ///
    /// </summary>
    public bool HasFocus { get; set; }
    /// <summary>
    ///
    /// </summary>
    public int KeyboardHandleCount { get; private set; }
    /// <summary>
    ///
    /// </summary>
    public int MouseHandleCount { get; private set; }

    /// <summary>
    ///
    /// </summary>
    public void HandleKeyboard(KeyboardState keyboardState, Microsoft.Xna.Framework.GameTime gameTime)
    {
        KeyboardHandleCount++;
    }

    /// <summary>
    ///
    /// </summary>
    public void HandleMouse(MouseState mouseState, Microsoft.Xna.Framework.GameTime gameTime)
    {
        MouseHandleCount++;
    }
}