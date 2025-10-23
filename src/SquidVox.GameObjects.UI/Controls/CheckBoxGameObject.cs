using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Graphics;
using SquidVox.Core.Context;
using SquidVox.Core.GameObjects;
using SquidVox.Core.Interfaces.Services;
using SquidVox.GameObjects.UI.Events;
using SquidVox.GameObjects.UI.Types;

namespace SquidVox.GameObjects.UI.Controls;

/// <summary>
/// CheckBox game object with customizable styling and state management
/// </summary>
public class CheckBoxGameObject : Base2dGameObject
{
    private readonly string _fontName;
    private readonly int _fontSize;
    private IAssetManagerService? _assetManagerService;
    private Texture2D? _checkboxCheckedTexture;
    private Texture2D? _checkboxUncheckedTexture;
    private DynamicSpriteFont? _font;
    private bool _isChecked;
    private bool _isHovered;
    private bool _isInitialized;
    private bool _isPressed;

    private MouseState _previousMouseState;
    private string _text;

    /// <summary>
    /// Event fired when the checked state changes
    /// </summary>
    public event EventHandler<bool>? CheckedChanged;

    /// <summary>
    /// Event fired when the checkbox is clicked
    /// </summary>
    public event EventHandler? Click;

    /// <summary>
    /// Initializes a new CheckBox game object
    /// </summary>
    /// <param name="text">CheckBox label text</param>
    /// <param name="isChecked">Initial checked state</param>
    /// <param name="fontName">Font name for text display</param>
    /// <param name="fontSize">Font size for text display</param>
    /// <param name="assetManagerService">Asset manager service for loading resources</param>
    public CheckBoxGameObject(
        string text = "CheckBox",
        bool isChecked = false,
        string fontName = "Monocraft",
        int fontSize = 14,
        IAssetManagerService? assetManagerService = null
    )
    {
        _text = text;
        _isChecked = isChecked;
        _fontName = fontName;
        _fontSize = fontSize;
        _assetManagerService = assetManagerService;

        // Default styling
        SetDefaultColors();
        SetDefaultSize();
    }

