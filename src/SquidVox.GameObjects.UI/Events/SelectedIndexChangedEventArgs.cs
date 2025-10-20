namespace SquidVox.GameObjects.UI.Events;

/// <summary>
/// Event arguments for selected index changed event
/// </summary>
public class SelectedIndexChangedEventArgs : EventArgs
{
    public SelectedIndexChangedEventArgs(int oldIndex, int newIndex)
    {
        OldIndex = oldIndex;
        NewIndex = newIndex;
    }

    public int OldIndex { get; }
    public int NewIndex { get; }
}
