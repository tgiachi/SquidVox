using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace SquidVox.Core.Interfaces.GameObjects;

/// <summary>
/// Defines the contract for objects that can receive keyboard and mouse input events.
/// </summary>
public interface ISVoxInputReceiver
{
    /// <summary>
    /// Gets or sets whether this object has input focus for keyboard and mouse events.
    /// </summary>
    bool HasFocus { get; set; }

    /// <summary>
    /// Handles keyboard input when the object has focus.
    /// </summary>
    /// <param name="keyboardState">The current keyboard state.</param>
    /// <param name="gameTime">Game timing information.</param>
    void HandleKeyboard(KeyboardState keyboardState, GameTime gameTime);

    /// <summary>
    /// Handles mouse input when the object has focus.
    /// </summary>
    /// <param name="mouseState">The current mouse state.</param>
    /// <param name="gameTime">Game timing information.</param>
    void HandleMouse(MouseState mouseState, GameTime gameTime);
}
