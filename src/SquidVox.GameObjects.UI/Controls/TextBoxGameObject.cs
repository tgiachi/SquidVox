using System.Text;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Graphics;
using SquidVox.Core.Context;
using SquidVox.Core.GameObjects;
using SquidVox.Core.Interfaces.Services;
using SquidVox.GameObjects.UI.Events;
using SquidVox.GameObjects.UI.Utils;

namespace SquidVox.GameObjects.UI.Controls;

/// <summary>
/// TextBox game object for text input with cursor, selection, and various input features.
/// </summary>
public class TextBoxGameObject : Base2dGameObject
{
    private const float CursorBlinkInterval = 0.5f;
    private const float CursorWidth = 1f;
    private const float TextPadding = 8f;
    private readonly string _fontName;
    private readonly int _fontSize;
    private readonly StringBuilder _inputBuffer = new();
    private IAssetManagerService? _assetManagerService;
    private float _cursorBlinkTimer;
    private int _cursorPosition;
    private DynamicSpriteFont? _font;
    private bool _isFocused;
    private bool _isInitialized;
    private int? _maxLength;
    private char? _passwordChar;
    private string _placeholderText = "Type here...";

    private KeyboardState _previousKeyboardState;
    private MouseState _previousMouseState;
    private int _selectionEnd = -1;
    private int _selectionStart = -1;
    private string _text = string.Empty;
    private float _textOffset;

    /// <summary>
    /// Event fired when text changes.
    /// </summary>
    public event EventHandler<TextChangedEventArgs>? TextChanged;

    /// <summary>
    /// Event fired when TextBox gains focus.
    /// </summary>
    public event EventHandler? GotFocus;

    /// <summary>
    /// Event fired when TextBox loses focus.
    /// </summary>
    public event EventHandler? LostFocus;

    /// <summary>
    /// Event fired when Enter key is pressed.
    /// </summary>
    public event EventHandler? EnterPressed;

    /// <summary>
    /// Initializes a new TextBox game object.
    /// </summary>
    /// <param name="width">Width of the TextBox.</param>
    /// <param name="height">Height of the TextBox.</param>
    /// <param name="fontName">Font name for text display.</param>
    /// <param name="fontSize">Font size for text display.</param>
    /// <param name="assetManagerService">Asset manager service for loading resources.</param>
    public TextBoxGameObject(
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
        _assetManagerService = assetManagerService;

        // Default styling
        BackgroundColor = Color.White;
        ForegroundColor = Color.Black;
        BorderColor = Color.Gray;
        FocusedBorderColor = Color.CornflowerBlue;
        BorderWidth = 1;
        TextColor = Color.Black;
        PlaceholderColor = Color.Gray;
        SelectionColor = Color.CornflowerBlue;
        CursorColor = Color.Black;
    }

    /// <summary>
    /// Initializes the textbox resources.
    /// </summary>
    public void Initialize(IAssetManagerService assetManagerService, GraphicsDevice graphicsDevice)
    {
        if (_isInitialized)
        {
            return;
        }

        _assetManagerService = assetManagerService;
        LoadFont();

        _isInitialized = true;
    }

    /// <summary>
    /// Loads the font for text display.
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
    /// Updates the textbox state.
    /// </summary>
    protected override void OnUpdate(GameTime gameTime)
    {
        if (_isFocused)
        {
            _cursorBlinkTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_cursorBlinkTimer > CursorBlinkInterval * 2)
            {
                _cursorBlinkTimer = 0f;
            }
        }

