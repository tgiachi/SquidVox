using MoonSharp.Interpreter;
using SquidVox.Core.Attributes.Scripts;
using SquidVox.Core.Enums;
using SquidVox.Core.Interfaces.Services;

namespace SquidVox.World3d.Modules;

/// <summary>
/// Lua module for input management.
/// </summary>
[ScriptModule("input_manager", "Input Manager Module")]
public class InputManagerModule
{
    private readonly IInputManager _inputManager;

    /// <summary>
    /// Initializes a new instance of the InputManagerModule class.
    /// </summary>
    /// <param name="inputManager">The input manager service.</param>
    public InputManagerModule(IInputManager inputManager)
    {
        _inputManager = inputManager;
    }

    /// <summary>
    /// Binds a key combination to a Lua callback function.
    /// </summary>
    /// <param name="keyBinding">The key binding string (e.g., "Ctrl+A", "F1", "Shift+Escape").</param>
    /// <param name="callback">The Lua function to execute when the key is pressed.</param>
    [ScriptFunction("bind_key", "Binds a key to a callback action.")]
    public void BindKey(string keyBinding, Closure callback)
    {
        _inputManager.BindKey(
            keyBinding,
            () =>
            {
                try
                {
                    callback.Call();
                }
                catch (Exception ex)
                {
                    throw new ScriptRuntimeException(
                        $"Error executing key binding callback for '{keyBinding}': {ex.Message}",
                        ex
                    );
                }
            }
        );
    }

    /// <summary>
    /// Binds a key combination to a Lua callback function with a specific input context.
    /// </summary>
    /// <param name="keyBinding">The key binding string (e.g., "Ctrl+A", "F1", "Shift+Escape").</param>
    /// <param name="callback">The Lua function to execute when the key is pressed.</param>
    /// <param name="contextName">The input context name ("UI", "Gameplay3D", "Gameplay2D", "Debug", "Paused").</param>
    [ScriptFunction("bind_key_context", "Binds a key to a callback action with a specific context.")]
    public void BindKeyWithContext(string keyBinding, DynValue callback, string contextName)
    {
        if (callback == null || callback.Type != DataType.Function)
        {
            throw new ScriptRuntimeException("Callback must be a function");
        }

        if (!Enum.TryParse<InputContext>(contextName, true, out var context))
        {
            throw new ScriptRuntimeException(
                $"Invalid input context: {contextName}. Valid values: None, UI, Gameplay3D, Gameplay2D, Debug, Paused"
            );
        }

        var closure = callback.Function;

        _inputManager.BindKey(
            keyBinding,
            () =>
            {
                try
                {
                    closure.Call();
                }
                catch (Exception ex)
                {
                    throw new ScriptRuntimeException(
                        $"Error executing key binding callback for '{keyBinding}': {ex.Message}",
                        ex
                    );
                }
            },
            context
        );
    }

    /// <summary>
    /// Unbinds a key combination.
    /// </summary>
    /// <param name="keyBinding">The key binding string to unbind.</param>
    [ScriptFunction("unbind_key", "Unbinds a key.")]
    public void UnbindKey(string keyBinding)
    {
        _inputManager.UnbindKey(keyBinding);
    }

    /// <summary>
    /// Clears all key bindings.
    /// </summary>
    [ScriptFunction("clear_bindings", "Clears all key bindings.")]
    public void ClearBindings()
    {
        _inputManager.ClearBindings();
    }

    /// <summary>
    /// Gets or sets the current input context.
    /// </summary>
    /// <param name="contextName">The context name ("None", "UI", "Gameplay3D", "Gameplay2D", "Debug", "Paused").</param>
    [ScriptFunction("set_context", "Sets the current input context.")]
    public void SetContext(string contextName)
    {
        if (!Enum.TryParse<InputContext>(contextName, true, out var context))
        {
            throw new ScriptRuntimeException(
                $"Invalid input context: {contextName}. Valid values: None, UI, Gameplay3D, Gameplay2D, Debug, Paused"
            );
        }

        _inputManager.CurrentContext = context;
    }

    /// <summary>
    /// Gets the current input context name.
    /// </summary>
    /// <returns>The current context name as a string.</returns>
    [ScriptFunction("get_context", "Gets the current input context.")]
    public string GetContext()
    {
        return _inputManager.CurrentContext.ToString();
    }

    /// <summary>
    /// Checks if a key is currently down.
    /// </summary>
    /// <param name="keyName">The key name (e.g., "A", "Space", "Escape").</param>
    /// <returns>True if the key is down.</returns>
    [ScriptFunction("is_key_down", "Checks if a key is currently down.")]
    public bool IsKeyDown(string keyName)
    {
        return Enum.TryParse<Microsoft.Xna.Framework.Input.Keys>(keyName, true, out var key) && _inputManager.IsKeyDown(key);
    }

    /// <summary>
    /// Checks if a key was just pressed this frame.
    /// </summary>
    /// <param name="keyName">The key name (e.g., "A", "Space", "Escape").</param>
    /// <returns>True if the key was just pressed.</returns>
    [ScriptFunction("is_key_pressed", "Checks if a key was just pressed.")]
    public bool IsKeyPressed(string keyName)
    {
        return Enum.TryParse<Microsoft.Xna.Framework.Input.Keys>(keyName, true, out var key) &&
               _inputManager.IsKeyPressed(key);
    }
}
