using System.Numerics;
using DryIoc;
using FontStashSharp;
using FontStashSharp.Interfaces;
using Silk.NET.Maths;
using SquidVox.Core.GameObjects;
using SquidVox.Core.Interfaces.Services;
using SquidVox.World.Context;
using TrippyGL;

namespace SquidVox.World.GameObjects;

/// <summary>
/// A game object for rendering text in scenes.
/// </summary>
public class TextGameObject : Base2dGameObject
{
    private DynamicSpriteFont? _font;
    private string _text;
    private int _fontSize;
    private FSColor _color;

    /// <summary>
    /// Initializes a new instance of the TextGameObject class.
    /// </summary>
    /// <param name="text">The text to display.</param>
    /// <param name="fontName">The name of the font to use.</param>
    /// <param name="fontSize">The font size.</param>
    /// <param name="position">The position of the text.</param>
    /// <param name="color">The color of the text.</param>
    public TextGameObject(
        string text = "Text",
        string fontName = "Monocraft",
        int fontSize = 14,
        Vector2D<float>? position = null,
        FSColor? color = null)
    {
        _text = text ?? string.Empty;
        _fontSize = fontSize;
        FontName = fontName;
        Position = position ?? Vector2D<float>.Zero;
        _color = color ?? FSColor.White;

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
    public FSColor Color
    {
        get => _color;
        set => _color = value;
    }

    /// <summary>
    /// Renders the text.
    /// </summary>
    /// <param name="textureBatcher">TextureBatcher for rendering textures.</param>
    /// <param name="fontRenderer">Font renderer for drawing text.</param>
    protected override void OnRender(TextureBatcher textureBatcher, IFontStashRenderer fontRenderer)
    {
        if (_font == null || string.IsNullOrEmpty(_text))
        {
            return;
        }

        var absolutePosition = GetAbsolutePosition();
        var position = new System.Numerics.Vector2(absolutePosition.X, absolutePosition.Y);

        _font.DrawText(fontRenderer, _text, position, _color, scale: Vector2.One);
    }

    /// <summary>
    /// Centers the text horizontally in the viewport.
    /// </summary>
    /// <param name="viewportWidth">The width of the viewport.</param>
    public void CenterHorizontal(float viewportWidth)
    {
        if (_font == null)
        {
            return;
        }

        var textSize = Size;
        var centerX = (viewportWidth - textSize.X) / 2f;
        Position = new Vector2D<float>(centerX, Position.Y);
    }

    /// <summary>
    /// Centers the text vertically in the viewport.
    /// </summary>
    /// <param name="viewportHeight">The height of the viewport.</param>
    public void CenterVertical(float viewportHeight)
    {
        if (_font == null)
        {
            return;
        }

        var textSize = Size;
        var centerY = (viewportHeight - textSize.Y) / 2f;
        Position = new Vector2D<float>(Position.X, centerY);
    }

    /// <summary>
    /// Centers the text in the viewport.
    /// </summary>
    /// <param name="viewportWidth">The width of the viewport.</param>
    /// <param name="viewportHeight">The height of the viewport.</param>
    public void Center(float viewportWidth, float viewportHeight)
    {
        if (_font == null)
        {
            return;
        }

        var textSize = Size;
        var centerX = (viewportWidth - textSize.X) / 2f;
        var centerY = (viewportHeight - textSize.Y) / 2f;
        Position = new Vector2D<float>(centerX, centerY);
    }

    /// <summary>
    /// Loads the font from the asset manager.
    /// </summary>
    private void LoadFont()
    {
        var container = SquidVoxGraphicContext.Container;
        if (container != null)
        {
            var assetManager = container.Resolve<IAssetManagerService>();
            _font = assetManager.GetFont(FontName, FontSize);
        }
    }

    /// <summary>
    /// Updates the component size based on text dimensions.
    /// </summary>
    private void UpdateSize()
    {
        if (_font != null && !string.IsNullOrEmpty(_text))
        {
            var measuredSize = _font.MeasureString(_text);
            Size = new Vector2D<float>(measuredSize.X, measuredSize.Y);
        }
        else
        {
            Size = Vector2D<float>.Zero;
        }
    }
}