        HandleInput();
    }

    /// <summary>
    /// Handles keyboard and mouse input.
    /// </summary>
    private void HandleInput()
    {
        var currentKeyboardState = Keyboard.GetState();
        var currentMouseState = Mouse.GetState();

        HandleMouseInput(currentMouseState);

        if (_isFocused && !IsReadOnly)
        {
            HandleKeyboardInput(currentKeyboardState);
        }

        _previousKeyboardState = currentKeyboardState;
        _previousMouseState = currentMouseState;
    }

    /// <summary>
    /// Handles mouse input for focus and cursor positioning.
    /// </summary>
    private void HandleMouseInput(MouseState currentMouseState)
    {
        var mousePosition = new Vector2(currentMouseState.X, currentMouseState.Y);
        var absolutePos = GetAbsolutePosition();
        var bounds = new Rectangle((int)absolutePos.X, (int)absolutePos.Y, (int)Size.X, (int)Size.Y);

        if (currentMouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
        {
            if (bounds.Contains(mousePosition))
            {
                IsFocused = true;

                // Position cursor based on mouse click
                if (_font != null)
                {
                    var relativeX = mousePosition.X - absolutePos.X - TextPadding + _textOffset;
                    _cursorPosition = GetCharacterIndexAtPosition(relativeX);
                    ClearSelection();
                }
            }
            else
            {
                IsFocused = false;
            }
        }
    }

    /// <summary>
    /// Handles keyboard input for text editing.
    /// </summary>
    private void HandleKeyboardInput(KeyboardState currentKeyboardState)
    {
        var pressedKeys = currentKeyboardState.GetPressedKeys();
        var isShiftPressed = currentKeyboardState.IsKeyDown(Keys.LeftShift) ||
                             currentKeyboardState.IsKeyDown(Keys.RightShift);
        var isCtrlPressed = currentKeyboardState.IsKeyDown(Keys.LeftControl) ||
                            currentKeyboardState.IsKeyDown(Keys.RightControl);

        foreach (var key in pressedKeys)
        {
            if (!_previousKeyboardState.IsKeyDown(key))
            {
                HandleKeyPress(key, isShiftPressed, isCtrlPressed);
            }
        }
    }

    /// <summary>
    /// Handles individual key presses.
    /// </summary>
    private void HandleKeyPress(Keys key, bool isShiftPressed, bool isCtrlPressed)
    {
        switch (key)
        {
            case Keys.Back:
                HandleBackspace();
                break;
            case Keys.Delete:
                HandleDelete();
                break;
            case Keys.Left:
                HandleLeftArrow(isShiftPressed);
                break;
            case Keys.Right:
                HandleRightArrow(isShiftPressed);
                break;
            case Keys.Home:
                HandleHome(isShiftPressed);
                break;
            case Keys.End:
                HandleEnd(isShiftPressed);
                break;
            case Keys.A when isCtrlPressed:
                SelectAll();
                break;
            case Keys.C when isCtrlPressed:
                // Copy to clipboard would go here
                break;
            case Keys.V when isCtrlPressed:
                // Paste from clipboard would go here
                break;
            case Keys.X when isCtrlPressed:
                // Cut to clipboard would go here
                break;
            case Keys.Enter:
                EnterPressed?.Invoke(this, EventArgs.Empty);
                break;
            default:
                HandleCharacterInput(key, isShiftPressed);
                break;
        }
    }

    /// <summary>
    /// Handles character input
    /// </summary>
    private void HandleCharacterInput(Keys key, bool isShiftPressed)
    {
        var character = KeysHelper.GetCharFromKey(key, isShiftPressed);
        if (character.HasValue && !char.IsControl(character.Value))
        {
            InsertCharacter(character.Value);
        }
    }

    /// <summary>
    /// Inserts a character at the current cursor position
    /// </summary>
    private void InsertCharacter(char character)
    {
        if (_maxLength.HasValue && _text.Length >= _maxLength.Value)
        {
            return;
        }

        if (HasSelection)
        {
            DeleteSelection();
        }

        var newText = _text.Insert(_cursorPosition, character.ToString());
        _text = newText;
        _cursorPosition++;
        UpdateTextOffset();
        TextChanged?.Invoke(this, new TextChangedEventArgs(_text.Remove(_cursorPosition - 1, 1), _text));
    }

    /// <summary>
    /// Handles backspace key
    /// </summary>
    private void HandleBackspace()
    {
        if (HasSelection)
        {
            DeleteSelection();
        }
        else if (_cursorPosition > 0)
        {
            var oldText = _text;
            _text = _text.Remove(_cursorPosition - 1, 1);
            _cursorPosition--;
            UpdateTextOffset();
            TextChanged?.Invoke(this, new TextChangedEventArgs(oldText, _text));
        }
    }

    /// <summary>
    /// Handles delete key
    /// </summary>
    private void HandleDelete()
    {
        if (HasSelection)
        {
            DeleteSelection();
        }
        else if (_cursorPosition < _text.Length)
        {
            var oldText = _text;
            _text = _text.Remove(_cursorPosition, 1);
            UpdateTextOffset();
            TextChanged?.Invoke(this, new TextChangedEventArgs(oldText, _text));
        }
    }

    /// <summary>
    /// Handles left arrow key
    /// </summary>
    private void HandleLeftArrow(bool isShiftPressed)
    {
        if (isShiftPressed)
        {
            if (_selectionStart == -1)
            {
                _selectionStart = _cursorPosition;
            }

            if (_cursorPosition > 0)
            {
                _cursorPosition--;
                _selectionEnd = _cursorPosition;
            }
        }
        else
        {
            ClearSelection();
            if (_cursorPosition > 0)
            {
                _cursorPosition--;
            }
        }

        UpdateTextOffset();
    }

    /// <summary>
    /// Handles right arrow key
    /// </summary>
    private void HandleRightArrow(bool isShiftPressed)
    {
        if (isShiftPressed)
        {
            if (_selectionStart == -1)
            {
                _selectionStart = _cursorPosition;
            }

            if (_cursorPosition < _text.Length)
            {
                _cursorPosition++;
                _selectionEnd = _cursorPosition;
            }
        }
        else
        {
            ClearSelection();
            if (_cursorPosition < _text.Length)
            {
                _cursorPosition++;
            }
        }

        UpdateTextOffset();
    }

    /// <summary>
    /// Handles Home key
    /// </summary>
    private void HandleHome(bool isShiftPressed)
    {
        if (isShiftPressed)
        {
            if (_selectionStart == -1)
            {
                _selectionStart = _cursorPosition;
            }

            _cursorPosition = 0;
            _selectionEnd = _cursorPosition;
        }
        else
        {
            ClearSelection();
            _cursorPosition = 0;
        }

        UpdateTextOffset();
    }

    /// <summary>
    /// Handles End key
    /// </summary>
    private void HandleEnd(bool isShiftPressed)
    {
        if (isShiftPressed)
        {
            if (_selectionStart == -1)
            {
                _selectionStart = _cursorPosition;
            }

            _cursorPosition = _text.Length;
            _selectionEnd = _cursorPosition;
        }
        else
        {
            ClearSelection();
            _cursorPosition = _text.Length;
        }

        UpdateTextOffset();
    }

    /// <summary>
    /// Selects all text
    /// </summary>
    public void SelectAll()
    {
        if (_text.Length > 0)
        {
            _selectionStart = 0;
            _selectionEnd = _text.Length;
            _cursorPosition = _text.Length;
        }
    }

    /// <summary>
    /// Clears the current selection
    /// </summary>
    public void ClearSelection()
    {
        _selectionStart = -1;
        _selectionEnd = -1;
    }

    /// <summary>
    /// Deletes the currently selected text
    /// </summary>
    private void DeleteSelection()
    {
        if (!HasSelection)
        {
            return;
        }

        var start = Math.Min(_selectionStart, _selectionEnd);
        var length = Math.Abs(_selectionEnd - _selectionStart);
        var oldText = _text;
        _text = _text.Remove(start, length);
        _cursorPosition = start;
        ClearSelection();
        UpdateTextOffset();
        TextChanged?.Invoke(this, new TextChangedEventArgs(oldText, _text));
    }

    /// <summary>
    /// Gets the character index at the given X position
    /// </summary>
    private int GetCharacterIndexAtPosition(float x)
    {
        if (_font == null || string.IsNullOrEmpty(_text))
        {
            return 0;
        }

        var displayText = GetDisplayText();
        for (var i = 0; i <= displayText.Length; i++)
        {
            var substring = displayText[..i];
            var width = _font.MeasureString(substring).X;
            if (width > x)
            {
                return Math.Max(0, i - 1);
            }
        }

        return displayText.Length;
    }

    /// <summary>
    /// Updates text offset for horizontal scrolling
    /// </summary>
    private void UpdateTextOffset()
    {
        if (_font == null)
        {
            return;
        }

        var displayText = GetDisplayText();
        var cursorText = displayText[.._cursorPosition];
        var cursorX = _font.MeasureString(cursorText).X;
        var textAreaWidth = Size.X - TextPadding * 2;

        // Scroll left if cursor is off the left edge
        if (cursorX + _textOffset < 0)
        {
            _textOffset = -cursorX;
        }
        // Scroll right if cursor is off the right edge
        else if (cursorX + _textOffset > textAreaWidth)
        {
            _textOffset = textAreaWidth - cursorX;
        }
    }

    /// <summary>
    /// Gets the text to display (with password masking if enabled)
    /// </summary>
    private string GetDisplayText()
    {
        if (_passwordChar.HasValue && !string.IsNullOrEmpty(_text))
        {
            return new string(_passwordChar.Value, _text.Length);
        }

        return _text;
    }

    /// <summary>
    /// Renders the TextBox
    /// </summary>
    protected override void OnRender(SpriteBatch spriteBatch)
    {
        if (!IsVisible)
        {
            return;
        }

        var absolutePos = GetAbsolutePosition();
        var bounds = new Rectangle((int)absolutePos.X, (int)absolutePos.Y, (int)Size.X, (int)Size.Y);

        // Draw border
        var borderColor = _isFocused ? FocusedBorderColor : BorderColor;
        if (BorderWidth > 0)
        {
            DrawBorder(spriteBatch, bounds, borderColor);
        }

        // Draw background
        var innerBounds = new Rectangle(
            bounds.X + BorderWidth,
            bounds.Y + BorderWidth,
            bounds.Width - BorderWidth * 2,
            bounds.Height - BorderWidth * 2
        );
        spriteBatch.Draw(SquidVoxEngineContext.WhitePixel, innerBounds, BackgroundColor * Opacity);

        // Set up clipping for text area
        var textBounds = new Rectangle(
            innerBounds.X + (int)TextPadding,
            innerBounds.Y,
            innerBounds.Width - (int)(TextPadding * 2),
            innerBounds.Height
        );

        // Draw selection background
        if (HasSelection)
        {
            DrawSelection(spriteBatch, textBounds);
        }

        // Draw text
        DrawText(spriteBatch, textBounds);

        // Draw cursor
        if (_isFocused && _cursorBlinkTimer < CursorBlinkInterval)
        {
            DrawCursor(spriteBatch, textBounds);
        }
    }

    /// <summary>
    /// Draws the selection background
    /// </summary>
    private void DrawSelection(SpriteBatch spriteBatch, Rectangle textBounds)
    {
        if (_font == null || !HasSelection)
        {
            return;
        }

        var displayText = GetDisplayText();
        var start = Math.Min(_selectionStart, _selectionEnd);
        var end = Math.Max(_selectionStart, _selectionEnd);

        var beforeSelection = displayText[..start];
        var selection = displayText[start..end];

        var beforeWidth = _font.MeasureString(beforeSelection).X;
        var selectionWidth = _font.MeasureString(selection).X;

        var selectionBounds = new Rectangle(
            textBounds.X + (int)(beforeWidth + _textOffset),
            textBounds.Y,
            (int)selectionWidth,
            textBounds.Height
        );

        spriteBatch.Draw(SquidVoxEngineContext.WhitePixel, selectionBounds, SelectionColor * Opacity);
    }

    /// <summary>
    /// Draws the text content
    /// </summary>
    private void DrawText(SpriteBatch spriteBatch, Rectangle textBounds)
    {
        if (_font == null)
        {
            return;
        }

        string displayText;
        Color textColor;

        if (string.IsNullOrEmpty(_text))
        {
            displayText = _placeholderText;
            textColor = PlaceholderColor;
        }
        else
        {
            displayText = GetDisplayText();
            textColor = TextColor;
        }

        if (!string.IsNullOrEmpty(displayText))
        {
            var textPosition = new Vector2(
                textBounds.X + _textOffset,
                textBounds.Y + (textBounds.Height - _font.MeasureString(displayText).Y) / 2f
            );

            // TODO: Implement proper text clipping
            spriteBatch.DrawString(_font, displayText, textPosition, textColor * Opacity);
        }
    }

    /// <summary>
    /// Draws the cursor
    /// </summary>
    private void DrawCursor(SpriteBatch spriteBatch, Rectangle textBounds)
    {
        if (_font == null)
        {
            return;
        }

        var displayText = GetDisplayText();
        var cursorText = displayText[.._cursorPosition];
        var cursorX = _font.MeasureString(cursorText).X;

        var cursorBounds = new Rectangle(
            textBounds.X + (int)(cursorX + _textOffset),
            textBounds.Y + 2,
            (int)CursorWidth,
            textBounds.Height - 4
        );

        spriteBatch.Draw(SquidVoxEngineContext.WhitePixel, cursorBounds, CursorColor * Opacity);
    }

    /// <summary>
    /// Draws a border around the specified bounds
    /// </summary>
    private void DrawBorder(SpriteBatch spriteBatch, Rectangle bounds, Color borderColor)
    {
        var color = borderColor * Opacity;

        // Top border
        spriteBatch.Draw(SquidVoxEngineContext.WhitePixel, new Rectangle(bounds.X, bounds.Y, bounds.Width, BorderWidth), color);
        // Bottom border
        spriteBatch.Draw(
            SquidVoxEngineContext.WhitePixel,
            new Rectangle(bounds.X, bounds.Bottom - BorderWidth, bounds.Width, BorderWidth),
            color
        );
        // Left border
        spriteBatch.Draw(SquidVoxEngineContext.WhitePixel, new Rectangle(bounds.X, bounds.Y, BorderWidth, bounds.Height), color);
        // Right border
        spriteBatch.Draw(
            SquidVoxEngineContext.WhitePixel,
            new Rectangle(bounds.Right - BorderWidth, bounds.Y, BorderWidth, bounds.Height),
            color
        );
    }

    /// <summary>
    /// Current text content of the TextBox
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            var newText = value ?? string.Empty;
            if (_maxLength.HasValue && newText.Length > _maxLength.Value)
            {
                newText = newText[.._maxLength.Value];
            }

            if (_text != newText)
            {
                var oldText = _text;
                _text = newText;
                _cursorPosition = Math.Min(_cursorPosition, _text.Length);
                ClearSelection();
                UpdateTextOffset();
                TextChanged?.Invoke(this, new TextChangedEventArgs(oldText, _text));
            }
        }
    }

    /// <summary>
    /// Placeholder text shown when TextBox is empty
    /// </summary>
    public string PlaceholderText
    {
        get => _placeholderText;
        set => _placeholderText = value ?? string.Empty;
    }

    /// <summary>
    /// Current cursor position in the text
    /// </summary>
    public int CursorPosition
    {
        get => _cursorPosition;
        set => _cursorPosition = Math.Clamp(value, 0, _text.Length);
    }

    /// <summary>
    /// Whether the TextBox currently has focus
    /// </summary>
    public bool IsFocused
    {
        get => _isFocused;
        set
        {
            if (_isFocused != value)
            {
                _isFocused = value;
                if (_isFocused)
                {
                    _cursorBlinkTimer = 0f;
                    GotFocus?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    ClearSelection();
                    LostFocus?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }

    /// <summary>
    /// Whether the TextBox is read-only
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Character to use for password masking (null for normal text)
    /// </summary>
    public char? PasswordChar
    {
        get => _passwordChar;
        set => _passwordChar = value;
    }

    /// <summary>
    /// Maximum length of text (null for unlimited)
    /// </summary>
    public int? MaxLength
    {
        get => _maxLength;
        set
        {
            _maxLength = value;
            if (_maxLength.HasValue && _text.Length > _maxLength.Value)
            {
                Text = _text[.._maxLength.Value];
            }
        }
    }

    /// <summary>
    /// Whether text is currently selected
    /// </summary>
    public bool HasSelection => _selectionStart != -1 && _selectionEnd != -1 && _selectionStart != _selectionEnd;

    /// <summary>
    /// Currently selected text
    /// </summary>
    public string SelectedText
    {
        get
        {
            if (!HasSelection)
            {
                return string.Empty;
            }

            var start = Math.Min(_selectionStart, _selectionEnd);
            var length = Math.Abs(_selectionEnd - _selectionStart);
            return _text.Substring(start, length);
        }
    }

    /// <summary>
    /// Opacity of the textbox (0.0 to 1.0)
    /// </summary>
    public float Opacity { get; set; } = 1.0f;

    /// <summary>
    /// Background color of the TextBox
    /// </summary>
    public Color BackgroundColor { get; set; }

    /// <summary>
    /// Foreground color of the TextBox
    /// </summary>
    public Color ForegroundColor { get; set; }

    /// <summary>
    /// Border color of the TextBox
    /// </summary>
    public Color BorderColor { get; set; }

    /// <summary>
    /// Border color when focused
    /// </summary>
    public Color FocusedBorderColor { get; set; }

    /// <summary>
    /// Border width in pixels
    /// </summary>
    public int BorderWidth { get; set; }

    /// <summary>
    /// Text color
    /// </summary>
    public Color TextColor { get; set; }

    /// <summary>
    /// Placeholder text color
    /// </summary>
    public Color PlaceholderColor { get; set; }

    /// <summary>
    /// Selection background color
    /// </summary>
    public Color SelectionColor { get; set; }

    /// <summary>
    /// Cursor color
    /// </summary>
    public Color CursorColor { get; set; }
}
