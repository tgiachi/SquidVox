namespace SquidVox.GameObjects.UI.Types;

/// <summary>
/// Represents an item in a ComboBox
/// </summary>
public class ComboItem
{
    public ComboItem(string text, object? value = null)
    {
        Text = text;
        Value = value ?? text;
    }

    /// <summary>
    /// Display text for the item
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Associated value/data for the item
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Whether this item is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}
