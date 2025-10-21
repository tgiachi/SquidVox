using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Serilog;
using SquidVox.Core.Data.Input;
using SquidVox.Core.Enums;
using SquidVox.Core.Interfaces.GameObjects;
using SquidVox.Core.Interfaces.Services;

namespace SquidVox.World3d.Services;

/// <summary>
/// Implements the input management service.
/// </summary>
public class InputManagerService : IInputManager
{
    private readonly ILogger _logger = Log.ForContext<InputManagerService>();
    private readonly Stack<ISVoxInputReceiver> _focusStack = new();
    private readonly Dictionary<KeyBinding, (Action Action, InputContext? Context)> _keyBindings = new();
    private readonly HashSet<KeyBinding> _processedBindings = new();

    private ISVoxInputReceiver? _currentFocus;
    private InputContext _currentContext = InputContext.None;

    /// <summary>
    /// Gets or sets the current input context.
    /// </summary>
    public InputContext CurrentContext
    {
        get => _currentContext;
        set
        {
            if (_currentContext != value)
            {
                var oldContext = _currentContext;
                _currentContext = value;
                ContextChanged?.Invoke(this, new InputContextChangedEventArgs(oldContext, value));
                _logger.Debug("Input context changed from {OldContext} to {NewContext}", oldContext, value);
            }
        }
    }

    /// <summary>
    /// Gets the current keyboard state.
    /// </summary>
    public KeyboardState CurrentKeyboardState { get; private set; }

    /// <summary>
    /// Gets the previous keyboard state.
    /// </summary>
    public KeyboardState PreviousKeyboardState { get; private set; }

    /// <summary>
    /// Gets the current mouse state.
    /// </summary>
    public MouseState CurrentMouseState { get; private set; }

    /// <summary>
    /// Gets the previous mouse state.
    /// </summary>
    public MouseState PreviousMouseState { get; private set; }

    /// <summary>
    /// Gets the current input focus receiver.
    /// </summary>
    public ISVoxInputReceiver? CurrentFocus => _currentFocus;

    /// <summary>
    /// Gets the depth of the focus stack.
    /// </summary>
    public int FocusStackDepth => _focusStack.Count;

    /// <summary>
    /// Event raised when the input context changes.
    /// </summary>
    public event EventHandler<InputContextChangedEventArgs>? ContextChanged;

    /// <summary>
    /// Sets the input focus to a specific receiver.
    /// </summary>
    /// <param name="receiver">The receiver to give focus to.</param>
    public void SetFocus(ISVoxInputReceiver? receiver)
    {
        if (_currentFocus != null)
        {
            _currentFocus.HasFocus = false;
        }

        _currentFocus = receiver;

        if (_currentFocus != null)
        {
            _currentFocus.HasFocus = true;
            _logger.Debug("Input focus set to {Receiver}", _currentFocus.GetType().Name);
        }
        else
        {
            _logger.Debug("Input focus cleared");
        }
    }

    /// <summary>
    /// Clears the current input focus.
    /// </summary>
    public void ClearFocus()
    {
        SetFocus(null);
    }

    /// <summary>
    /// Pushes a new focus receiver onto the focus stack.
    /// </summary>
    /// <param name="receiver">The receiver to push.</param>
    public void PushFocusStack(ISVoxInputReceiver receiver)
    {
        ArgumentNullException.ThrowIfNull(receiver);

        if (_currentFocus != null)
        {
            _focusStack.Push(_currentFocus);
            _currentFocus.HasFocus = false;
        }

        _currentFocus = receiver;
        _currentFocus.HasFocus = true;

        _logger.Debug("Pushed {Receiver} to focus stack (depth: {Depth})", receiver.GetType().Name, _focusStack.Count + 1);
    }

    /// <summary>
    /// Pops the top focus receiver from the stack.
    /// </summary>
    public void PopFocusStack()
    {
        if (_currentFocus != null)
        {
            _currentFocus.HasFocus = false;
        }

        if (_focusStack.TryPop(out var previous))
        {
            _currentFocus = previous;
            _currentFocus.HasFocus = true;
            _logger.Debug("Popped focus stack, restored {Receiver} (depth: {Depth})", _currentFocus.GetType().Name, _focusStack.Count);
        }
        else
        {
            _currentFocus = null;
            _logger.Debug("Popped focus stack, no previous focus");
        }
    }

    /// <summary>
    /// Binds a key combination to an action.
    /// </summary>
    /// <param name="binding">The key binding string.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="context">Optional context to limit when this binding is active.</param>
    public void BindKey(string binding, Action action, InputContext? context = null)
    {
        var keyBinding = KeyBinding.Parse(binding);
        BindKey(keyBinding, action, context);
    }

