using DryIoc;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SquidVox.Core.Context;
using SquidVox.Core.Data.Scripts;
using SquidVox.Core.GameObjects;
using SquidVox.Core.Interfaces.Services;

namespace SquidVox.GameObjects.UI.Controls;

/// <summary>
/// A modal dialog that displays Lua script error information.
/// This component shows detailed error information including message, file location, line number, and stack trace.
/// </summary>
public class ScriptErrorGameObject : Base2dGameObject
{
    private readonly ScriptErrorInfo _errorInfo;
    private DynamicSpriteFont? _titleFont;
    private DynamicSpriteFont? _contentFont;
    private ButtonGameObject? _closeButton;
    private ScrollableTextGameObject? _stackTracePanel;
    private bool _isInitialized;

    // Dialog dimensions and styling
    private const int DialogWidth = 700;
    private const int DialogMaxHeight = 500;
    private const int DialogPadding = 20;
    private const int TitleBarHeight = 40;
    private const int ButtonHeight = 35;
    private const int ButtonWidth = 100;
    private const int Spacing = 10;

    // Colors
    private readonly Color _overlayColor = new Color(0, 0, 0, 180);        // Semi-transparent black
    private readonly Color _dialogBackgroundColor = new Color(40, 40, 40); // Dark gray
    private readonly Color _titleBarColor = new Color(200, 50, 50);        // Red for errors
    private readonly Color _titleTextColor = Color.White;
    private readonly Color _labelColor = new Color(200, 200, 200);
    private readonly Color _errorTextColor = new Color(255, 100, 100);
    private readonly Color _codeBackgroundColor = new Color(30, 30, 30);

    /// <summary>
    /// Event fired when the dialog is closed.
    /// </summary>
    public event EventHandler? Closed;

    /// <summary>
    /// Initializes a new instance of the ScriptErrorGameObject class.
    /// </summary>
    /// <param name="errorInfo">The error information to display.</param>
    /// <param name="assetManagerService">The asset manager service for loading fonts.</param>
    public ScriptErrorGameObject(ScriptErrorInfo errorInfo)
    {
        _errorInfo = errorInfo ?? throw new ArgumentNullException(nameof(errorInfo));

        Name = "ScriptErrorDialog";
        ZIndex = 1000; // Ensure it's rendered on top of everything

        // Full screen overlay
        Size = Vector2.Zero; // Will be set during initialization
        Position = Vector2.Zero;
    }

    /// <summary>
    /// Initializes the dialog resources.
    /// </summary>
    /// <param name="assetManagerService">Asset manager service for loading resources.</param>
    /// <param name="graphicsDevice">Graphics device for creating textures.</param>
    public void Initialize(IAssetManagerService assetManagerService, GraphicsDevice graphicsDevice)
    {
        if (_isInitialized)
        {
            return;
        }

        // Set full screen size
        Size = new Vector2(graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height);

        // Load fonts
        LoadFonts(assetManagerService);

        // Create close button
        CreateCloseButton(assetManagerService);

        _isInitialized = true;
    }

    /// <summary>
    /// Loads the fonts for the dialog.
    /// </summary>
    private void LoadFonts(IAssetManagerService assetManagerService)
    {
        try
        {
            _titleFont = assetManagerService.GetFont("Monocraft", 18);
            _contentFont = assetManagerService.GetFont("Monocraft", 14);
        }
        catch
        {
            // Fonts might not load, will be handled in rendering
        }
    }

    /// <summary>
    /// Creates the close button for the dialog.
    /// </summary>
    private void CreateCloseButton(IAssetManagerService assetManagerService)
    {
        _closeButton = new ButtonGameObject(
            text: "Close",
            width: ButtonWidth,
            height: ButtonHeight,
            fontName: "Monocraft",
            fontSize: 14,
            assetManagerService: assetManagerService
        );

        _closeButton.Initialize(assetManagerService, SquidVoxGraphicContext.GraphicsDevice!);

        // Style the button
        _closeButton.SetFlatStyle(
            backgroundColor: new Color(70, 70, 70),
            textColor: Color.White,
            hoverColor: new Color(90, 90, 90)
        );

        _closeButton.Click += OnCloseButtonClick;

        // Position will be set during rendering
        AddChild(_closeButton);
    }

    /// <summary>
    /// Handles the close button click event.
    /// </summary>
    private void OnCloseButtonClick(object? sender, EventArgs e)
    {
        Close();
    }

    /// <summary>
    /// Closes the dialog and fires the Closed event.
    /// </summary>
    public void Close()
    {
        Closed?.Invoke(this, EventArgs.Empty);
    }


