namespace SquidVox.GameObjects.UI.Data;

/// <summary>
/// Represents an item in a ListBox
/// </summary>
public class ListBoxItem
{
    public ListBoxItem(string text, object? value = null)
    {
        Text = text;
        Value = value;
    }

    public string Text { get; set; }
    public object? Value { get; set; }

    public override string ToString()
    {
        return Text;
    }
}
