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
/// Slider game object for selecting numeric values within a range
/// </summary>
public class SliderGameObject : Base2dGameObject
{
    private readonly string _fontName;
    private readonly int _fontSize;
    private IAssetManagerService? _assetManagerService;
    private DynamicSpriteFont? _font;
    private bool _isDragging;
    private bool _isHovered;
    private bool _isInitialized;

    private float _maxValue = 100f;
    private float _minValue;
    private MouseState _previousMouseState;
    private float _step = 1f;
    private Texture2D? _thumbTexture;
    private Texture2D? _trackTexture;
    private float _value = 50f;
    private Texture2D? _whitePixel;

    /// <summary>
    /// Initializes a new Slider game object
    /// </summary>
    public SliderGameObject(
        float minValue = 0f,
        float maxValue = 100f,
        float initialValue = 50f,
        float width = 200f,
        string fontName = "Monocraft",
        int fontSize = 12,
        IAssetManagerService? assetManagerService = null
    )
    {
        _minValue = minValue;
        _maxValue = maxValue;
        _value = Math.Clamp(initialValue, minValue, maxValue);
        _fontName = fontName;
        _fontSize = fontSize;
        _assetManagerService = assetManagerService;

        // Default styling
        SetDefaultColors();
        SetDefaultSize(width);
    }

    /// <summary>
    /// Initializes the slider resources
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

