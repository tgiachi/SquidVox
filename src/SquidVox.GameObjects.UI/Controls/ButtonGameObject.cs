using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Graphics;
using SquidVox.Core.Context;
using SquidVox.Core.GameObjects;
using SquidVox.Core.Interfaces.Services;
using SquidVox.GameObjects.UI.Types;

namespace SquidVox.GameObjects.UI.Controls;

/// <summary>
/// Button game object with hover effects, click handling, and customizable styling
/// </summary>
public class ButtonGameObject : Base2dGameObject
{
    private readonly string _fontName;
    private readonly int _fontSize;
    private IAssetManagerService? _assetManagerService;
    private DynamicSpriteFont? _font;
    private bool _isInitialized;

    private MouseState _previousMouseState;
    private string _text;


    /// <summary>
    /// Initializes a new Button game object
    /// </summary>
    /// <param name="text">Button text</param>
    /// <param name="width">Width of the button</param>
    /// <param name="height">Height of the button</param>
    /// <param name="fontName">Font name for text display</param>
    /// <param name="fontSize">Font size for text display</param>
    /// <param name="assetManagerService">Asset manager service for loading resources</param>
    public ButtonGameObject(
        string text = "Button",
        float width = 120f,
        float height = 35f,
        string fontName = "Monocraft",
        int fontSize = 14,
        IAssetManagerService? assetManagerService = null
    )
    {
        _text = text;
        Size = new Vector2(width, height);
        _fontName = fontName;
        _fontSize = fontSize;
        _assetManagerService = assetManagerService;

        // Default styling
        SetDefaultColors();
    }

    /// <summary>
    /// Sets default color scheme
    /// </summary>
    private void SetDefaultColors()
    {
        // Default button colors (similar to Windows/web buttons)
        NormalBackgroundColor = new Color(240, 240, 240);
        HoverBackgroundColor = new Color(229, 241, 251);
        PressedBackgroundColor = new Color(204, 228, 247);
        DisabledBackgroundColor = new Color(245, 245, 245);

        NormalTextColor = Color.Black;
        HoverTextColor = Color.Black;
        PressedTextColor = Color.Black;
        DisabledTextColor = Color.Gray;

        NormalBorderColor = new Color(173, 173, 173);
        HoverBorderColor = new Color(0, 120, 215);
        PressedBorderColor = new Color(0, 84, 153);
        DisabledBorderColor = new Color(204, 204, 204);
    }

    /// <summary>
    /// Sets a flat button style
    /// </summary>
    public void SetFlatStyle(Color backgroundColor, Color textColor, Color? hoverColor = null)
    {
        var hover = hoverColor ?? Color.Lerp(backgroundColor, Color.White, 0.1f);
        var pressed = Color.Lerp(backgroundColor, Color.Black, 0.1f);
        var disabled = Color.Lerp(backgroundColor, Color.Gray, 0.5f);

        NormalBackgroundColor = backgroundColor;
        HoverBackgroundColor = hover;
        PressedBackgroundColor = pressed;
        DisabledBackgroundColor = disabled;

        NormalTextColor = textColor;
        HoverTextColor = textColor;
        PressedTextColor = textColor;
        DisabledTextColor = Color.Lerp(textColor, Color.Gray, 0.5f);

        // No borders for flat style
        BorderWidth = 0;
        NormalBorderColor = Color.Transparent;
        HoverBorderColor = Color.Transparent;
        PressedBorderColor = Color.Transparent;
        DisabledBorderColor = Color.Transparent;
    }

    /// <summary>
    /// Sets an outlined button style
    /// </summary>
    public void SetOutlinedStyle(Color borderColor, Color textColor, Color? hoverBackgroundColor = null)
    {
        var hoverBg = hoverBackgroundColor ?? Color.Lerp(borderColor, Color.White, 0.9f);
        var pressedBg = Color.Lerp(borderColor, Color.White, 0.8f);

        NormalBackgroundColor = Color.Transparent;
        HoverBackgroundColor = hoverBg;
        PressedBackgroundColor = pressedBg;
        DisabledBackgroundColor = Color.Transparent;

        NormalTextColor = textColor;
        HoverTextColor = textColor;
        PressedTextColor = textColor;
        DisabledTextColor = Color.Lerp(textColor, Color.Gray, 0.5f);

        BorderWidth = 2;
        NormalBorderColor = borderColor;
        HoverBorderColor = borderColor;
        PressedBorderColor = borderColor;
        DisabledBorderColor = Color.Lerp(borderColor, Color.Gray, 0.5f);
    }

    /// <summary>
    /// Initializes the button resources
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
    /// Updates the button state
    /// </summary>
    protected override void OnUpdate(GameTime gameTime)
    {
        if (!IsEnabled)
        {
            return;
        }

        HandleMouseInput();
    }

