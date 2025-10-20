using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Graphics;
using SquidVox.Core.GameObjects;
using SquidVox.Core.Interfaces.Services;
using SquidVox.GameObjects.UI.Data;
using SquidVox.GameObjects.UI.Types;

namespace SquidVox.GameObjects.UI.Controls;

/// <summary>
/// ListBox game object for displaying and selecting items from a list
/// </summary>
public class ListBoxGameObject : Base2dGameObject
{
    private readonly string _fontName;
    private readonly int _fontSize;
    private readonly List<ListBoxItem> _items = new();
    private readonly HashSet<int> _selectedIndices = new();
    private IAssetManagerService? _assetManagerService;
    private DynamicSpriteFont? _font;
    private int _hoveredIndex = -1;
    private bool _isInitialized;

    private MouseState _previousMouseState;
    private int _scrollOffset;
    private Texture2D? _whitePixel;

    public ListBoxGameObject(
        float width = 200f,
        float height = 150f,
        string fontName = "Monocraft",
        int fontSize = 14,
        IAssetManagerService? assetManagerService = null
    )
    {
        _fontName = fontName;
        _fontSize = fontSize;
        Size = new Vector2(width, height);
        _assetManagerService = assetManagerService;

        SetDefaultColors();
    }

    public void Initialize(IAssetManagerService assetManagerService, GraphicsDevice graphicsDevice)
    {
        if (_isInitialized)
        {
            return;
        }

        _assetManagerService = assetManagerService;
        LoadFont();

        _whitePixel = new Texture2D(graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });

