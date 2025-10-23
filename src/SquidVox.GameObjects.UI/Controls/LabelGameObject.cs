using DryIoc;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SquidVox.Core.Context;
using SquidVox.Core.GameObjects;
using SquidVox.Core.Interfaces.Services;
using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace SquidVox.GameObjects.UI.Controls;

/// <summary>
/// A simple component for rendering text in scenes.
/// </summary>
public class LabelGameObject : Base2dGameObject
{
    private DynamicSpriteFont? _font;
    private string _text;
    private int _fontSize;

    /// <summary>
    /// Initializes a new instance of the TextGameObject class.
    /// </summary>
    /// <param name="text">The text to display.</param>
    /// <param name="fontName">The name of the font to use.</param>
    /// <param name="fontSize">The font size.</param>
    /// <param name="position">The relative position of the text.</param>
    /// <param name="color">The color of the text.</param>
    public LabelGameObject(
        string text = "Text",
        string fontName = "Monocraft",
        int fontSize = 14,
        Vector2? position = null,
        Color? color = null
    )
    {
        _text = text ?? string.Empty;
        _fontSize = fontSize;
        FontName = fontName;
        Position = position ?? Vector2.Zero;
        Color = color ?? Color.White;

        LoadFont();
        UpdateSize();
    }

    /// <summary>
    /// Gets or sets the text to display.
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value ?? string.Empty;
                UpdateSize();
            }
        }
    }

    /// <summary>
    /// Gets the font name.
    /// </summary>
    public string FontName { get; }

    /// <summary>
    /// Gets or sets the font size.
    /// </summary>
    public int FontSize
    {
        get => _fontSize;
        set
        {
            if (_fontSize != value)
            {
                _fontSize = value;
                LoadFont();
                UpdateSize();
            }
        }
    }

    /// <summary>
    /// Gets or sets the color of the text.
    /// </summary>
    public Color Color { get; set; }

    /// <summary>
    /// Gets or sets the opacity of the text (0.0 to 1.0).
    /// Setting to 0.0 will also set IsVisible to false for optimization.
    /// </summary>
    public float Opacity
    {
        get => _opacity;
        set
        {
            _opacity = MathHelper.Clamp(value, 0.0f, 1.0f);
            if (_opacity == 0.0f)
            {
                IsVisible = false;
            }
        }
    }
    private float _opacity = 1.0f;



    /// <summary>
    /// Draws the text content.
    /// </summary>
    /// <param name="spriteBatch">The sprite batch to use for drawing.</param>
    protected override void OnRender(SpriteBatch spriteBatch)
    {
        if (_font == null || string.IsNullOrEmpty(_text))
        {
            return;
        }

        var absolutePosition = GetAbsolutePosition();
        var drawColor = Color * _opacity;

        // Use rotation and scale from base class
        var origin = Vector2.Zero; // Top-left origin
        FontStashSharp.SpriteBatchExtensions.DrawString(spriteBatch, _font, _text, absolutePosition, drawColor, Rotation, origin, Scale, 0f);
    }

    /// <summary>
    /// Centers the text horizontally on the screen.
    /// </summary>
    public void CenterHorizontal()
    {
        if (_font == null)
        {
            return;
        }

        var textSize = Size;
        var viewportWidth = SquidVoxEngineContext.GraphicsDevice?.Viewport.Width ?? 800;
        var centerX = (viewportWidth - textSize.X) / 2f;
        Position = new Vector2(centerX, Position.Y);
    }

    /// <summary>
    /// Centers the text vertically on the screen.
    /// </summary>
    public void CenterVertical()
    {
        if (_font == null)
        {
            return;
        }

        var textSize = Size;
        var viewportHeight = SquidVoxEngineContext.GraphicsDevice?.Viewport.Height ?? 600;
        var centerY = (viewportHeight - textSize.Y) / 2f;
        Position = new Vector2(Position.X, centerY);
    }

    /// <summary>
    /// Centers the text on the screen.
    /// </summary>
    public void Center()
    {
        CenterHorizontal();
        CenterVertical();
    }

    /// <summary>
    /// Loads the font.
    /// </summary>
    private void LoadFont()
    {
        var assetManager = SquidVoxEngineContext.Container.Resolve<IAssetManagerService>();
        _font = assetManager.GetFont(FontName, FontSize);
    }

    /// <summary>
    /// Updates the component size based on text dimensions.
    /// </summary>
    private void UpdateSize()
    {
        if (_font != null && !string.IsNullOrEmpty(_text))
        {
            Size = _font.MeasureString(_text);
        }
        else
        {
            Size = Vector2.Zero;
        }
    }
}