        // Create a 1x1 white pixel texture for drawing rectangles
        _whitePixel = new Texture2D(graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });

        // Recalculate size with loaded font
        RecalculateSize();

        _isInitialized = true;
    }

    private void SetDefaultColors()
    {
        // Track colors
        TrackBackgroundColor = new Color(200, 200, 200);
        TrackFillColor = new Color(0, 120, 215);
        DisabledTrackColor = new Color(230, 230, 230);

        // Thumb colors
        ThumbNormalColor = Color.White;
        ThumbHoverColor = new Color(248, 248, 248);
        ThumbPressedColor = new Color(240, 240, 240);
        ThumbDisabledColor = new Color(245, 245, 245);

        // Thumb border colors
        ThumbBorderColor = new Color(118, 118, 118);
        ThumbBorderHoverColor = new Color(0, 120, 215);
        ThumbBorderPressedColor = new Color(0, 84, 153);

        // Label colors
        ValueLabelColor = Color.Black;
        DisabledValueLabelColor = Color.Gray;
    }

    private void SetDefaultSize(float width)
    {
        if (Orientation == SliderOrientation.Horizontal)
        {
            var height = Math.Max(ThumbSize, TrackHeight);
            if (ShowValueLabel)
            {
                height += _fontSize + LabelSpacing;
            }

            Size = new Vector2(width, height);
        }
        else
        {
            var width2 = Math.Max(ThumbSize, TrackHeight);
            if (ShowValueLabel)
            {
                width2 += 50 + LabelSpacing; // Approximate label width
            }

            Size = new Vector2(width2, width); // Swap for vertical
        }
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
            _trackTexture = _assetManagerService.GetTexture("slider_track");
            _thumbTexture = _assetManagerService.GetTexture("slider_thumb");
        }
        catch
        {
            // If textures fail to load, we'll fall back to programmatic drawing
            _trackTexture = null;
            _thumbTexture = null;
        }
    }

    private void RecalculateSize()
    {
        if (Orientation == SliderOrientation.Horizontal)
        {
            var height = Math.Max(ThumbSize, TrackHeight);
            if (ShowValueLabel && _font != null)
            {
                height += _font.MeasureString("000.0").Y + LabelSpacing;
            }

            Size = new Vector2(Size.X, height);
        }
        else
        {
            var width = Math.Max(ThumbSize, TrackHeight);
            if (ShowValueLabel && _font != null)
            {
                width += _font.MeasureString("000.0").X + LabelSpacing;
            }

            Size = new Vector2(width, Size.Y);
        }
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        if (!IsEnabled)
        {
            _isHovered = false;
            _isDragging = false;
            return;
        }

        var mouseState = Mouse.GetState();
        var mousePosition = new Vector2(mouseState.X, mouseState.Y);

        // Check if mouse is over the slider
        var sliderBounds = GetSliderBounds();
        var thumbBounds = GetThumbBounds();

        _isHovered = sliderBounds.Contains(mousePosition) || thumbBounds.Contains(mousePosition);

        // Handle mouse press/release
        if (_isHovered && mouseState.LeftButton == ButtonState.Pressed &&
            _previousMouseState.LeftButton == ButtonState.Released)
        {
            _isDragging = true;
            HasFocus = true;
            UpdateValueFromMouse(mousePosition);
            DragStarted?.Invoke(this, EventArgs.Empty);
        }
        else if (_isDragging && mouseState.LeftButton == ButtonState.Released)
        {
            _isDragging = false;
            DragEnded?.Invoke(this, EventArgs.Empty);
        }

        // Handle dragging
        if (_isDragging && mouseState.LeftButton == ButtonState.Pressed)
        {
            UpdateValueFromMouse(mousePosition);
        }

        _previousMouseState = mouseState;
    }

    private void UpdateValueFromMouse(Vector2 mousePosition)
    {
        var sliderBounds = GetSliderBounds();
        float percentage;

        if (Orientation == SliderOrientation.Horizontal)
        {
            percentage = (mousePosition.X - sliderBounds.X) / sliderBounds.Width;
        }
        else
        {
            percentage = 1f - (mousePosition.Y - sliderBounds.Y) / sliderBounds.Height;
        }

        percentage = Math.Clamp(percentage, 0f, 1f);
        Value = _minValue + percentage * (_maxValue - _minValue);
    }

    private Rectangle GetSliderBounds()
    {
        var position = GetAbsolutePosition();

        if (Orientation == SliderOrientation.Horizontal)
        {
            var trackY = position.Y + (Size.Y - TrackHeight) / 2;
            if (ShowValueLabel)
            {
                trackY -= (_fontSize + LabelSpacing) / 2;
            }

            return new Rectangle(
                (int)(position.X + ThumbSize / 2),
                (int)trackY,
                (int)(Size.X - ThumbSize),
                (int)TrackHeight
            );
        }

        var trackX = position.X + (Size.X - TrackHeight) / 2;
        if (ShowValueLabel && _font != null)
        {
            trackX -= (_font.MeasureString("000.0").X + LabelSpacing) / 2;
        }

        return new Rectangle(
            (int)trackX,
            (int)(position.Y + ThumbSize / 2),
            (int)TrackHeight,
            (int)(Size.Y - ThumbSize)
        );
    }

    private Rectangle GetThumbBounds()
    {
        var sliderBounds = GetSliderBounds();
        var percentage = (_value - _minValue) / (_maxValue - _minValue);

        if (Orientation == SliderOrientation.Horizontal)
        {
            var thumbX = sliderBounds.X + percentage * sliderBounds.Width - ThumbSize / 2;
            var thumbY = sliderBounds.Y + sliderBounds.Height / 2 - ThumbSize / 2;
            return new Rectangle((int)thumbX, (int)thumbY, (int)ThumbSize, (int)ThumbSize);
        }
        else
        {
            var thumbX = sliderBounds.X + sliderBounds.Width / 2 - ThumbSize / 2;
            var thumbY = sliderBounds.Y + (1f - percentage) * sliderBounds.Height - ThumbSize / 2;
            return new Rectangle((int)thumbX, (int)thumbY, (int)ThumbSize, (int)ThumbSize);
        }
    }

    private UIButtonState GetThumbState()
    {
        if (!IsEnabled)
        {
            return UIButtonState.Disabled;
        }

        if (_isDragging)
        {
            return UIButtonState.Pressed;
        }

        if (_isHovered)
        {
            return UIButtonState.Hovered;
        }

        return UIButtonState.Normal;
    }

    private Color GetThumbColor()
    {
        return GetThumbState() switch
        {
            UIButtonState.Normal => ThumbNormalColor,
            UIButtonState.Hovered => ThumbHoverColor,
            UIButtonState.Pressed => ThumbPressedColor,
            UIButtonState.Disabled => ThumbDisabledColor,
            _ => ThumbNormalColor
        };
    }

    private Color GetThumbBorderColor()
    {
        return GetThumbState() switch
        {
            UIButtonState.Normal => ThumbBorderColor,
            UIButtonState.Hovered => ThumbBorderHoverColor,
            UIButtonState.Pressed => ThumbBorderPressedColor,
            UIButtonState.Disabled => ThumbBorderColor,
            _ => ThumbBorderColor
        };
    }

    protected override void OnRender(SpriteBatch spriteBatch)
    {
        if (_whitePixel == null)
        {
            return;
        }

        DrawTrack(spriteBatch);
        DrawThumb(spriteBatch);

        if (ShowValueLabel)
        {
            DrawValueLabel(spriteBatch);
        }
    }

    private void DrawTrack(SpriteBatch spriteBatch)
    {
        if (_whitePixel == null)
        {
            return;
        }

        var trackBounds = GetSliderBounds();
        var trackColor = IsEnabled ? TrackBackgroundColor : DisabledTrackColor;

        // Draw background track
        spriteBatch.Draw(_whitePixel, trackBounds, trackColor * Opacity);

        // Draw filled portion
        if (IsEnabled && _value > _minValue)
        {
            var percentage = (_value - _minValue) / (_maxValue - _minValue);
            Rectangle fillBounds;

            if (Orientation == SliderOrientation.Horizontal)
            {
                fillBounds = new Rectangle(
                    trackBounds.X,
                    trackBounds.Y,
                    (int)(trackBounds.Width * percentage),
                    trackBounds.Height
                );
            }
            else
            {
                var fillHeight = (int)(trackBounds.Height * percentage);
                fillBounds = new Rectangle(
                    trackBounds.X,
                    trackBounds.Bottom - fillHeight,
                    trackBounds.Width,
                    fillHeight
                );
            }

            spriteBatch.Draw(_whitePixel, fillBounds, TrackFillColor * Opacity);
        }
    }

    private void DrawThumb(SpriteBatch spriteBatch)
    {
        var thumbBounds = GetThumbBounds();

        // Draw thumb using texture if available, otherwise fall back to programmatic drawing
        if (_thumbTexture != null)
        {
            var thumbColor = IsEnabled ? Color.White : Color.Gray;

            // Apply hover/pressed effects with slight tinting
            if (IsEnabled && _isDragging)
            {
                thumbColor = Color.Lerp(Color.White, Color.Blue, 0.3f);
            }
            else if (IsEnabled && _isHovered)
            {
                thumbColor = Color.Lerp(Color.White, Color.LightBlue, 0.2f);
            }

            spriteBatch.Draw(_thumbTexture, thumbBounds, thumbColor * Opacity);
        }
        else
        {
            // Fallback to programmatic drawing
            DrawThumbFallback(spriteBatch, thumbBounds);
        }
    }

    private void DrawThumbFallback(SpriteBatch spriteBatch, Rectangle thumbBounds)
    {
        if (_whitePixel == null)
        {
            return;
        }

        // Draw thumb background
        spriteBatch.Draw(_whitePixel, thumbBounds, GetThumbColor() * Opacity);

        // Draw thumb border
        var borderColor = GetThumbBorderColor() * Opacity;
        var borderWidth = 2;

        // Top border
        spriteBatch.Draw(
            _whitePixel,
            new Rectangle(thumbBounds.X, thumbBounds.Y, thumbBounds.Width, borderWidth),
            borderColor
        );
        // Bottom border
        spriteBatch.Draw(
            _whitePixel,
            new Rectangle(thumbBounds.X, thumbBounds.Bottom - borderWidth, thumbBounds.Width, borderWidth),
            borderColor
        );
        // Left border
        spriteBatch.Draw(
            _whitePixel,
            new Rectangle(thumbBounds.X, thumbBounds.Y, borderWidth, thumbBounds.Height),
            borderColor
        );
        // Right border
        spriteBatch.Draw(
            _whitePixel,
            new Rectangle(thumbBounds.Right - borderWidth, thumbBounds.Y, borderWidth, thumbBounds.Height),
            borderColor
        );
    }

    private void DrawValueLabel(SpriteBatch spriteBatch)
    {
        if (_font == null)
        {
            return;
        }

        var valueText = _value.ToString(ValueFormat);
        var textColor = IsEnabled ? ValueLabelColor : DisabledValueLabelColor;
        var sliderBounds = GetSliderBounds();

        Vector2 labelPosition;

        if (Orientation == SliderOrientation.Horizontal)
        {
            // Position label below the slider
            labelPosition = new Vector2(
                sliderBounds.X + sliderBounds.Width / 2 - _font.MeasureString(valueText).X / 2,
                sliderBounds.Bottom + LabelSpacing
            );
        }
        else
        {
            // Position label to the right of the slider
            labelPosition = new Vector2(
                sliderBounds.Right + LabelSpacing,
                sliderBounds.Y + sliderBounds.Height / 2 - _font.MeasureString(valueText).Y / 2
            );
        }

        spriteBatch.DrawString(_font, valueText, labelPosition, textColor * Opacity);
    }

    public void SetValueSilent(float value)
    {
        _value = Math.Clamp(value, _minValue, _maxValue);

        // Apply step if specified
        if (_step > 0)
        {
            _value = (float)(Math.Round((_value - _minValue) / _step) * _step + _minValue);
            _value = Math.Clamp(_value, _minValue, _maxValue);
        }
    }

    public float GetPercentage()
    {
        return (_value - _minValue) / (_maxValue - _minValue);
    }

    public void SetFromPercentage(float percentage)
    {
        percentage = Math.Clamp(percentage, 0f, 1f);
        Value = _minValue + percentage * (_maxValue - _minValue);
    }

    #region Properties

    public float MinValue
    {
        get => _minValue;
        set
        {
            _minValue = value;
            if (_maxValue < _minValue)
            {
                _maxValue = _minValue;
            }

            Value = Math.Clamp(_value, _minValue, _maxValue);
        }
    }

    public float MaxValue
    {
        get => _maxValue;
        set
        {
            _maxValue = value;
            if (_minValue > _maxValue)
            {
                _minValue = _maxValue;
            }

            Value = Math.Clamp(_value, _minValue, _maxValue);
        }
    }

    public float Value
    {
        get => _value;
        set
        {
            var newValue = Math.Clamp(value, _minValue, _maxValue);

            // Apply step if specified
            if (_step > 0)
            {
                newValue = (float)(Math.Round((newValue - _minValue) / _step) * _step + _minValue);
                newValue = Math.Clamp(newValue, _minValue, _maxValue);
            }

            if (Math.Abs(_value - newValue) > float.Epsilon)
            {
                var oldValue = _value;
                _value = newValue;
                ValueChanged?.Invoke(this, new SliderValueChangedEventArgs(oldValue, newValue));
            }
        }
    }

    public float Step
    {
        get => _step;
        set => _step = Math.Max(0f, value);
    }

    public bool ShowValueLabel { get; set; } = true;
    public SliderOrientation Orientation { get; set; } = SliderOrientation.Horizontal;
    public float TrackHeight { get; set; } = 8f;
    public float ThumbSize { get; set; } = 20f;
    public float LabelSpacing { get; set; } = 8f;
    public string ValueFormat { get; set; } = "F1";
    public float Opacity { get; set; } = 1.0f;

    // Color properties
    public Color TrackBackgroundColor { get; set; }
    public Color TrackFillColor { get; set; }
    public Color DisabledTrackColor { get; set; }
    public Color ThumbNormalColor { get; set; }
    public Color ThumbHoverColor { get; set; }
    public Color ThumbPressedColor { get; set; }
    public Color ThumbDisabledColor { get; set; }
    public Color ThumbBorderColor { get; set; }
    public Color ThumbBorderHoverColor { get; set; }
    public Color ThumbBorderPressedColor { get; set; }
    public Color ValueLabelColor { get; set; }
    public Color DisabledValueLabelColor { get; set; }

    public event EventHandler<SliderValueChangedEventArgs>? ValueChanged;
    public event EventHandler? DragStarted;
    public event EventHandler? DragEnded;

    #endregion
}