        _isInitialized = true;
    }

    private void SetDefaultColors()
    {
        BackgroundColor = Color.White;
        BorderColor = Color.Gray;
        DisabledBackgroundColor = new Color(245, 245, 245);
        DisabledBorderColor = new Color(204, 204, 204);

        ItemBackgroundColor = Color.White;
        AlternateItemBackgroundColor = new Color(248, 248, 248);
        HoverItemBackgroundColor = Color.LightGray;
        SelectedItemBackgroundColor = Color.CornflowerBlue;
        DisabledItemBackgroundColor = new Color(240, 240, 240);

        ItemTextColor = Color.Black;
        HoverItemTextColor = Color.Black;
        SelectedItemTextColor = Color.White;
        DisabledItemTextColor = Color.Gray;
    }

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

    protected override void OnUpdate(GameTime gameTime)
    {
        if (!IsEnabled)
        {
            return;
        }

        HandleMouseInput();
    }

    private void HandleMouseInput()
    {
        var mouseState = Mouse.GetState();
        var mousePosition = new Vector2(mouseState.X, mouseState.Y);
        var absolutePos = GetAbsolutePosition();
        var bounds = new Rectangle((int)absolutePos.X, (int)absolutePos.Y, (int)Size.X, (int)Size.Y);

        if (!bounds.Contains(mousePosition))
        {
            _hoveredIndex = -1;
            return;
        }

        // Calculate which item is hovered
        var relativeY = mousePosition.Y - absolutePos.Y - BorderWidth;
        var itemIndex = (int)(relativeY / ItemHeight) + _scrollOffset;

        if (itemIndex >= 0 && itemIndex < _items.Count)
        {
            _hoveredIndex = itemIndex;

            // Handle click
            if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
            {
                HandleItemClick(itemIndex, mouseState);
            }
        }
        else
        {
            _hoveredIndex = -1;
        }

        // Handle mouse wheel scrolling
        var scrollDelta = mouseState.ScrollWheelValue - _previousMouseState.ScrollWheelValue;
        if (scrollDelta != 0)
        {
            _scrollOffset -= Math.Sign(scrollDelta);
            _scrollOffset = Math.Clamp(_scrollOffset, 0, Math.Max(0, _items.Count - GetVisibleItemCount()));
        }

        _previousMouseState = mouseState;
    }

    private void HandleItemClick(int itemIndex, MouseState mouseState)
    {
        if (SelectionMode == ListBoxSelectionMode.None)
        {
            return;
        }

        var isCtrlPressed = Keyboard.GetState().IsKeyDown(Keys.LeftControl) ||
                            Keyboard.GetState().IsKeyDown(Keys.RightControl);

        if (SelectionMode == ListBoxSelectionMode.Single)
        {
            if (!_selectedIndices.Contains(itemIndex))
            {
                var oldIndex = SelectedIndex;
                _selectedIndices.Clear();
                _selectedIndices.Add(itemIndex);
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        else if (SelectionMode == ListBoxSelectionMode.Multiple)
        {
            if (isCtrlPressed)
            {
                // Toggle selection
                if (_selectedIndices.Contains(itemIndex))
                {
                    _selectedIndices.Remove(itemIndex);
                }
                else
                {
                    _selectedIndices.Add(itemIndex);
                }
            }
            else
            {
                // Replace selection
                _selectedIndices.Clear();
                _selectedIndices.Add(itemIndex);
            }

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private int GetVisibleItemCount()
    {
        return (int)((Size.Y - BorderWidth * 2) / ItemHeight);
    }

    protected override void OnRender(SpriteBatch spriteBatch)
    {
        if (_whitePixel == null || _font == null)
        {
            return;
        }

        var absolutePos = GetAbsolutePosition();
        var bounds = new Rectangle((int)absolutePos.X, (int)absolutePos.Y, (int)Size.X, (int)Size.Y);

        // Draw background
        var bgColor = IsEnabled ? BackgroundColor : DisabledBackgroundColor;
        spriteBatch.Draw(_whitePixel, bounds, bgColor * Opacity);

        // Draw border
        if (BorderWidth > 0)
        {
            var borderColor = IsEnabled ? BorderColor : DisabledBorderColor;
            DrawBorder(spriteBatch, bounds, borderColor);
        }

        // Draw items
        var innerBounds = new Rectangle(
            bounds.X + (int)BorderWidth,
            bounds.Y + (int)BorderWidth,
            bounds.Width - (int)(BorderWidth * 2),
            bounds.Height - (int)(BorderWidth * 2)
        );

        DrawItems(spriteBatch, innerBounds);
    }

    private void DrawItems(SpriteBatch spriteBatch, Rectangle bounds)
    {
        var visibleCount = GetVisibleItemCount();
        var startIndex = _scrollOffset;
        var endIndex = Math.Min(startIndex + visibleCount, _items.Count);

        for (var i = startIndex; i < endIndex; i++)
        {
            var item = _items[i];
            var itemY = bounds.Y + (i - startIndex) * ItemHeight;
            var itemBounds = new Rectangle(bounds.X, (int)itemY, bounds.Width, (int)ItemHeight);

            // Determine background color
            Color itemBgColor;
            if (!IsEnabled)
            {
                itemBgColor = DisabledItemBackgroundColor;
            }
            else if (_selectedIndices.Contains(i))
            {
                itemBgColor = SelectedItemBackgroundColor;
            }
            else if (i == _hoveredIndex)
            {
                itemBgColor = HoverItemBackgroundColor;
            }
            else if (ShowAlternatingRows && i % 2 == 1)
            {
                itemBgColor = AlternateItemBackgroundColor;
            }
            else
            {
                itemBgColor = ItemBackgroundColor;
            }

            // Draw item background
            spriteBatch.Draw(_whitePixel, itemBounds, itemBgColor * Opacity);

            // Draw item text
            if (!string.IsNullOrEmpty(item.Text))
            {
                Color itemTextColor;
                if (!IsEnabled)
                {
                    itemTextColor = DisabledItemTextColor;
                }
                else if (_selectedIndices.Contains(i))
                {
                    itemTextColor = SelectedItemTextColor;
                }
                else if (i == _hoveredIndex)
                {
                    itemTextColor = HoverItemTextColor;
                }
                else
                {
                    itemTextColor = ItemTextColor;
                }

                var textPosition = new Vector2(
                    itemBounds.X + ItemPadding,
                    itemBounds.Y + (ItemHeight - _font.MeasureString(item.Text).Y) / 2
                );

                spriteBatch.DrawString(_font, item.Text, textPosition, itemTextColor * Opacity);
            }
        }
    }

    private void DrawBorder(SpriteBatch spriteBatch, Rectangle bounds, Color borderColor)
    {
        var color = borderColor * Opacity;
        var borderWidth = (int)BorderWidth;

        // Top border
        spriteBatch.Draw(_whitePixel, new Rectangle(bounds.X, bounds.Y, bounds.Width, borderWidth), color);
        // Bottom border
        spriteBatch.Draw(
            _whitePixel,
            new Rectangle(bounds.X, bounds.Bottom - borderWidth, bounds.Width, borderWidth),
            color
        );
        // Left border
        spriteBatch.Draw(_whitePixel, new Rectangle(bounds.X, bounds.Y, borderWidth, bounds.Height), color);
        // Right border
        spriteBatch.Draw(
            _whitePixel,
            new Rectangle(bounds.Right - borderWidth, bounds.Y, borderWidth, bounds.Height),
            color
        );
    }

    #region Item Management

    public void AddItem(ListBoxItem item)
    {
        _items.Add(item);
    }

    public void AddItem(string text, object? value = null)
    {
        AddItem(new ListBoxItem(text, value));
    }

    public void AddItems(IEnumerable<ListBoxItem> items)
    {
        _items.AddRange(items);
    }

    public void AddItems(IEnumerable<string> texts)
    {
        _items.AddRange(texts.Select(text => new ListBoxItem(text)));
    }

    public bool RemoveItemAt(int index)
    {
        if (index < 0 || index >= _items.Count)
        {
            return false;
        }

        _items.RemoveAt(index);

        // Adjust selections
        _selectedIndices.Remove(index);
        var newIndices = new HashSet<int>();
        foreach (var selectedIndex in _selectedIndices)
        {
            if (selectedIndex > index)
            {
                newIndices.Add(selectedIndex - 1);
            }
            else
            {
                newIndices.Add(selectedIndex);
            }
        }

        _selectedIndices.Clear();
        foreach (var idx in newIndices)
        {
            _selectedIndices.Add(idx);
        }

        return true;
    }

    public void ClearItems()
    {
        _items.Clear();
        _selectedIndices.Clear();
        _scrollOffset = 0;
    }

    public void ClearSelection()
    {
        _selectedIndices.Clear();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SelectItem(int index)
    {
        if (index >= 0 && index < _items.Count && SelectionMode != ListBoxSelectionMode.None)
        {
            if (SelectionMode == ListBoxSelectionMode.Single)
            {
                _selectedIndices.Clear();
            }

            _selectedIndices.Add(index);
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    #endregion

    #region Properties

    public ListBoxSelectionMode SelectionMode { get; set; } = ListBoxSelectionMode.Single;
    public float ItemHeight { get; set; } = 24f;
    public float ItemPadding { get; set; } = 8f;
    public float BorderWidth { get; set; } = 1f;
    public bool ShowAlternatingRows { get; set; } = true;
    public float Opacity { get; set; } = 1.0f;

    public Color BackgroundColor { get; set; }
    public Color BorderColor { get; set; }
    public Color DisabledBackgroundColor { get; set; }
    public Color DisabledBorderColor { get; set; }

    public Color ItemBackgroundColor { get; set; }
    public Color AlternateItemBackgroundColor { get; set; }
    public Color HoverItemBackgroundColor { get; set; }
    public Color SelectedItemBackgroundColor { get; set; }
    public Color DisabledItemBackgroundColor { get; set; }

    public Color ItemTextColor { get; set; }
    public Color HoverItemTextColor { get; set; }
    public Color SelectedItemTextColor { get; set; }
    public Color DisabledItemTextColor { get; set; }

    public IReadOnlyList<ListBoxItem> Items => _items.AsReadOnly();
    public IReadOnlySet<int> SelectedIndices => _selectedIndices;

    public int SelectedIndex
    {
        get => _selectedIndices.Count > 0 ? _selectedIndices.Min() : -1;
        set
        {
            if (value >= -1 && value < _items.Count)
            {
                ClearSelection();
                if (value >= 0)
                {
                    SelectItem(value);
                }
            }
        }
    }

    public ListBoxItem? SelectedItem
    {
        get
        {
            var index = SelectedIndex;
            return index >= 0 && index < _items.Count ? _items[index] : null;
        }
    }

    public event EventHandler? SelectionChanged;

    #endregion
}