    /// <summary>
    /// Renders the error dialog.
    /// </summary>
    protected override void OnRender(SpriteBatch spriteBatch)
    {
        if (!IsVisible)
        {
            return;
        }

        var viewport = spriteBatch.GraphicsDevice.Viewport;

        // Draw full-screen overlay
        DrawOverlay(spriteBatch, viewport);

        // Calculate dialog position (centered)
        var dialogX = (viewport.Width - DialogWidth) / 2;
        var dialogY = 50; // Some padding from top

        // Calculate content height
        var contentHeight = CalculateContentHeight();
        var dialogHeight = Math.Min(contentHeight + TitleBarHeight + ButtonHeight + DialogPadding * 3, DialogMaxHeight);

        var dialogBounds = new Rectangle(dialogX, dialogY, DialogWidth, dialogHeight);

        // Draw dialog background
        DrawDialogBackground(spriteBatch, dialogBounds);

        // Draw title bar
        DrawTitleBar(spriteBatch, dialogBounds);

        // Draw error content
        DrawErrorContent(spriteBatch, dialogBounds);

        // Position and draw close button
        if (_closeButton != null)
        {
            _closeButton.Position = new Vector2(
                dialogBounds.X + dialogBounds.Width - ButtonWidth - DialogPadding,
                dialogBounds.Y + dialogBounds.Height - ButtonHeight - DialogPadding
            );
        }
    }

    /// <summary>
    /// Draws the semi-transparent overlay.
    /// </summary>
    private void DrawOverlay(SpriteBatch spriteBatch, Viewport viewport)
    {
        var overlayBounds = new Rectangle(0, 0, viewport.Width, viewport.Height);
        spriteBatch.Draw(SquidVoxGraphicContext.WhitePixel, overlayBounds, _overlayColor);
    }

    /// <summary>
    /// Draws the dialog background.
    /// </summary>
    private void DrawDialogBackground(SpriteBatch spriteBatch, Rectangle bounds)
    {
        spriteBatch.Draw(SquidVoxGraphicContext.WhitePixel, bounds, _dialogBackgroundColor);

        // Draw border
        var borderColor = new Color(100, 100, 100);
        var borderWidth = 2;
        DrawBorder(spriteBatch, bounds, borderColor, borderWidth);
    }

    /// <summary>
    /// Draws a border around a rectangle.
    /// </summary>
    private void DrawBorder(SpriteBatch spriteBatch, Rectangle bounds, Color color, int width)
    {
        // Top
        spriteBatch.Draw(SquidVoxGraphicContext.WhitePixel, new Rectangle(bounds.X, bounds.Y, bounds.Width, width), color);
        // Bottom
        spriteBatch.Draw(
            SquidVoxGraphicContext.WhitePixel,
            new Rectangle(bounds.X, bounds.Bottom - width, bounds.Width, width),
            color
        );
        // Left
        spriteBatch.Draw(SquidVoxGraphicContext.WhitePixel, new Rectangle(bounds.X, bounds.Y, width, bounds.Height), color);
        // Right
        spriteBatch.Draw(
            SquidVoxGraphicContext.WhitePixel,
            new Rectangle(bounds.Right - width, bounds.Y, width, bounds.Height),
            color
        );
    }

    /// <summary>
    /// Draws the title bar.
    /// </summary>
    private void DrawTitleBar(SpriteBatch spriteBatch, Rectangle dialogBounds)
    {
        var titleBarBounds = new Rectangle(dialogBounds.X, dialogBounds.Y, dialogBounds.Width, TitleBarHeight);
        spriteBatch.Draw(SquidVoxGraphicContext.WhitePixel, titleBarBounds, _titleBarColor);

        if (_titleFont != null)
        {
            var title = "Lua Script Error";
            var titlePosition = new Vector2(
                titleBarBounds.X + DialogPadding,
                titleBarBounds.Y + (TitleBarHeight - _titleFont.MeasureString(title).Y) / 2f
            );
            spriteBatch.DrawString(_titleFont, title, titlePosition, _titleTextColor);
        }
    }

