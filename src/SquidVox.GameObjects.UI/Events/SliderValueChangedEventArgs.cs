namespace SquidVox.GameObjects.UI.Events;

/// <summary>
/// Event arguments for slider value changes
/// </summary>
public class SliderValueChangedEventArgs : EventArgs
{
    public SliderValueChangedEventArgs(float oldValue, float newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }

    public float OldValue { get; }
    public float NewValue { get; }
}
