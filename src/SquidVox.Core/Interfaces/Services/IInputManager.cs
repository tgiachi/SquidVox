using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SquidVox.Core.Data.Input;
using SquidVox.Core.Enums;
using SquidVox.Core.Interfaces.GameObjects;

namespace SquidVox.Core.Interfaces.Services;

/// <summary>
/// Defines the contract for input management services.
/// Handles focus management, input distribution, and key bindings.
/// </summary>
public interface IInputManager : IDisposable
{
    /// <summary>
    /// Gets or sets the current input context.
    /// </summary>
    InputContext CurrentContext { get; set; }

    /// <summary>
    /// Gets the current keyboard state.
    /// </summary>
    KeyboardState CurrentKeyboardState { get; }

    /// <summary>
    /// Gets the previous keyboard state.
    /// </summary>
    KeyboardState PreviousKeyboardState { get; }

    /// <summary>
    /// Gets the current mouse state.
    /// </summary>
    MouseState CurrentMouseState { get; }

    /// <summary>
    /// Gets the previous mouse state.
    /// </summary>
    MouseState PreviousMouseState { get; }

    /// <summary>
    /// Gets the current input focus receiver.
    /// </summary>
    ISVoxInputReceiver? CurrentFocus { get; }

    /// <summary>
    /// Sets the input focus to a specific receiver.
    /// Clears focus from the previous receiver.
    /// </summary>
    /// <param name="receiver">The receiver to give focus to.</param>
    void SetFocus(ISVoxInputReceiver? receiver);

    /// <summary>
    /// Clears the current input focus.
    /// </summary>
    void ClearFocus();

    /// <summary>
    /// Pushes a new focus receiver onto the focus stack.
    /// Used for modal dialogs, menus, etc.
    /// </summary>
    /// <param name="receiver">The receiver to push.</param>
    void PushFocusStack(ISVoxInputReceiver receiver);

    /// <summary>
    /// Pops the top focus receiver from the stack.
    /// Restores focus to the previous receiver.
    /// </summary>
    void PopFocusStack();

    /// <summary>
    /// Gets the depth of the focus stack.
    /// </summary>
    int FocusStackDepth { get; }

    /// <summary>
    /// Binds a key combination to an action.
    /// </summary>
    /// <param name="binding">The key binding string (e.g., "Ctrl+A", "Shift+F1").</param>
    /// <param name="action">The action to execute when the binding is pressed.</param>
    /// <param name="context">Optional context to limit when this binding is active.</param>
    void BindKey(string binding, Action action, InputContext? context = null);

    /// <summary>
    /// Binds a key combination to an action.
    /// </summary>
    /// <param name="binding">The key binding.</param>
    /// <param name="action">The action to execute when the binding is pressed.</param>
    /// <param name="context">Optional context to limit when this binding is active.</param>
    void BindKey(KeyBinding binding, Action action, InputContext? context = null);

    /// <summary>
    /// Unbinds a key combination.
    /// </summary>
    /// <param name="binding">The key binding string.</param>
    void UnbindKey(string binding);

    /// <summary>
    /// Unbinds a key combination.
    /// </summary>
    /// <param name="binding">The key binding.</param>
    void UnbindKey(KeyBinding binding);

    /// <summary>
    /// Clears all key bindings.
    /// </summary>
    void ClearBindings();

    /// <summary>
    /// Checks if a key is currently down.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key is down.</returns>
    bool IsKeyDown(Keys key);

    /// <summary>
    /// Checks if a key was just pressed this frame.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key was just pressed.</returns>
    bool IsKeyPressed(Keys key);

    /// <summary>
    /// Checks if a key was just released this frame.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key was just released.</returns>
    bool IsKeyReleased(Keys key);

    /// <summary>
    /// Checks if a mouse button is currently down.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>True if the button is down.</returns>
    bool IsMouseButtonDown(MouseButton button);

    /// <summary>
    /// Checks if a mouse button was just pressed this frame.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>True if the button was just pressed.</returns>
    bool IsMouseButtonPressed(MouseButton button);

    /// <summary>
    /// Checks if a mouse button was just released this frame.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>True if the button was just released.</returns>
    bool IsMouseButtonReleased(MouseButton button);

    /// <summary>
    /// Updates the input manager.
    /// Samples input states and processes key bindings.
    /// </summary>
    /// <param name="gameTime">Game timing information.</param>
    void Update(GameTime gameTime);

    /// <summary>
    /// Distributes input to the current focus receiver.
    /// </summary>
    /// <param name="gameTime">Game timing information.</param>
    void DistributeInput(GameTime gameTime);

    /// <summary>
    /// Event raised when the input context changes.
    /// </summary>
    event EventHandler<InputContextChangedEventArgs>? ContextChanged;
}

/// <summary>
/// Mouse button enumeration.
/// </summary>
public enum MouseButton
{
    /// <summary>
    /// Left mouse button.
    /// </summary>
    Left,

    /// <summary>
    /// Right mouse button.
    /// </summary>
    Right,

    /// <summary>
    /// Middle mouse button.
    /// </summary>
    Middle,

    /// <summary>
    /// Extra button 1.
    /// </summary>
    XButton1,

    /// <summary>
    /// Extra button 2.
    /// </summary>
    XButton2
}

/// <summary>
/// Event arguments for input context changes.
/// </summary>
public class InputContextChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the previous input context.
    /// </summary>
    public InputContext OldContext { get; }

    /// <summary>
    /// Gets the new input context.
    /// </summary>
    public InputContext NewContext { get; }

    /// <summary>
    /// Initializes a new instance of the InputContextChangedEventArgs class.
    /// </summary>
    /// <param name="oldContext">The previous context.</param>
    /// <param name="newContext">The new context.</param>
    public InputContextChangedEventArgs(InputContext oldContext, InputContext newContext)
    {
        OldContext = oldContext;
        NewContext = newContext;
    }
}
