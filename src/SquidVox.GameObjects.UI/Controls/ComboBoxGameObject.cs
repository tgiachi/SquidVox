using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Graphics;
using SquidVox.Core.GameObjects;
using SquidVox.Core.Interfaces.Services;
using SquidVox.GameObjects.UI.Events;
using SquidVox.GameObjects.UI.Types;

namespace SquidVox.GameObjects.UI.Controls;

/// <summary>
/// ComboBox game object with dropdown functionality and customizable styling
/// </summary>
public class ComboBoxGameObject : Base2dGameObject
{
    private const float ItemHeight = 24f;
    private const int MaxVisibleItems = 8;
    private readonly Vector2 _dropdownOffset;
    private readonly string _fontName;
    private readonly int _fontSize;
    private readonly List<ComboItem> _items = new();
    private IAssetManagerService? _assetManagerService;
    private float _dropdownHeight;
    private DynamicSpriteFont? _font;
    private bool _isInitialized;

    private MouseState _previousMouseState;
    private int _selectedIndex = -1;
    private Texture2D? _whitePixel;

    /// <summary>
    /// Initializes a new ComboBox game object
    /// </summary>
    /// <param name="width">Width of the ComboBox</param>
    /// <param name="height">Height of the ComboBox button</param>
    /// <param name="fontName">Font name for text display</param>
    /// <param name="fontSize">Font size for text display</param>
    /// <param name="assetManagerService">Asset manager service for loading resources</param>
    public ComboBoxGameObject(
        float width = 200f,
        float height = 28f,
        string fontName = "Monocraft",
        int fontSize = 14,
        IAssetManagerService? assetManagerService = null
    )
    {
        Size = new Vector2(width, height);
        _fontName = fontName;
        _fontSize = fontSize;
        _dropdownOffset = new Vector2(0, height);
        _assetManagerService = assetManagerService;

        // Default styling
        BackgroundColor = Color.White;
        ForegroundColor = Color.Black;
        BorderColor = Color.Gray;
        BorderWidth = 1;
        TextColor = Color.Black;
        DropdownBackgroundColor = Color.White;
        DropdownBorderColor = Color.Gray;
        SelectedItemColor = Color.LightBlue;
        HoverItemColor = Color.LightGray;
        ArrowColor = Color.Black;
    }

    /// <summary>
    /// Updates dropdown height based on item count
    /// </summary>
    private void UpdateDropdownHeight()
    {
        var visibleItems = Math.Min(_items.Count, MaxVisibleItems);
        _dropdownHeight = visibleItems * ItemHeight + BorderWidth * 2;
    }

    /// <summary>
    /// Initializes the combobox resources
    /// </summary>
    public void Initialize(IAssetManagerService assetManagerService, GraphicsDevice graphicsDevice)
    {
        if (_isInitialized)
        {
            return;
        }

        _assetManagerService = assetManagerService;
        LoadFont();

        // Create a 1x1 white pixel texture for drawing rectangles
        _whitePixel = new Texture2D(graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });

        UpdateDropdownHeight();

