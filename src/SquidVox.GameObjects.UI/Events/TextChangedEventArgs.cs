namespace SquidVox.GameObjects.UI.Events;

/// <summary>
/// Event arguments for text changed event
/// </summary>
public class TextChangedEventArgs : EventArgs
{
    public TextChangedEventArgs(string oldText, string newText)
    {
        OldText = oldText;
        NewText = newText;
    }

    public string OldText { get; }
    public string NewText { get; }
}