    /// <summary>
    /// Binds a key combination to an action.
    /// </summary>
    /// <param name="binding">The key binding.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="context">Optional context to limit when this binding is active.</param>
    public void BindKey(KeyBinding binding, Action action, InputContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(action);

        _keyBindings[binding] = (action, context);
        _logger.Debug("Bound key {Binding} to action (context: {Context})", binding, context?.ToString() ?? "any");
    }

    /// <summary>
    /// Unbinds a key combination.
    /// </summary>
    /// <param name="binding">The key binding string.</param>
    public void UnbindKey(string binding)
    {
        var keyBinding = KeyBinding.Parse(binding);
        UnbindKey(keyBinding);
    }

    /// <summary>
    /// Unbinds a key combination.
    /// </summary>
    /// <param name="binding">The key binding.</param>
    public void UnbindKey(KeyBinding binding)
    {
        if (_keyBindings.Remove(binding))
        {
            _logger.Debug("Unbound key {Binding}", binding);
        }
    }

    /// <summary>
    /// Clears all key bindings.
    /// </summary>
    public void ClearBindings()
    {
        _keyBindings.Clear();
        _logger.Debug("Cleared all key bindings");
    }

    /// <summary>
    /// Checks if a key is currently down.
    /// </summary>
    public bool IsKeyDown(Keys key)
    {
        return CurrentKeyboardState.IsKeyDown(key);
    }

    /// <summary>
    /// Checks if a key was just pressed this frame.
    /// </summary>
    public bool IsKeyPressed(Keys key)
    {
        return CurrentKeyboardState.IsKeyDown(key) && PreviousKeyboardState.IsKeyUp(key);
    }

    /// <summary>
    /// Checks if a key was just released this frame.
    /// </summary>
    public bool IsKeyReleased(Keys key)
    {
        return CurrentKeyboardState.IsKeyUp(key) && PreviousKeyboardState.IsKeyDown(key);
    }

    /// <summary>
    /// Checks if a mouse button is currently down.
    /// </summary>
    public bool IsMouseButtonDown(MouseButton button)
    {
        return GetMouseButtonState(CurrentMouseState, button) == ButtonState.Pressed;
    }

    /// <summary>
    /// Checks if a mouse button was just pressed this frame.
    /// </summary>
    public bool IsMouseButtonPressed(MouseButton button)
    {
        return GetMouseButtonState(CurrentMouseState, button) == ButtonState.Pressed &&
               GetMouseButtonState(PreviousMouseState, button) == ButtonState.Released;
    }

    /// <summary>
    /// Checks if a mouse button was just released this frame.
    /// </summary>
    public bool IsMouseButtonReleased(MouseButton button)
    {
        return GetMouseButtonState(CurrentMouseState, button) == ButtonState.Released &&
               GetMouseButtonState(PreviousMouseState, button) == ButtonState.Pressed;
    }

    private static ButtonState GetMouseButtonState(MouseState state, MouseButton button)
    {
        return button switch
        {
            MouseButton.Left => state.LeftButton,
            MouseButton.Right => state.RightButton,
            MouseButton.Middle => state.MiddleButton,
            MouseButton.XButton1 => state.XButton1,
            MouseButton.XButton2 => state.XButton2,
            _ => ButtonState.Released
        };
    }

    /// <summary>
    /// Updates the input manager.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        PreviousKeyboardState = CurrentKeyboardState;
        PreviousMouseState = CurrentMouseState;

        CurrentKeyboardState = Keyboard.GetState();
        CurrentMouseState = Mouse.GetState();

        ProcessKeyBindings();
    }

    private void ProcessKeyBindings()
    {
        _processedBindings.Clear();

        foreach (var (binding, (action, context)) in _keyBindings)
        {
            if (context.HasValue && context.Value != CurrentContext)
                continue;

            if (binding.IsJustPressed(CurrentKeyboardState, PreviousKeyboardState))
            {
                if (_processedBindings.Add(binding))
                {
                    try
                    {
                        action.Invoke();
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Error executing key binding action for {Binding}", binding);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Distributes input to the current focus receiver.
    /// </summary>
    public void DistributeInput(GameTime gameTime)
    {
        if (_currentFocus != null && _currentFocus.HasFocus)
        {
            _currentFocus.HandleKeyboard(CurrentKeyboardState, gameTime);
            _currentFocus.HandleMouse(CurrentMouseState, gameTime);
        }
    }

    /// <summary>
    /// Disposes of the input manager.
    /// </summary>
    public void Dispose()
    {
        _focusStack.Clear();
        _keyBindings.Clear();
        _processedBindings.Clear();
        _currentFocus = null;
        GC.SuppressFinalize(this);
    }
}
