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
/// RadioButton game object with group management and customizable styling
/// </summary>
public class RadioButtonGameObject : Base2dGameObject
{
    private readonly string _fontName;
    private readonly int _fontSize;
    private IAssetManagerService? _assetManagerService;
    private DynamicSpriteFont? _font;
    private bool _isHovered;
    private bool _isInitialized;
    private bool _isPressed;
    private bool _isSelected;

    private MouseState _previousMouseState;
    private Texture2D? _radioCheckedTexture;
    private Texture2D? _radioUncheckedTexture;
    private string _text;
    private string _value;

    public event EventHandler<bool>? SelectionChanged;
    public event EventHandler? Click;

    public RadioButtonGameObject(
        string text = "RadioButton",
        string? value = null,
        bool isSelected = false,
        string fontName = "Monocraft",
        int fontSize = 14,
        IAssetManagerService? assetManagerService = null
    )
    {
        _text = text;
        _value = value ?? text;
        _isSelected = isSelected;
        _fontName = fontName;
        _fontSize = fontSize;
        _assetManagerService = assetManagerService;

        SetDefaultColors();
        SetDefaultSize();
    }

    public void Initialize(IAssetManagerService assetManagerService, GraphicsDevice graphicsDevice)
    {
        if (_isInitialized)
        {
            return;
        }

        _assetManagerService = assetManagerService;
        LoadFont();
        LoadTextures();



        RecalculateSize();

        _isInitialized = true;
    }

    private void SetDefaultColors()
    {
        NormalBackgroundColor = Color.White;
        HoverBackgroundColor = new Color(248, 248, 248);
        PressedBackgroundColor = new Color(230, 230, 230);
        DisabledBackgroundColor = new Color(245, 245, 245);
        SelectedBackgroundColor = Color.White;

        NormalBorderColor = new Color(118, 118, 118);
        HoverBorderColor = new Color(0, 120, 215);
        PressedBorderColor = new Color(0, 84, 153);
        DisabledBorderColor = new Color(204, 204, 204);
        SelectedBorderColor = new Color(0, 120, 215);

        NormalTextColor = Color.Black;
        HoverTextColor = Color.Black;
        PressedTextColor = Color.Black;
        DisabledTextColor = Color.Gray;

        DotColor = new Color(0, 120, 215);
        DisabledDotColor = new Color(204, 204, 204);
    }