    /// <summary>
    /// Gets or sets the checkbox text
    /// </summary>
    public string Text
    {
        get => _text;
        set => _text = value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets whether the checkbox is checked
    /// </summary>
    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked != value)
            {
                _isChecked = value;
                CheckedChanged?.Invoke(this, _isChecked);
            }
        }
    }

    /// <summary>
    /// Size of the checkbox square
    /// </summary>
    public float CheckBoxSize { get; set; } = 16f;

    /// <summary>
    /// Spacing between checkbox and text
    /// </summary>
    public float TextSpacing { get; set; } = 8f;

    /// <summary>
    /// Border width for the checkbox
    /// </summary>
    public float BorderWidth { get; set; } = 1f;

    /// <summary>
    /// Text alignment relative to checkbox
    /// </summary>
    public TextAlignment TextAlignment { get; set; } = TextAlignment.Left;

    /// <summary>
    /// Opacity of the checkbox (0.0 to 1.0)
    /// </summary>
    public float Opacity { get; set; } = 1.0f;

    // Color properties for different states
    public Color NormalBackgroundColor { get; set; }
    public Color HoverBackgroundColor { get; set; }
    public Color PressedBackgroundColor { get; set; }
    public Color DisabledBackgroundColor { get; set; }
    public Color CheckedBackgroundColor { get; set; }

    public Color NormalBorderColor { get; set; }
    public Color HoverBorderColor { get; set; }
    public Color PressedBorderColor { get; set; }
    public Color DisabledBorderColor { get; set; }
    public Color CheckedBorderColor { get; set; }

    public Color NormalTextColor { get; set; }
    public Color HoverTextColor { get; set; }
    public Color PressedTextColor { get; set; }
    public Color DisabledTextColor { get; set; }

    public Color CheckMarkColor { get; set; }
    public Color DisabledCheckMarkColor { get; set; }

    /// <summary>
    /// Gets or sets the font color for the text
    /// </summary>
    public Color FontColor
    {
        get => NormalTextColor;
        set
        {
            NormalTextColor = value;
            HoverTextColor = value;
            PressedTextColor = value;
        }
    }

    /// <summary>
    /// Sets default color scheme
    /// </summary>
    private void SetDefaultColors()
    {
        // Checkbox background colors
        NormalBackgroundColor = Color.White;
        HoverBackgroundColor = new Color(248, 248, 248);
        PressedBackgroundColor = new Color(230, 230, 230);
        DisabledBackgroundColor = new Color(245, 245, 245);
        CheckedBackgroundColor = new Color(0, 120, 215);

        // Checkbox border colors
        NormalBorderColor = new Color(118, 118, 118);
        HoverBorderColor = new Color(0, 120, 215);
        PressedBorderColor = new Color(0, 84, 153);
        DisabledBorderColor = new Color(204, 204, 204);
        CheckedBorderColor = new Color(0, 120, 215);

        // Text colors
        NormalTextColor = Color.Black;
        HoverTextColor = Color.Black;
        PressedTextColor = Color.Black;
        DisabledTextColor = Color.Gray;

        // Check mark colors
        CheckMarkColor = Color.White;
        DisabledCheckMarkColor = new Color(204, 204, 204);
    }

    /// <summary>
    /// Sets default size based on text and checkbox size
    /// </summary>
    private void SetDefaultSize()
    {
        // We'll calculate proper size after font is loaded
        Size = new Vector2(CheckBoxSize + TextSpacing + 100, CheckBoxSize + 4);
    }

    /// <summary>
    /// Initializes the checkbox resources
    /// </summary>
    public void Initialize(IAssetManagerService assetManagerService, GraphicsDevice graphicsDevice)
    {
        if (_isInitialized)
        {
            return;
        }

        _assetManagerService = assetManagerService;
        LoadFont();
        LoadTextures();



        // Recalculate size with loaded font
        RecalculateSize();

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
            // Fall back to default font if loading fails
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
    /// Loads the checkbox textures
    /// </summary>
    private void LoadTextures()
    {
        if (_assetManagerService == null)
        {
            return;
        }

        try
        {
            _checkboxUncheckedTexture = _assetManagerService.GetTexture("checkbox_unchecked");
            _checkboxCheckedTexture = _assetManagerService.GetTexture("checkbox_checked");
        }
        catch
        {
            // If textures fail to load, we'll fall back to programmatic drawing
            _checkboxUncheckedTexture = null;
            _checkboxCheckedTexture = null;
        }
    }

    /// <summary>
    /// Recalculates component size based on text and checkbox dimensions
    /// </summary>
    private void RecalculateSize()
    {
        if (_font == null)
        {
            return;
        }

        var textSize = _font.MeasureString(_text);
        Size = new Vector2(
            CheckBoxSize + TextSpacing + textSize.X + 8,
            Math.Max(CheckBoxSize, textSize.Y) + 4
        );
    }

    /// <summary>
    /// Updates the checkbox state
    /// </summary>
    protected override void OnUpdate(GameTime gameTime)
    {
        if (!IsEnabled)
        {
            _isHovered = false;
            _isPressed = false;
            return;
        }

        var mouseState = Mouse.GetState();
        var mousePosition = new Vector2(mouseState.X, mouseState.Y);
        var absolutePos = GetAbsolutePosition();
        var bounds = new Rectangle((int)absolutePos.X, (int)absolutePos.Y, (int)Size.X, (int)Size.Y);
        var wasHovered = _isHovered;

        _isHovered = bounds.Contains(mousePosition);

        // Handle mouse press/release
        if (_isHovered)
        {
            if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
            {
                _isPressed = true;
                HasFocus = true;
            }
            else if (mouseState.LeftButton == ButtonState.Released &&
                     _previousMouseState.LeftButton == ButtonState.Pressed && _isPressed)
            {
                _isPressed = false;
                IsChecked = !IsChecked;
                Click?.Invoke(this, EventArgs.Empty);
            }
        }
        else
        {
            _isPressed = false;
        }

        if (mouseState.LeftButton == ButtonState.Released)
        {
            _isPressed = false;
        }

        _previousMouseState = mouseState;
    }

    /// <summary>
    /// Gets the current visual state of the checkbox
    /// </summary>
    private UIButtonState GetCurrentState()
    {
        if (!IsEnabled)
        {
            return UIButtonState.Disabled;
        }

        if (_isPressed)
        {
            return UIButtonState.Pressed;
        }

        if (_isHovered)
        {
            return UIButtonState.Hovered;
        }

        return UIButtonState.Normal;
    }

    /// <summary>
    /// Gets the background color for the current state
    /// </summary>
    private Color GetBackgroundColor()
    {
        if (_isChecked && IsEnabled)
        {
            return GetCurrentState() switch
            {
                UIButtonState.Hovered => Color.Lerp(CheckedBackgroundColor, Color.White, 0.1f),
                UIButtonState.Pressed => Color.Lerp(CheckedBackgroundColor, Color.Black, 0.1f),
                _ => CheckedBackgroundColor
            };
        }

        return GetCurrentState() switch
        {
            UIButtonState.Normal => NormalBackgroundColor,
            UIButtonState.Hovered => HoverBackgroundColor,
            UIButtonState.Pressed => PressedBackgroundColor,
            UIButtonState.Disabled => DisabledBackgroundColor,
            _ => NormalBackgroundColor
        };
    }

    /// <summary>
    /// Gets the border color for the current state
    /// </summary>
    private Color GetBorderColor()
    {
        if (_isChecked && IsEnabled)
        {
            return CheckedBorderColor;
        }

        return GetCurrentState() switch
        {
            UIButtonState.Normal => NormalBorderColor,
            UIButtonState.Hovered => HoverBorderColor,
            UIButtonState.Pressed => PressedBorderColor,
            UIButtonState.Disabled => DisabledBorderColor,
            _ => NormalBorderColor
        };
    }

    /// <summary>
    /// Gets the text color for the current state
    /// </summary>
    private Color GetTextColor()
    {
        return GetCurrentState() switch
        {
            UIButtonState.Normal => NormalTextColor,
            UIButtonState.Hovered => HoverTextColor,
            UIButtonState.Pressed => PressedTextColor,
            UIButtonState.Disabled => DisabledTextColor,
            _ => NormalTextColor
        };
    }

    /// <summary>
    /// Renders the checkbox
    /// </summary>
    protected override void OnRender(SpriteBatch spriteBatch)
    {
        if (_font == null)
        {
            return;
        }

        var absolutePos = GetAbsolutePosition();
        var checkBoxRect = new Rectangle(
            (int)absolutePos.X,
            (int)(absolutePos.Y + (Size.Y - CheckBoxSize) / 2),
            (int)CheckBoxSize,
            (int)CheckBoxSize
        );

        // Draw checkbox using textures if available, otherwise fall back to programmatic drawing
        if (_checkboxUncheckedTexture != null && _checkboxCheckedTexture != null)
        {
            var texture = _isChecked ? _checkboxCheckedTexture : _checkboxUncheckedTexture;
            var tintColor = IsEnabled ? Color.White : Color.Gray;

            // Apply hover/pressed effects with slight tinting
            if (IsEnabled && _isHovered && !_isPressed)
            {
                tintColor = Color.Lerp(Color.White, Color.LightBlue, 0.2f);
            }
            else if (IsEnabled && _isPressed)
            {
                tintColor = Color.Lerp(Color.White, Color.Blue, 0.1f);
            }

            spriteBatch.Draw(texture, checkBoxRect, tintColor * Opacity);
        }
        else
        {
            // Fall back to programmatic drawing
            DrawCheckboxFallback(spriteBatch, checkBoxRect);
        }

        // Draw text
        if (!string.IsNullOrEmpty(_text))
        {
            var textPosition = new Vector2(
                checkBoxRect.Right + TextSpacing,
                absolutePos.Y + (Size.Y - _font.MeasureString(_text).Y) / 2
            );

            spriteBatch.DrawString(_font, _text, textPosition, GetTextColor() * Opacity);
        }
    }

    /// <summary>
    /// Draws the check mark inside the checkbox
    /// </summary>
    private void DrawCheckMark(SpriteBatch spriteBatch, Rectangle checkBoxRect)
    {

        var checkColor = IsEnabled ? CheckMarkColor : DisabledCheckMarkColor;
        var center = checkBoxRect.Center;
        var size = (int)(CheckBoxSize * 0.6f);
        var thickness = Math.Max(1, (int)(CheckBoxSize * 0.1f));

        // Draw a simple check mark using lines
        var startX = center.X - size / 3;
        var startY = center.Y;
        var midX = center.X - size / 6;
        var midY = center.Y + size / 3;
        var endX = center.X + size / 2;
        var endY = center.Y - size / 3;

        // Draw the check mark as thick lines
        DrawLine(spriteBatch, new Vector2(startX, startY), new Vector2(midX, midY), thickness, checkColor);
        DrawLine(spriteBatch, new Vector2(midX, midY), new Vector2(endX, endY), thickness, checkColor);
    }

    /// <summary>
    /// Draws a line between two points
    /// </summary>
    private void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, int thickness, Color color)
    {

        var distance = Vector2.Distance(start, end);
        var angle = (float)Math.Atan2(end.Y - start.Y, end.X - start.X);

        spriteBatch.Draw(
            SquidVoxEngineContext.WhitePixel,
            new Rectangle((int)start.X, (int)start.Y - thickness / 2, (int)distance, thickness),
            null,
            color * Opacity,
            angle,
            Vector2.Zero,
            SpriteEffects.None,
            0
        );
    }

    /// <summary>
    /// Draws checkbox using programmatic drawing when textures are not available
    /// </summary>
    private void DrawCheckboxFallback(SpriteBatch spriteBatch, Rectangle checkBoxRect)
    {

        // Draw checkbox background
        spriteBatch.Draw(SquidVoxEngineContext.WhitePixel, checkBoxRect, GetBackgroundColor() * Opacity);

        // Draw checkbox border
        if (BorderWidth > 0)
        {
            var borderColor = GetBorderColor() * Opacity;
            var borderWidth = (int)BorderWidth;

            // Top border
            spriteBatch.Draw(
                SquidVoxEngineContext.WhitePixel,
                new Rectangle(checkBoxRect.X, checkBoxRect.Y, checkBoxRect.Width, borderWidth),
                borderColor
            );
            // Bottom border
            spriteBatch.Draw(
                SquidVoxEngineContext.WhitePixel,
                new Rectangle(checkBoxRect.X, checkBoxRect.Bottom - borderWidth, checkBoxRect.Width, borderWidth),
                borderColor
            );
            // Left border
            spriteBatch.Draw(
                SquidVoxEngineContext.WhitePixel,
                new Rectangle(checkBoxRect.X, checkBoxRect.Y, borderWidth, checkBoxRect.Height),
                borderColor
            );
            // Right border
            spriteBatch.Draw(
                SquidVoxEngineContext.WhitePixel,
                new Rectangle(checkBoxRect.Right - borderWidth, checkBoxRect.Y, borderWidth, checkBoxRect.Height),
                borderColor
            );
        }

        // Draw check mark if checked
        if (_isChecked)
        {
            DrawCheckMark(spriteBatch, checkBoxRect);
        }
    }

    /// <summary>
    /// Toggles the checked state
    /// </summary>
    public void Toggle()
    {
        if (IsEnabled)
        {
            IsChecked = !IsChecked;
        }
    }

    /// <summary>
    /// Sets the checked state without triggering events
    /// </summary>
    /// <param name="isChecked">New checked state</param>
    public void SetCheckedSilent(bool isChecked)
    {
        _isChecked = isChecked;
    }
}
