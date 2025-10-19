using DryIoc;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SquidVox.Core.GameObjects;
using SquidVox.Core.Interfaces.Services;
using SquidVox.World3d.Context;

namespace SquidVox.World3d.GameObjects;

/// <summary>
/// A simple component for rendering text in scenes.
/// </summary>
public sealed class TextGameObject : Base2dGameObject
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
    public TextGameObject(
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
    /// </summary>
    public float Opacity { get; set; } = 1.0f;

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
        var drawColor = Color * Opacity;

        spriteBatch.DrawString(_font, _text, absolutePosition, drawColor);
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
        var viewportWidth = SquidVoxGraphicContext.GraphicsDevice?.Viewport.Width ?? 800;
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
        var viewportHeight = SquidVoxGraphicContext.GraphicsDevice?.Viewport.Height ?? 600;
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
        var assetManager = SquidVoxGraphicContext.Container.Resolve<IAssetManagerService>();
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