    private void SetDefaultSize()
    {
        Size = new Vector2(RadioButtonSize + TextSpacing + 100, RadioButtonSize + 4);
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

    private void LoadTextures()
    {
        if (_assetManagerService == null)
        {
            return;
        }

        try
        {
            _radioUncheckedTexture = _assetManagerService.GetTexture("radio_unchecked");
            _radioCheckedTexture = _assetManagerService.GetTexture("radio_checked");
        }
        catch
        {
            _radioUncheckedTexture = null;
            _radioCheckedTexture = null;
        }
    }

    private void RecalculateSize()
    {
        if (_font == null)
        {
            return;
        }

        var textSize = _font.MeasureString(_text);
        Size = new Vector2(
            RadioButtonSize + TextSpacing + textSize.X + 8,
            Math.Max(RadioButtonSize, textSize.Y) + 4
        );
    }

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

        _isHovered = bounds.Contains(mousePosition);

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
                if (!_isSelected)
                {
                    IsSelected = true;
                }

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

    private Color GetBackgroundColor()
    {
        if (_isSelected && IsEnabled)
        {
            return GetCurrentState() switch
            {
                UIButtonState.Hovered => Color.Lerp(SelectedBackgroundColor, Color.Black, 0.05f),
                UIButtonState.Pressed => Color.Lerp(SelectedBackgroundColor, Color.Black, 0.1f),
                _ => SelectedBackgroundColor
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

    private Color GetBorderColor()
    {
        if (_isSelected && IsEnabled)
        {
            return SelectedBorderColor;
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

    protected override void OnRender(SpriteBatch spriteBatch)
    {
        if (_font == null)
        {
            return;
        }

        var position = GetAbsolutePosition();
        var radioButtonRect = new Rectangle(
            (int)position.X,
            (int)(position.Y + (Size.Y - RadioButtonSize) / 2),
            (int)RadioButtonSize,
            (int)RadioButtonSize
        );

        if (_radioUncheckedTexture != null && _radioCheckedTexture != null)
        {
            var texture = _isSelected ? _radioCheckedTexture : _radioUncheckedTexture;
            var tintColor = IsEnabled ? Color.White : Color.Gray;

            if (IsEnabled && _isHovered && !_isPressed)
            {
                tintColor = Color.Lerp(Color.White, Color.LightBlue, 0.2f);
            }
            else if (IsEnabled && _isPressed)
            {
                tintColor = Color.Lerp(Color.White, Color.Blue, 0.1f);
            }

            spriteBatch.Draw(texture, radioButtonRect, tintColor * Opacity);
        }
        else
        {
            var center = new Vector2(
                position.X + RadioButtonSize / 2,
                position.Y + Size.Y / 2
            );
            DrawRadioButtonFallback(spriteBatch, center);
        }

        if (!string.IsNullOrEmpty(_text))
        {
            var textPosition = new Vector2(
                position.X + RadioButtonSize + TextSpacing,
                position.Y + (Size.Y - _font.MeasureString(_text).Y) / 2
            );

            spriteBatch.DrawString(_font, _text, textPosition, GetTextColor() * Opacity);
        }
    }

    private void DrawCircle(SpriteBatch spriteBatch, Vector2 center, float radius, Color color)
    {

        var steps = Math.Max(16, (int)radius);
        for (var i = 0; i < steps; i++)
        {
            var angle = (float)(2 * Math.PI * i / steps);
            var x = center.X + (float)Math.Cos(angle) * (radius - 1);
            var y = center.Y + (float)Math.Sin(angle) * (radius - 1);

            var pixelRect = new Rectangle((int)x, (int)y, 2, 2);
            spriteBatch.Draw(SquidVoxEngineContext.WhitePixel, pixelRect, color);
        }

        var centerSize = Math.Max(1, (int)(radius * 0.7f));
        var centerRect = new Rectangle(
            (int)(center.X - centerSize / 2),
            (int)(center.Y - centerSize / 2),
            centerSize,
            centerSize
        );
        spriteBatch.Draw(SquidVoxEngineContext.WhitePixel, centerRect, color);
    }

    private void DrawCircleOutline(SpriteBatch spriteBatch, Vector2 center, float radius, float thickness, Color color)
    {

        var steps = Math.Max(16, (int)radius);
        for (var i = 0; i < steps; i++)
        {
            var angle = (float)(2 * Math.PI * i / steps);
            var x = center.X + (float)Math.Cos(angle) * radius;
            var y = center.Y + (float)Math.Sin(angle) * radius;

            var pixelRect = new Rectangle((int)x, (int)y, (int)thickness, (int)thickness);
            spriteBatch.Draw(SquidVoxEngineContext.WhitePixel, pixelRect, color);
        }
    }

    private void DrawRadioButtonFallback(SpriteBatch spriteBatch, Vector2 center)
    {

        DrawCircle(spriteBatch, center, RadioButtonSize / 2, GetBackgroundColor() * Opacity);

        if (BorderWidth > 0)
        {
            DrawCircleOutline(spriteBatch, center, RadioButtonSize / 2, BorderWidth, GetBorderColor() * Opacity);
        }

        if (_isSelected)
        {
            var dotColor = IsEnabled ? DotColor : DisabledDotColor;
            var dotRadius = RadioButtonSize * 0.25f;
            DrawCircle(spriteBatch, center, dotRadius, dotColor * Opacity);
        }
    }

    public void Select()
    {
        if (IsEnabled)
        {
            IsSelected = true;
        }
    }

    internal void SetGroup(RadioButtonGroup? group)
    {
        Group = group;
    }

    internal void SetSelectedSilent(bool isSelected)
    {
        _isSelected = isSelected;
    }

    /// <summary>
    /// Text displayed next to the radio button.
    /// </summary>
    public string Text
    {
        get => _text;
        set => _text = value ?? string.Empty;
    }

    public string Value
    {
        get => _value;
        set => _value = value ?? string.Empty;
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (value && !_isSelected)
            {
                if (Group != null)
                {
                    Group.SelectRadioButton(this);
                }
                else
                {
                    _isSelected = true;
                    SelectionChanged?.Invoke(this, true);
                }
            }
            else if (!value && _isSelected)
            {
                if (Group == null)
                {
                    _isSelected = false;
                    SelectionChanged?.Invoke(this, false);
                }
            }
        }
    }

    public RadioButtonGroup? Group { get; private set; }
    public float RadioButtonSize { get; set; } = 16f;
    public float TextSpacing { get; set; } = 8f;
    public float BorderWidth { get; set; } = 1f;
    public TextAlignment TextAlignment { get; set; } = TextAlignment.Left;
    public float Opacity { get; set; } = 1.0f;

    public Color NormalBackgroundColor { get; set; }
    public Color HoverBackgroundColor { get; set; }
    public Color PressedBackgroundColor { get; set; }
    public Color DisabledBackgroundColor { get; set; }
    public Color SelectedBackgroundColor { get; set; }

    public Color NormalBorderColor { get; set; }
    public Color HoverBorderColor { get; set; }
    public Color PressedBorderColor { get; set; }
    public Color DisabledBorderColor { get; set; }
    public Color SelectedBorderColor { get; set; }

    public Color NormalTextColor { get; set; }
    public Color HoverTextColor { get; set; }
    public Color PressedTextColor { get; set; }
    public Color DisabledTextColor { get; set; }

    public Color DotColor { get; set; }
    public Color DisabledDotColor { get; set; }
}
