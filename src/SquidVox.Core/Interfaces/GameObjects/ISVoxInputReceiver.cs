using Silk.NET.Input;
using SquidVox.Core.Data.Graphics;

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
    /// <param name="keyboard">The keyboard device.</param>
    /// <param name="gameTime">Game timing information.</param>
    void HandleKeyboard(IKeyboard keyboard, GameTime gameTime);

    /// <summary>
    /// Handles mouse input when the object has focus.
    /// </summary>
    /// <param name="mouse">The mouse device.</param>
    /// <param name="gameTime">Game timing information.</param>
    void HandleMouse(IMouse mouse, GameTime gameTime);
}
