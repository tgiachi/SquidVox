using SquidVox.Core.Enums;

namespace SquidVox.Core.Interfaces.Services;

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