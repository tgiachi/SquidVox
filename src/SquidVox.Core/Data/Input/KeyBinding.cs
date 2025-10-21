using Microsoft.Xna.Framework.Input;

namespace SquidVox.Core.Data.Input;

/// <summary>
/// Represents a key binding with optional modifiers (Ctrl, Shift, Alt).
/// </summary>
public readonly struct KeyBinding : IEquatable<KeyBinding>
{
    /// <summary>
    /// Gets the primary key.
    /// </summary>
    public Keys Key { get; }

    /// <summary>
    /// Gets whether Ctrl modifier is required.
    /// </summary>
    public bool RequiresCtrl { get; }

    /// <summary>
    /// Gets whether Shift modifier is required.
    /// </summary>
    public bool RequiresShift { get; }

    /// <summary>
    /// Gets whether Alt modifier is required.
    /// </summary>
    public bool RequiresAlt { get; }

    /// <summary>
    /// Initializes a new instance of the KeyBinding struct.
    /// </summary>
    /// <param name="key">The primary key.</param>
    /// <param name="requiresCtrl">Whether Ctrl modifier is required.</param>
    /// <param name="requiresShift">Whether Shift modifier is required.</param>
    /// <param name="requiresAlt">Whether Alt modifier is required.</param>
    public KeyBinding(Keys key, bool requiresCtrl = false, bool requiresShift = false, bool requiresAlt = false)
    {
        Key = key;
        RequiresCtrl = requiresCtrl;
        RequiresShift = requiresShift;
        RequiresAlt = requiresAlt;
    }

    /// <summary>
    /// Parses a key binding string (e.g., "Ctrl+A", "Shift+F1", "Ctrl+Shift+S").
    /// </summary>
    /// <param name="binding">The binding string.</param>
    /// <returns>The parsed key binding.</returns>
    /// <exception cref="ArgumentException">Thrown when binding format is invalid.</exception>
    public static KeyBinding Parse(string binding)
    {
        if (string.IsNullOrWhiteSpace(binding))
            throw new ArgumentException("Binding string cannot be null or empty", nameof(binding));

        var parts = binding.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
            throw new ArgumentException("Invalid binding format", nameof(binding));

        bool requiresCtrl = false;
        bool requiresShift = false;
        bool requiresAlt = false;
        Keys? key = null;

        foreach (var part in parts)
        {
            var upperPart = part.ToUpperInvariant();

            if (upperPart == "CTRL" || upperPart == "CONTROL")
            {
                requiresCtrl = true;
            }
            else if (upperPart == "SHIFT")
            {
                requiresShift = true;
            }
            else if (upperPart == "ALT")
            {
                requiresAlt = true;
            }
            else
            {
                if (Enum.TryParse<Keys>(part, true, out var parsedKey))
                {
                    key = parsedKey;
                }
                else
                {
                    throw new ArgumentException($"Invalid key: {part}", nameof(binding));
                }
            }
        }

        if (!key.HasValue)
            throw new ArgumentException("No key specified in binding", nameof(binding));

        return new KeyBinding(key.Value, requiresCtrl, requiresShift, requiresAlt);
    }

    /// <summary>
    /// Checks if this key binding matches the current keyboard state.
    /// </summary>
    /// <param name="keyboardState">The current keyboard state.</param>
    /// <returns>True if the binding is currently pressed.</returns>
    public bool IsPressed(KeyboardState keyboardState)
    {
        if (!keyboardState.IsKeyDown(Key))
            return false;

        bool ctrlPressed = keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);
        bool shiftPressed = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);
        bool altPressed = keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt);

        if (RequiresCtrl && !ctrlPressed)
            return false;
        if (RequiresShift && !shiftPressed)
            return false;
        if (RequiresAlt && !altPressed)
            return false;

        if (!RequiresCtrl && ctrlPressed)
            return false;
        if (!RequiresShift && shiftPressed)
            return false;
        if (!RequiresAlt && altPressed)
            return false;

        return true;
    }

    /// <summary>
    /// Checks if this key binding was just pressed (not held).
    /// </summary>
    /// <param name="currentState">The current keyboard state.</param>
    /// <param name="previousState">The previous keyboard state.</param>
    /// <returns>True if the binding was just pressed this frame.</returns>
    public bool IsJustPressed(KeyboardState currentState, KeyboardState previousState)
    {
        return IsPressed(currentState) && !IsPressed(previousState);
    }

    public override string ToString()
    {
        var parts = new List<string>();

        if (RequiresCtrl) parts.Add("Ctrl");
        if (RequiresShift) parts.Add("Shift");
        if (RequiresAlt) parts.Add("Alt");
        parts.Add(Key.ToString());

        return string.Join("+", parts);
    }

    public bool Equals(KeyBinding other)
    {
        return Key == other.Key &&
               RequiresCtrl == other.RequiresCtrl &&
               RequiresShift == other.RequiresShift &&
               RequiresAlt == other.RequiresAlt;
    }

    public override bool Equals(object? obj)
    {
        return obj is KeyBinding other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Key, RequiresCtrl, RequiresShift, RequiresAlt);
    }

    public static bool operator ==(KeyBinding left, KeyBinding right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(KeyBinding left, KeyBinding right)
    {
        return !left.Equals(right);
    }
}
