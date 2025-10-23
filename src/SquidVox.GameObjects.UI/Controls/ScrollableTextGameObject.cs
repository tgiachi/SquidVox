using System;
using System.Collections.Generic;
using DryIoc;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SquidVox.Core.Context;
using SquidVox.Core.GameObjects;
using SquidVox.Core.Interfaces.Services;

namespace SquidVox.GameObjects.UI.Controls;

/// <summary>
/// A scrollable text panel that displays long text content with mouse wheel scrolling support.
/// </summary>
public class ScrollableTextGameObject : Base2dGameObject
{
    private DynamicSpriteFont? _font;
    private string _text = string.Empty;
    private Color _textColor = Color.White;
    private readonly Color _scrollbarTrackColor = new Color(50, 50, 50);
    private readonly Color _scrollbarThumbColor = new Color(120, 120, 120);

    // Scrolling state
    private float _scrollOffset;
    private float _maxScrollOffset;
    private MouseState _previousMouseState;
    private readonly List<string> _lines = new();
    private float _lineHeight;

    // Configuration
    private int _padding = 10;
    private const int _scrollbarWidth = 8;
    private const int _scrollbarPadding = 2;

    /// <summary>
    /// Gets or sets the text content to display.
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value ?? string.Empty;
                UpdateLines();
            }
        }
    }

    /// <summary>
    /// Gets or sets the text color.
    /// </summary>
    public Color TextColor
    {
        get => _textColor;
        set => _textColor = value;
    }

    /// <summary>
    /// Gets or sets the background color.
    /// </summary>
    public Color BackgroundColor { get; set; } = new Color(30, 30, 30);

    /// <summary>
    /// Gets or sets the border color.
    /// </summary>
    public Color BorderColor { get; set; } = new Color(60, 60, 60);

    /// <summary>
    /// Gets or sets the padding inside the panel.
    /// </summary>
    public int Padding
    {
        get => _padding;
        set
        {
            if (_padding != value)
            {
                _padding = value;
                UpdateLines();
            }
        }
    }

    /// <summary>
    /// Gets or sets the scroll speed (pixels per mouse wheel notch).
    /// </summary>
    public float ScrollSpeed { get; set; } = 20f;

    /// <summary>
    /// Initializes a new instance of the ScrollableTextGameObject class.
    /// </summary>
    /// <param name="width">Width of the panel.</param>
    /// <param name="height">Height of the panel.</param>
    /// <param name="fontName">Name of the font to use.</param>
    /// <param name="fontSize">Size of the font.</param>
    public ScrollableTextGameObject(float width, float height, string fontName = "DefaultMono", int fontSize = 12)
    {
        Size = new Vector2(width, height);
        Name = "ScrollableTextPanel";
    }

    /// <summary>
    /// Initializes the scrollable text panel.
    /// </summary>
    /// <param name="assetManagerService">Asset manager service for loading fonts.</param>
    /// <param name="fontName">Name of the font to use.</param>
    /// <param name="fontSize">Size of the font.</param>
    public void Initialize(string fontName = "DefaultMono", int fontSize = 12)
    {
        try
        {
            _font = SquidVoxEngineContext.Container.Resolve<IAssetManagerService>().GetFont(fontName, fontSize);
            if (_font != null)
            {
                _lineHeight = _font.MeasureString("Ay").Y;
                UpdateLines();
            }
        }
        catch
        {
            // Font loading failed, will be handled in rendering
        }
    }

    /// <summary>
    /// Updates the panel and handles scrolling input.
    /// </summary>
    protected override void OnUpdate(GameTime gameTime)
    {
        base.OnUpdate(gameTime);

        if (!IsEnabled || _font == null)
        {
            return;
        }

        var currentMouseState = Mouse.GetState();
        var absolutePos = GetAbsolutePosition();
        var bounds = new Rectangle((int)absolutePos.X, (int)absolutePos.Y, (int)Size.X, (int)Size.Y);

        // Handle mouse wheel scrolling
        if (bounds.Contains(currentMouseState.Position))
        {
            var scrollDelta = currentMouseState.ScrollWheelValue - _previousMouseState.ScrollWheelValue;
            if (scrollDelta != 0)
            {
                _scrollOffset -= scrollDelta / 120f * ScrollSpeed;
                _scrollOffset = Math.Clamp(_scrollOffset, 0f, _maxScrollOffset);
            }
        }

        _previousMouseState = currentMouseState;
    }

    /// <summary>
    /// Renders the scrollable text panel.
    /// </summary>
    protected override void OnRender(SpriteBatch spriteBatch)
    {
        if (!IsVisible || _font == null)
        {
            return;
        }

        var absolutePos = GetAbsolutePosition();
        var bounds = new Rectangle((int)absolutePos.X, (int)absolutePos.Y, (int)Size.X, (int)Size.Y);

        // Draw background
        spriteBatch.Draw(SquidVoxEngineContext.WhitePixel, bounds, BackgroundColor);

        // Draw border
        DrawBorder(spriteBatch, bounds, BorderColor, 1);

        // Draw scrollable text content
        DrawScrollableContent(spriteBatch, bounds);

        // Draw scrollbar if needed
        if (_maxScrollOffset > 0)
        {
            DrawScrollbar(spriteBatch, bounds);
        }
    }

    /// <summary>
    /// Draws the scrollable text content, rendering only visible lines.
    /// </summary>
    private void DrawScrollableContent(SpriteBatch spriteBatch, Rectangle bounds)
    {
        if (_font == null || _lines.Count == 0)
        {
            return;
        }

        var visibleHeight = bounds.Height - _padding * 2;
        var maxVisibleLines = (int)(visibleHeight / _lineHeight);

        // Calculate which lines to render
        var startLine = (int)(_scrollOffset / _lineHeight);
        var endLine = Math.Min(startLine + maxVisibleLines + 1, _lines.Count);

        // Draw visible lines
        var startY = bounds.Y + _padding;

        for (int i = startLine; i < endLine; i++)
        {
            var lineY = startY + (i - startLine) * _lineHeight;

            // Only draw if within bounds
            if (lineY >= bounds.Y && lineY < bounds.Bottom - _padding)
            {
                var position = new Vector2(bounds.X + _padding, lineY);
                spriteBatch.DrawString(_font, _lines[i], position, _textColor);
            }
        }
    }

    /// <summary>
    /// Draws the scrollbar.
    /// </summary>
    private void DrawScrollbar(SpriteBatch spriteBatch, Rectangle bounds)
    {
        var visibleHeight = bounds.Height - _padding * 2;
        var contentHeight = _lines.Count * _lineHeight;

        // Scrollbar track
        var trackBounds = new Rectangle(
            bounds.Right - _scrollbarWidth - _scrollbarPadding,
            bounds.Y + _scrollbarPadding,
            _scrollbarWidth,
            bounds.Height - _scrollbarPadding * 2
        );

        spriteBatch.Draw(SquidVoxEngineContext.WhitePixel, trackBounds, _scrollbarTrackColor);

        // Scrollbar thumb
        var thumbHeight = Math.Max(20, trackBounds.Height * (visibleHeight / contentHeight));
        var thumbPosition = trackBounds.Y + (trackBounds.Height - thumbHeight) * (_scrollOffset / _maxScrollOffset);

        var thumbBounds = new Rectangle(
            trackBounds.X,
            (int)thumbPosition,
            _scrollbarWidth,
            (int)thumbHeight
        );

        spriteBatch.Draw(SquidVoxEngineContext.WhitePixel, thumbBounds, _scrollbarThumbColor);
    }

    /// <summary>
    /// Draws a border around a rectangle.
    /// </summary>
    private void DrawBorder(SpriteBatch spriteBatch, Rectangle bounds, Color color, int width)
    {
        // Top
        spriteBatch.Draw(SquidVoxEngineContext.WhitePixel, new Rectangle(bounds.X, bounds.Y, bounds.Width, width), color);
        // Bottom
        spriteBatch.Draw(SquidVoxEngineContext.WhitePixel, new Rectangle(bounds.X, bounds.Bottom - width, bounds.Width, width), color);
        // Left
        spriteBatch.Draw(SquidVoxEngineContext.WhitePixel, new Rectangle(bounds.X, bounds.Y, width, bounds.Height), color);
        // Right
        spriteBatch.Draw(SquidVoxEngineContext.WhitePixel, new Rectangle(bounds.Right - width, bounds.Y, width, bounds.Height), color);
    }

    /// <summary>
    /// Updates the line list based on current text and panel width.
    /// </summary>
    private void UpdateLines()
    {
        _lines.Clear();

        if (_font == null || string.IsNullOrEmpty(_text))
        {
            _maxScrollOffset = 0;
            _scrollOffset = 0;
            return;
        }

        var maxLineWidth = (int)(Size.X - _padding * 2 - _scrollbarWidth - _scrollbarPadding * 2);
        var textLines = _text.Split('\n');

        // Wrap each line if needed
        foreach (var line in textLines)
        {
            var wrappedLines = WrapTextToLines(line, _font, maxLineWidth);
            _lines.AddRange(wrappedLines);
        }

        // Update max scroll offset
        var visibleHeight = Size.Y - _padding * 2;
        var contentHeight = _lines.Count * _lineHeight;
        var maxVisibleLines = (int)(visibleHeight / _lineHeight);

        _maxScrollOffset = Math.Max(0, (_lines.Count - maxVisibleLines) * _lineHeight);

        // Clamp current scroll offset
        _scrollOffset = Math.Clamp(_scrollOffset, 0f, _maxScrollOffset);
    }

    /// <summary>
    /// Wraps a single line of text into multiple lines.
    /// </summary>
    private List<string> WrapTextToLines(string text, DynamicSpriteFont font, int maxWidth)
    {
        var result = new List<string>();

        if (string.IsNullOrEmpty(text))
        {
            result.Add(string.Empty);
            return result;
        }

        var words = text.Split(' ');
        var currentLine = string.Empty;

        foreach (var word in words)
        {
            var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
            var testWidth = font.MeasureString(testLine).X;

            if (testWidth > maxWidth && !string.IsNullOrEmpty(currentLine))
            {
                result.Add(currentLine);
                currentLine = word;
            }
            else
            {
                currentLine = testLine;
            }
        }

        if (!string.IsNullOrEmpty(currentLine))
        {
            result.Add(currentLine);
        }

        if (result.Count == 0)
        {
            result.Add(string.Empty);
        }

        return result;
    }

    /// <summary>
    /// Scrolls to the top of the content.
    /// </summary>
    public void ScrollToTop()
    {
        _scrollOffset = 0;
    }

    /// <summary>
    /// Scrolls to the bottom of the content.
    /// </summary>
    public void ScrollToBottom()
    {
        _scrollOffset = _maxScrollOffset;
    }

    /// <summary>
    /// Scrolls by a specific amount.
    /// </summary>
    /// <param name="delta">Amount to scroll (positive = down, negative = up).</param>
    public void ScrollBy(float delta)
    {
        _scrollOffset = Math.Clamp(_scrollOffset + delta, 0f, _maxScrollOffset);
    }
}