        _isInitialized = true;
    }

    /// <summary>
    /// Loads the font for text display
    /// </summary>
    private void LoadFont()
    {
        if (_assetManagerService == null)
        {
            return;
        }

        try
        {
            _font = _assetManagerService.GetFont(_fontName, _fontSize);
        }
        catch
        {
            try
            {
                _font = _assetManagerService.GetFont("Monocraft", _fontSize);
            }
            catch
            {
                // If no font available, _font remains null
            }
        }
    }

    /// <summary>
    /// Updates the combobox state
    /// </summary>
    protected override void OnUpdate(GameTime gameTime)
    {
        HandleMouseInput();
    }

    /// <summary>
    /// Handles mouse input for interaction
    /// </summary>
    private void HandleMouseInput()
    {
        var currentMouseState = Mouse.GetState();
        var mousePosition = new Vector2(currentMouseState.X, currentMouseState.Y);
        var absolutePos = GetAbsolutePosition();

        var comboBoxBounds = new Rectangle((int)absolutePos.X, (int)absolutePos.Y, (int)Size.X, (int)Size.Y);

        // Check for mouse click on ComboBox button
        if (currentMouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
        {
            if (comboBoxBounds.Contains(mousePosition))
            {
                ToggleDropdown();
            }
            else if (IsDropdownOpen)
            {
                // Check if clicking on dropdown item
                var dropdownBounds = GetDropdownBounds();
                if (dropdownBounds.Contains(mousePosition))
                {
                    HandleDropdownItemClick(mousePosition, dropdownBounds);
                }
                else
                {
                    // Click outside - close dropdown
                    CloseDropdown();
                }
            }
        }

        _previousMouseState = currentMouseState;
    }

    /// <summary>
    /// Gets the bounds of the dropdown area
    /// </summary>
    private Rectangle GetDropdownBounds()
    {
        var absolutePos = GetAbsolutePosition();
        return new Rectangle(
            (int)(absolutePos.X + _dropdownOffset.X),
            (int)(absolutePos.Y + _dropdownOffset.Y),
            (int)Size.X,
            (int)_dropdownHeight
        );
    }

    /// <summary>
    /// Handles click on dropdown item
    /// </summary>
    private void HandleDropdownItemClick(Vector2 mousePosition, Rectangle dropdownBounds)
    {
        var relativeY = mousePosition.Y - dropdownBounds.Y - BorderWidth;
        var itemIndex = (int)(relativeY / ItemHeight);

        if (itemIndex >= 0 && itemIndex < _items.Count && _items[itemIndex].IsEnabled)
        {
            SelectedIndex = itemIndex;
            CloseDropdown();
        }
    }

    /// <summary>
    /// Toggles the dropdown open/closed state
    /// </summary>
    public void ToggleDropdown()
    {
        if (IsDropdownOpen)
        {
            CloseDropdown();
        }
        else
        {
            OpenDropdown();
        }
    }

    /// <summary>
    /// Opens the dropdown
    /// </summary>
    public void OpenDropdown()
    {
        if (_items.Count == 0)
        {
            return;
        }

        IsDropdownOpen = true;
        DropdownOpened?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Closes the dropdown
    /// </summary>
    public void CloseDropdown()
    {
        IsDropdownOpen = false;
        DropdownClosed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Renders the ComboBox
    /// </summary>
    protected override void OnRender(SpriteBatch spriteBatch)
    {
        if (!IsVisible || _whitePixel == null)
        {
            return;
        }

        DrawComboBoxButton(spriteBatch);

        if (IsDropdownOpen)
        {
            DrawDropdown(spriteBatch);
        }
    }

    /// <summary>
    /// Draws the main ComboBox button
    /// </summary>
    private void DrawComboBoxButton(SpriteBatch spriteBatch)
    {
        var absolutePos = GetAbsolutePosition();
        var bounds = new Rectangle((int)absolutePos.X, (int)absolutePos.Y, (int)Size.X, (int)Size.Y);

        // Draw border
        if (BorderWidth > 0)
        {
            DrawBorder(spriteBatch, bounds, BorderColor);
        }

        // Draw background
        var innerBounds = new Rectangle(
            bounds.X + BorderWidth,
            bounds.Y + BorderWidth,
            bounds.Width - BorderWidth * 2,
            bounds.Height - BorderWidth * 2
        );
        spriteBatch.Draw(_whitePixel, innerBounds, BackgroundColor * Opacity);

        // Draw text
        DrawButtonText(spriteBatch, innerBounds);

        // Draw dropdown arrow
        DrawArrow(spriteBatch, bounds);
    }

    /// <summary>
    /// Draws the dropdown list
    /// </summary>
    private void DrawDropdown(SpriteBatch spriteBatch)
    {
        var dropdownBounds = GetDropdownBounds();
        var mousePosition = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);

        // Draw dropdown border
        if (BorderWidth > 0)
        {
            DrawBorder(spriteBatch, dropdownBounds, DropdownBorderColor);
        }

        // Draw dropdown background
        var innerBounds = new Rectangle(
            dropdownBounds.X + BorderWidth,
            dropdownBounds.Y + BorderWidth,
            dropdownBounds.Width - BorderWidth * 2,
            dropdownBounds.Height - BorderWidth * 2
        );
        spriteBatch.Draw(_whitePixel, innerBounds, DropdownBackgroundColor * Opacity);

        // Draw items
        for (var i = 0; i < Math.Min(_items.Count, MaxVisibleItems); i++)
        {
            var item = _items[i];
            var itemBounds = new Rectangle(
                innerBounds.X,
                innerBounds.Y + (int)(i * ItemHeight),
                innerBounds.Width,
                (int)ItemHeight
            );

            // Determine item background color
            var itemBgColor = DropdownBackgroundColor;
            if (i == _selectedIndex)
            {
                itemBgColor = SelectedItemColor;
            }
            else if (itemBounds.Contains(mousePosition))
            {
                itemBgColor = HoverItemColor;
            }

            // Draw item background
            spriteBatch.Draw(_whitePixel, itemBounds, itemBgColor * Opacity);

            // Draw item text
            if (_font != null && !string.IsNullOrEmpty(item.Text))
            {
                var textColor = item.IsEnabled ? TextColor : Color.Gray;
                var textPosition = new Vector2(
                    itemBounds.X + 8,
                    itemBounds.Y + (ItemHeight - _font.MeasureString(item.Text).Y) / 2
                );
                spriteBatch.DrawString(_font, item.Text, textPosition, textColor * Opacity);
            }
        }
    }

    /// <summary>
    /// Draws text on the ComboBox button
    /// </summary>
    private void DrawButtonText(SpriteBatch spriteBatch, Rectangle bounds)
    {
        if (_font == null)
        {
            return;
        }

        string displayText;
        if (_selectedIndex >= 0 && _selectedIndex < _items.Count)
        {
            displayText = _items[_selectedIndex].Text;
        }
        else
        {
            displayText = PlaceholderText;
        }

        if (string.IsNullOrEmpty(displayText))
        {
            return;
        }

        // Measure and position the text (left-aligned with padding)
        var textPosition = new Vector2(
            bounds.X + 8, // 8px left padding
            bounds.Y + (bounds.Height - _font.MeasureString(displayText).Y) / 2f
        );

        var textColor = _selectedIndex >= 0 ? TextColor : Color.Gray;
        spriteBatch.DrawString(_font, displayText, textPosition, textColor * Opacity);
    }

    /// <summary>
    /// Draws a simple dropdown arrow
    /// </summary>
    private void DrawArrow(SpriteBatch spriteBatch, Rectangle bounds)
    {
        // Simple arrow using rectangles (triangle pointing down)
        var arrowSize = 6;
        var arrowX = bounds.Right - arrowSize - 8;
        var arrowY = bounds.Y + (bounds.Height - arrowSize) / 2;

        // Draw a simple downward triangle using lines/rectangles
        for (var i = 0; i < arrowSize / 2; i++)
        {
            var lineWidth = (i + 1) * 2;
            var lineX = arrowX + (arrowSize - lineWidth) / 2;
            var lineRect = new Rectangle(lineX, arrowY + i, lineWidth, 1);
            spriteBatch.Draw(_whitePixel, lineRect, ArrowColor * Opacity);
        }
    }

    /// <summary>
    /// Draws a border around the specified bounds
    /// </summary>
    private void DrawBorder(SpriteBatch spriteBatch, Rectangle bounds, Color borderColor)
    {
        var color = borderColor * Opacity;

        // Top border
        spriteBatch.Draw(_whitePixel, new Rectangle(bounds.X, bounds.Y, bounds.Width, BorderWidth), color);
        // Bottom border
        spriteBatch.Draw(
            _whitePixel,
            new Rectangle(bounds.X, bounds.Bottom - BorderWidth, bounds.Width, BorderWidth),
            color
        );
        // Left border
        spriteBatch.Draw(_whitePixel, new Rectangle(bounds.X, bounds.Y, BorderWidth, bounds.Height), color);
        // Right border
        spriteBatch.Draw(
            _whitePixel,
            new Rectangle(bounds.Right - BorderWidth, bounds.Y, BorderWidth, bounds.Height),
            color
        );
    }

    #region Properties

    /// <summary>
    /// Background color of the ComboBox
    /// </summary>
    public Color BackgroundColor { get; set; }

    /// <summary>
    /// Foreground color of the ComboBox
    /// </summary>
    public Color ForegroundColor { get; set; }

    /// <summary>
    /// Border color of the ComboBox
    /// </summary>
    public Color BorderColor { get; set; }

    /// <summary>
    /// Border width in pixels
    /// </summary>
    public int BorderWidth { get; set; }

    /// <summary>
    /// Text color
    /// </summary>
    public Color TextColor { get; set; }

    /// <summary>
    /// Background color of dropdown items
    /// </summary>
    public Color DropdownBackgroundColor { get; set; }

    /// <summary>
    /// Border color of dropdown
    /// </summary>
    public Color DropdownBorderColor { get; set; }

    /// <summary>
    /// Color of selected item in dropdown
    /// </summary>
    public Color SelectedItemColor { get; set; }

    /// <summary>
    /// Color when hovering over items
    /// </summary>
    public Color HoverItemColor { get; set; }

    /// <summary>
    /// Color of the dropdown arrow
    /// </summary>
    public Color ArrowColor { get; set; }

    /// <summary>
    /// Opacity of the combobox (0.0 to 1.0)
    /// </summary>
    public float Opacity { get; set; } = 1.0f;

    /// <summary>
    /// Currently selected item index (-1 if none selected)
    /// </summary>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (value >= -1 && value < _items.Count && _selectedIndex != value)
            {
                var oldIndex = _selectedIndex;
                _selectedIndex = value;
                SelectedIndexChanged?.Invoke(this, new SelectedIndexChangedEventArgs(oldIndex, _selectedIndex));
            }
        }
    }

    /// <summary>
    /// Currently selected item (null if none selected)
    /// </summary>
    public ComboItem? SelectedItem => _selectedIndex >= 0 && _selectedIndex < _items.Count ? _items[_selectedIndex] : null;

    /// <summary>
    /// Text of the currently selected item
    /// </summary>
    public string? SelectedText => SelectedItem?.Text;

    /// <summary>
    /// Value of the currently selected item
    /// </summary>
    public object? SelectedValue => SelectedItem?.Value;

    /// <summary>
    /// Whether the dropdown is currently open
    /// </summary>
    public bool IsDropdownOpen { get; private set; }

    /// <summary>
    /// Placeholder text when no item is selected
    /// </summary>
    public string PlaceholderText { get; set; } = "Select an item...";

    /// <summary>
    /// Collection of items in the ComboBox
    /// </summary>
    public IReadOnlyList<ComboItem> Items => _items.AsReadOnly();

    /// <summary>
    /// Event fired when the selected index changes
    /// </summary>
    public event EventHandler<SelectedIndexChangedEventArgs>? SelectedIndexChanged;

    /// <summary>
    /// Event fired when the dropdown is opened
    /// </summary>
    public event EventHandler? DropdownOpened;

    /// <summary>
    /// Event fired when the dropdown is closed
    /// </summary>
    public event EventHandler? DropdownClosed;

    #endregion

    #region Item Management

    /// <summary>
    /// Adds an item to the ComboBox
    /// </summary>
    /// <param name="item">Item to add</param>
    public void AddItem(ComboItem item)
    {
        _items.Add(item);
        UpdateDropdownHeight();
    }

    /// <summary>
    /// Adds an item with text and optional value
    /// </summary>
    /// <param name="text">Display text</param>
    /// <param name="value">Associated value</param>
    public void AddItem(string text, object? value = null)
    {
        AddItem(new ComboItem(text, value));
    }

    /// <summary>
    /// Adds multiple items
    /// </summary>
    /// <param name="items">Items to add</param>
    public void AddItems(IEnumerable<ComboItem> items)
    {
        _items.AddRange(items);
        UpdateDropdownHeight();
    }

    /// <summary>
    /// Adds multiple items from text array
    /// </summary>
    /// <param name="texts">Text array</param>
    public void AddItems(IEnumerable<string> texts)
    {
        _items.AddRange(texts.Select(text => new ComboItem(text)));
        UpdateDropdownHeight();
    }

    /// <summary>
    /// Removes an item at the specified index
    /// </summary>
    /// <param name="index">Index to remove</param>
    public bool RemoveItemAt(int index)
    {
        if (index < 0 || index >= _items.Count)
        {
            return false;
        }

        _items.RemoveAt(index);

        // Adjust selected index if necessary
        if (_selectedIndex == index)
        {
            _selectedIndex = -1;
        }
        else if (_selectedIndex > index)
        {
            _selectedIndex--;
        }

        UpdateDropdownHeight();
        return true;
    }

    /// <summary>
    /// Removes all items
    /// </summary>
    public void ClearItems()
    {
        _items.Clear();
        _selectedIndex = -1;
        IsDropdownOpen = false;
        UpdateDropdownHeight();
    }

    /// <summary>
    /// Finds the index of an item by its text
    /// </summary>
    /// <param name="text">Text to find</param>
    /// <returns>Index or -1 if not found</returns>
    public int FindItemIndex(string text)
    {
        return _items.FindIndex(item => item.Text == text);
    }

    /// <summary>
    /// Selects an item by its text
    /// </summary>
    /// <param name="text">Text to select</param>
    /// <returns>True if item was found and selected</returns>
    public bool SelectItem(string text)
    {
        var index = FindItemIndex(text);
        if (index >= 0)
        {
            SelectedIndex = index;
            return true;
        }

        return false;
    }

    #endregion
}
