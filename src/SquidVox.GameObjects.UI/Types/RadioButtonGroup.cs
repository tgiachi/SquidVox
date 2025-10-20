using SquidVox.GameObjects.UI.Controls;

namespace SquidVox.GameObjects.UI.Types;

/// <summary>
/// Manages a group of radio buttons ensuring only one can be selected at a time
/// </summary>
public class RadioButtonGroup
{
    private readonly List<RadioButtonGameObject> _radioButtons = new();

    /// <summary>
    /// Gets the currently selected radio button
    /// </summary>
    public RadioButtonGameObject? SelectedButton { get; private set; }

    /// <summary>
    /// Gets the value of the currently selected radio button
    /// </summary>
    public string? SelectedValue => SelectedButton?.Value;

    /// <summary>
    /// Gets the index of the currently selected radio button
    /// </summary>
    public int SelectedIndex => SelectedButton == null ? -1 : _radioButtons.IndexOf(SelectedButton);

    /// <summary>
    /// Gets all radio buttons in this group
    /// </summary>
    public IReadOnlyList<RadioButtonGameObject> RadioButtons => _radioButtons.AsReadOnly();

    /// <summary>
    /// Event fired when the selection changes
    /// </summary>
    public event EventHandler<RadioButtonGameObject?>? SelectionChanged;

    /// <summary>
    /// Adds a radio button to this group
    /// </summary>
    /// <param name="radioButton">The radio button to add</param>
    public void AddRadioButton(RadioButtonGameObject radioButton)
    {
        if (radioButton == null)
        {
            return;
        }

        if (_radioButtons.Contains(radioButton))
        {
            return;
        }

        // Remove from previous group if any
        radioButton.Group?.RemoveRadioButton(radioButton);

        _radioButtons.Add(radioButton);
        radioButton.SetGroup(this);

        // If this is the first button or it's already selected, make it selected
        if (SelectedButton == null && radioButton.IsSelected)
        {
            SelectRadioButton(radioButton, false);
        }
        else if (SelectedButton != null && radioButton.IsSelected)
        {
            // Deselect this button since another is already selected
            radioButton.SetSelectedSilent(false);
        }
    }

    /// <summary>
    /// Removes a radio button from this group
    /// </summary>
    /// <param name="radioButton">The radio button to remove</param>
    public void RemoveRadioButton(RadioButtonGameObject radioButton)
    {
        if (radioButton == null)
        {
            return;
        }

        if (!_radioButtons.Contains(radioButton))
        {
            return;
        }

        _radioButtons.Remove(radioButton);
        radioButton.SetGroup(null);

        if (SelectedButton == radioButton)
        {
            SelectedButton = null;
            SelectionChanged?.Invoke(this, null);
        }
    }

    /// <summary>
    /// Selects a specific radio button in the group
    /// </summary>
    /// <param name="radioButton">The radio button to select</param>
    /// <param name="triggerEvent">Whether to trigger the SelectionChanged event</param>
    internal void SelectRadioButton(RadioButtonGameObject radioButton, bool triggerEvent = true)
    {
        if (radioButton == null)
        {
            return;
        }

        if (!_radioButtons.Contains(radioButton))
        {
            return;
        }

        if (SelectedButton == radioButton)
        {
            return;
        }

        // Deselect current selection
        if (SelectedButton != null)
        {
            SelectedButton.SetSelectedSilent(false);
        }

        // Select new button
        SelectedButton = radioButton;
        radioButton.SetSelectedSilent(true);

        if (triggerEvent)
        {
            SelectionChanged?.Invoke(this, SelectedButton);
        }
    }

    /// <summary>
    /// Selects a radio button by its value
    /// </summary>
    /// <param name="value">The value of the radio button to select</param>
    /// <returns>True if a radio button with the specified value was found and selected</returns>
    public bool SelectByValue(string value)
    {
        var radioButton = _radioButtons.FirstOrDefault(rb => rb.Value == value);
        if (radioButton != null)
        {
            SelectRadioButton(radioButton);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Selects a radio button by its index
    /// </summary>
    /// <param name="index">The index of the radio button to select</param>
    /// <returns>True if a radio button at the specified index was found and selected</returns>
    public bool SelectByIndex(int index)
    {
        if (index >= 0 && index < _radioButtons.Count)
        {
            SelectRadioButton(_radioButtons[index]);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Clears the selection (no radio button selected)
    /// </summary>
    public void ClearSelection()
    {
        if (SelectedButton != null)
        {
            SelectedButton.SetSelectedSilent(false);
            SelectedButton = null;
            SelectionChanged?.Invoke(this, null);
        }
    }
}