    /// <summary>
    /// Handles mouse input for button interaction
    /// </summary>
    private void HandleMouseInput()
    {
        var currentMouseState = Mouse.GetState();
        var mousePosition = new Vector2(currentMouseState.X, currentMouseState.Y);
        var absolutePos = GetAbsolutePosition();
        var bounds = new Rectangle((int)absolutePos.X, (int)absolutePos.Y, (int)Size.X, (int)Size.Y);
        var isMouseOver = bounds.Contains(mousePosition);

        var previousMouseOver = GetPreviousMouseOver();

        // Mouse enter/leave events
        if (isMouseOver && !previousMouseOver)
        {
            MouseEnter?.Invoke(this, EventArgs.Empty);
        }
        else if (!isMouseOver && previousMouseOver)
        {
            MouseLeave?.Invoke(this, EventArgs.Empty);
        }

        // Update button state based on mouse interaction
        if (!IsEnabled)
        {
            CurrentState = UIButtonState.Disabled;
        }
        else if (currentMouseState.LeftButton == ButtonState.Pressed && isMouseOver)
        {
            if (CurrentState != UIButtonState.Pressed)
            {
                CurrentState = UIButtonState.Pressed;
                IsPressed = true;
                MouseDown?.Invoke(this, EventArgs.Empty);
            }
        }
        else if (_previousMouseState.LeftButton == ButtonState.Pressed &&
                 currentMouseState.LeftButton == ButtonState.Released)
        {
            if (IsPressed)
            {
                IsPressed = false;
                MouseUp?.Invoke(this, EventArgs.Empty);

                // Fire click event if mouse is released over the button
                if (isMouseOver)
                {
                    Click?.Invoke(this, EventArgs.Empty);
                }
            }

            CurrentState = isMouseOver ? UIButtonState.Hovered : UIButtonState.Normal;
        }
        else if (isMouseOver)
        {
            CurrentState = UIButtonState.Hovered;
        }
        else
        {
            CurrentState = UIButtonState.Normal;
        }

        _previousMouseState = currentMouseState;
    }

    /// <summary>
    /// Determines if mouse was over the button in the previous frame
    /// </summary>
    private bool GetPreviousMouseOver()
    {
        var previousMousePosition = new Vector2(_previousMouseState.X, _previousMouseState.Y);
        var absolutePos = GetAbsolutePosition();
        var bounds = new Rectangle((int)absolutePos.X, (int)absolutePos.Y, (int)Size.X, (int)Size.Y);
        return bounds.Contains(previousMousePosition);
    }

    /// <summary>
    /// Gets the current background color based on button state
    /// </summary>
    private Color GetCurrentBackgroundColor()
    {
        return CurrentState switch
        {
            UIButtonState.Hovered => HoverBackgroundColor,
            UIButtonState.Pressed => PressedBackgroundColor,
            UIButtonState.Disabled => DisabledBackgroundColor,
            _ => NormalBackgroundColor
        };
    }

    /// <summary>
    /// Gets the current text color based on button state
    /// </summary>
    private Color GetCurrentTextColor()
    {
        return CurrentState switch
        {
            UIButtonState.Hovered => HoverTextColor,
            UIButtonState.Pressed => PressedTextColor,
            UIButtonState.Disabled => DisabledTextColor,
            _ => NormalTextColor
        };
    }

    /// <summary>
    /// Gets the current border color based on button state
    /// </summary>
    private Color GetCurrentBorderColor()
    {
        return CurrentState switch
        {
            UIButtonState.Hovered => HoverBorderColor,
            UIButtonState.Pressed => PressedBorderColor,
            UIButtonState.Disabled => DisabledBorderColor,
            _ => NormalBorderColor
        };
    }

    /// <summary>
    /// Renders the button
    /// </summary>
    protected override void OnRender(SpriteBatch spriteBatch)
    {
        if (!IsVisible || SquidVoxGraphicContext.WhitePixel == null)
        {
            return;
        }

        var absolutePos = GetAbsolutePosition();
        var bounds = new Rectangle((int)absolutePos.X, (int)absolutePos.Y, (int)Size.X, (int)Size.Y);

        // Draw background
        DrawBackground(spriteBatch, bounds);

        // Draw border
        if (BorderWidth > 0)
        {
            DrawBorder(spriteBatch, bounds);
        }

        // Draw text
        DrawText(spriteBatch, bounds);
    }

    /// <summary>
    /// Draws the button background
    /// </summary>
    private void DrawBackground(SpriteBatch spriteBatch, Rectangle bounds)
    {
        var backgroundColor = GetCurrentBackgroundColor();

        if (CornerRadius > 0)
        {
            // For rounded corners, we'd need a more complex drawing method
            // For now, draw as rectangle - rounded corners would be a future enhancement
        }

        spriteBatch.Draw(SquidVoxGraphicContext.WhitePixel, bounds, backgroundColor * Opacity);
    }