    /// <summary>
    /// Draws the error content.
    /// </summary>
    private void DrawErrorContent(SpriteBatch spriteBatch, Rectangle dialogBounds)
    {
        if (_contentFont == null)
        {
            return;
        }

        var contentX = dialogBounds.X + DialogPadding;
        var currentY = dialogBounds.Y + TitleBarHeight + DialogPadding;
        var maxWidth = DialogWidth - DialogPadding * 2;

        // Error Type and Message
        if (!string.IsNullOrEmpty(_errorInfo.ErrorType))
        {
            var errorTypeText = $"{_errorInfo.ErrorType}";
            spriteBatch.DrawString(_contentFont, errorTypeText, new Vector2(contentX, currentY), _errorTextColor);
            currentY += (int)_contentFont.MeasureString(errorTypeText).Y + Spacing;
        }

        if (!string.IsNullOrEmpty(_errorInfo.Message))
        {
            var wrappedMessage = WrapText(_errorInfo.Message, _contentFont, maxWidth);
            spriteBatch.DrawString(_contentFont, wrappedMessage, new Vector2(contentX, currentY), _labelColor);
            currentY += (int)_contentFont.MeasureString(wrappedMessage).Y + Spacing * 2;
        }

        // File and Line information
        if (!string.IsNullOrEmpty(_errorInfo.FileName) || _errorInfo.LineNumber.HasValue)
        {
            var locationText = $"File: {_errorInfo.FileName ?? "Unknown"}";
            if (_errorInfo.LineNumber.HasValue)
            {
                locationText += $":{_errorInfo.LineNumber}";
                if (_errorInfo.ColumnNumber.HasValue)
                {
                    locationText += $":{_errorInfo.ColumnNumber}";
                }
            }

            spriteBatch.DrawString(_contentFont, locationText, new Vector2(contentX, currentY), _labelColor);
            currentY += (int)_contentFont.MeasureString(locationText).Y + Spacing * 2;
        }

        // Stack trace
        if (!string.IsNullOrEmpty(_errorInfo.StackTrace))
        {
            var stackTraceLabel = "Stack Trace:";
            spriteBatch.DrawString(_contentFont, stackTraceLabel, new Vector2(contentX, currentY), _labelColor);
            currentY += (int)_contentFont.MeasureString(stackTraceLabel).Y + Spacing;

            // Create or update scrollable text panel for stack trace
            var maxStackHeight = dialogBounds.Bottom - currentY - ButtonHeight - DialogPadding * 2 - Spacing;

            if (_stackTracePanel == null)
            {
                _stackTracePanel = new ScrollableTextGameObject(maxWidth, maxStackHeight);
                _stackTracePanel.Initialize("DefaultMono", 12);
                _stackTracePanel.Text = _errorInfo.StackTrace;
                _stackTracePanel.BackgroundColor = _codeBackgroundColor;
                _stackTracePanel.BorderColor = new Color(60, 60, 60);
                _stackTracePanel.TextColor = new Color(180, 180, 180);
                AddChild(_stackTracePanel);
            }

            _stackTracePanel.Position = new Vector2(contentX, currentY);
            _stackTracePanel.Size = new Vector2(maxWidth, maxStackHeight);
        }
    }

    /// <summary>
    /// Calculates the total height needed for the dialog content.
    /// </summary>
    private int CalculateContentHeight()
    {
        if (_contentFont == null)
        {
            return 200;
        }

        var height = 0;
        var maxWidth = DialogWidth - DialogPadding * 2;

        if (!string.IsNullOrEmpty(_errorInfo.ErrorType))
        {
            height += (int)_contentFont.MeasureString(_errorInfo.ErrorType).Y + Spacing;
        }

        if (!string.IsNullOrEmpty(_errorInfo.Message))
        {
            var wrappedMessage = WrapText(_errorInfo.Message, _contentFont, maxWidth);
            height += (int)_contentFont.MeasureString(wrappedMessage).Y + Spacing * 2;
        }

        if (!string.IsNullOrEmpty(_errorInfo.FileName) || _errorInfo.LineNumber.HasValue)
        {
            height += (int)_contentFont.MeasureString("File:").Y + Spacing * 2;
        }

        if (!string.IsNullOrEmpty(_errorInfo.StackTrace))
        {
            height += (int)_contentFont.MeasureString("Stack Trace:").Y + Spacing;
            height += 150; // Fixed height for stack trace box
        }

        return height;
    }

    /// <summary>
    /// Wraps text to fit within a specified width.
    /// </summary>
    private string WrapText(string text, DynamicSpriteFont font, int maxWidth)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var words = text.Split(' ');
        var lines = new System.Collections.Generic.List<string>();
        var currentLine = string.Empty;

        foreach (var word in words)
        {
            var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
            var testWidth = font.MeasureString(testLine).X;

            if (testWidth > maxWidth && !string.IsNullOrEmpty(currentLine))
            {
                lines.Add(currentLine);
                currentLine = word;
            }
            else
            {
                currentLine = testLine;
            }
        }

        if (!string.IsNullOrEmpty(currentLine))
        {
            lines.Add(currentLine);
        }

        return string.Join("\n", lines);
    }
}