    /// <summary>
    /// Draws the button border
    /// </summary>
    private void DrawBorder(SpriteBatch spriteBatch, Rectangle bounds)
    {
        var borderColor = GetCurrentBorderColor() * Opacity;

        // Top border
        spriteBatch.Draw(SquidVoxGraphicContext.WhitePixel, new Rectangle(bounds.X, bounds.Y, bounds.Width, BorderWidth), borderColor);
        // Bottom border
        spriteBatch.Draw(
            _whitePixel,
            new Rectangle(bounds.X, bounds.Bottom - BorderWidth, bounds.Width, BorderWidth),
            borderColor
        );
        // Left border
        spriteBatch.Draw(SquidVoxGraphicContext.WhitePixel, new Rectangle(bounds.X, bounds.Y, BorderWidth, bounds.Height), borderColor);
        // Right border
        spriteBatch.Draw(
            _whitePixel,
            new Rectangle(bounds.Right - BorderWidth, bounds.Y, BorderWidth, bounds.Height),
            borderColor
        );
    }

    /// <summary>
    /// Draws the button text
    /// </summary>
    private void DrawText(SpriteBatch spriteBatch, Rectangle bounds)
    {
        if (_font == null || string.IsNullOrEmpty(_text))
        {
            return;
        }

        var textColor = GetCurrentTextColor() * Opacity;
        var textSize = _font.MeasureString(_text);

        // Calculate text position based on alignment
        var textPosition = TextAlignment switch
        {
            TextAlignment.Left => new Vector2(
                bounds.X + TextPadding.X,
                bounds.Y + (bounds.Height - textSize.Y) / 2f
            ),
            TextAlignment.Right => new Vector2(
                bounds.Right - TextPadding.X - textSize.X,
                bounds.Y + (bounds.Height - textSize.Y) / 2f
            ),
            TextAlignment.Center or _ => new Vector2(
                bounds.X + (bounds.Width - textSize.X) / 2f,
                bounds.Y + (bounds.Height - textSize.Y) / 2f
            )
        };

        spriteBatch.DrawString(_font, _text, textPosition, textColor);
    }

    /// <summary>
    /// Programmatically triggers a click event
    /// </summary>
    public void PerformClick()
    {
        if (IsEnabled)
        {
            Click?.Invoke(this, EventArgs.Empty);
        }
    }

    #region Properties

    /// <summary>
    /// Text displayed on the button
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value ?? string.Empty;
                TextChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Current visual state of the button
    /// </summary>
    public UIButtonState CurrentState { get; private set; } = UIButtonState.Normal;

    /// <summary>
    /// Whether the button is currently being pressed
    /// </summary>
    public bool IsPressed { get; private set; }

    /// <summary>
    /// Opacity of the button (0.0 to 1.0)
    /// </summary>
    public float Opacity { get; set; } = 1.0f;

    #region Colors

    /// <summary>
    /// Background color in normal state
    /// </summary>
    public Color NormalBackgroundColor { get; set; }

    /// <summary>
    /// Background color when hovered
    /// </summary>
    public Color HoverBackgroundColor { get; set; }

    /// <summary>
    /// Background color when pressed
    /// </summary>
    public Color PressedBackgroundColor { get; set; }

    /// <summary>
    /// Background color when disabled
    /// </summary>
    public Color DisabledBackgroundColor { get; set; }

    /// <summary>
    /// Text color in normal state
    /// </summary>
    public Color NormalTextColor { get; set; }

    /// <summary>
    /// Text color when hovered
    /// </summary>
    public Color HoverTextColor { get; set; }

    /// <summary>
    /// Text color when pressed
    /// </summary>
    public Color PressedTextColor { get; set; }

    /// <summary>
    /// Text color when disabled
    /// </summary>
    public Color DisabledTextColor { get; set; }

    /// <summary>
    /// Border color in normal state
    /// </summary>
    public Color NormalBorderColor { get; set; }

    /// <summary>
    /// Border color when hovered
    /// </summary>
    public Color HoverBorderColor { get; set; }

    /// <summary>
    /// Border color when pressed
    /// </summary>
    public Color PressedBorderColor { get; set; }

    /// <summary>
    /// Border color when disabled
    /// </summary>
    public Color DisabledBorderColor { get; set; }

    #endregion

    /// <summary>
    /// Border width in pixels
    /// </summary>
    public int BorderWidth { get; set; } = 1;

    /// <summary>
    /// Corner radius for rounded buttons (0 for square)
    /// </summary>
    public int CornerRadius { get; set; }

    /// <summary>
    /// Padding around the text content
    /// </summary>
    public Vector2 TextPadding { get; set; } = new(8, 4);

    /// <summary>
    /// Text alignment within the button
    /// </summary>
    public TextAlignment TextAlignment { get; set; } = TextAlignment.Center;

    /// <summary>
    /// Event fired when the button is clicked
    /// </summary>
    public event EventHandler? Click;

    /// <summary>
    /// Event fired when mouse enters the button area
    /// </summary>
    public event EventHandler? MouseEnter;

    /// <summary>
    /// Event fired when mouse leaves the button area
    /// </summary>
    public event EventHandler? MouseLeave;

    /// <summary>
    /// Event fired when mouse is pressed down on the button
    /// </summary>
    public event EventHandler? MouseDown;

    /// <summary>
    /// Event fired when mouse is released on the button
    /// </summary>
    public event EventHandler? MouseUp;

    /// <summary>
    /// Event fired when button text changes
    /// </summary>
    public event EventHandler? TextChanged;

    #endregion
}
