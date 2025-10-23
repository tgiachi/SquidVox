using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SquidVox.Core.Context;
using SquidVox.Core.GameObjects;
using SquidVox.GameObjects.UI.Controls;

namespace SquidVox.World3d.GameObjects;

/// <summary>
/// Debug information panel that displays system and game stats.
/// </summary>
public sealed class DebugInfoPanel : Base2dGameObject
{
    private readonly LabelGameObject _chunksLoadedLabel;
    private readonly LabelGameObject _entitiesRenderedLabel;
    private readonly LabelGameObject _memoryUsageLabel;
    private readonly LabelGameObject _drawCallsLabel;
    private readonly LabelGameObject _positionLabel;
    private readonly LabelGameObject _biomeLabel;
    private float _updateInterval = 0.5f;
    private float _timeSinceLastUpdate;

    // Panel styling properties
    private readonly Color _backgroundColor;
    private readonly Color _borderColor;
    private readonly int _borderWidth;
    private readonly int _padding;

    /// <summary>
    /// Initializes a new instance of the <see cref="DebugInfoPanel"/>.
    /// </summary>
    /// <param name="fontName">Font used to render the labels.</param>
    /// <param name="fontSize">Font size used for rendering.</param>
    /// <param name="position">Screen position anchored to the panel.</param>
    public DebugInfoPanel(
        string fontName = "DefaultMono",
        int fontSize = 14,
        Vector2? position = null)
    {
        Name = "DebugInfoPanel";
        Position = position ?? Vector2.Zero;
        _backgroundColor = new Color(0, 0, 0, 180);
        _borderColor = new Color(255, 255, 255, 100);
        _borderWidth = 1;
        _padding = 8;

        var textColor = new Color(220, 220, 220);
        var lineHeight = fontSize + 4;

        _chunksLoadedLabel = new LabelGameObject("Chunks: --", fontName, fontSize, new Vector2(_padding, _padding), textColor);
        _entitiesRenderedLabel = new LabelGameObject("Entities: --", fontName, fontSize, new Vector2(_padding, _padding + lineHeight), textColor);
        _memoryUsageLabel = new LabelGameObject("Memory: --", fontName, fontSize, new Vector2(_padding, _padding + lineHeight * 2), textColor);
        _drawCallsLabel = new LabelGameObject("Draw Calls: --", fontName, fontSize, new Vector2(_padding, _padding + lineHeight * 3), textColor);
        _positionLabel = new LabelGameObject("Position: --", fontName, fontSize, new Vector2(_padding, _padding + lineHeight * 4), textColor);
        _biomeLabel = new LabelGameObject("Biome: --", fontName, fontSize, new Vector2(_padding, _padding + lineHeight * 5), textColor);

        AddChild(_chunksLoadedLabel);
        AddChild(_entitiesRenderedLabel);
        AddChild(_memoryUsageLabel);
        AddChild(_drawCallsLabel);
        AddChild(_positionLabel);
        AddChild(_biomeLabel);

        // Calculate panel size based on content
        Size = new Vector2(250, _padding * 2 + lineHeight * 6);
    }

    /// <summary>
    /// Gets or sets the number of chunks currently loaded.
    /// </summary>
    public int ChunksLoaded { get; set; }

    /// <summary>
    /// Gets or sets the number of entities being rendered.
    /// </summary>
    public int EntitiesRendered { get; set; }

    /// <summary>
    /// Gets or sets the current memory usage in bytes.
    /// </summary>
    public long MemoryUsageBytes { get; set; }

    /// <summary>
    /// Gets or sets the number of draw calls per frame.
    /// </summary>
    public int DrawCalls { get; set; }

    /// <summary>
    /// Gets or sets the current player/camera position.
    /// </summary>
    public Vector3 CurrentPosition { get; set; }

    /// <summary>
    /// Gets or sets the current biome name.
    /// </summary>
    public string CurrentBiome { get; set; } = "Unknown";

    /// <summary>
    /// Gets or sets the update interval in seconds.
    /// </summary>
    public float UpdateInterval
    {
        get => _updateInterval;
        set => _updateInterval = MathHelper.Max(0.1f, value);
    }

    /// <summary>
    /// Updates the debug info panel.
    /// </summary>
    public override void Update(GameTime gameTime)
    {
        _timeSinceLastUpdate += (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_timeSinceLastUpdate >= _updateInterval)
        {
            UpdateLabels();
            _timeSinceLastUpdate = 0f;
        }

        base.Update(gameTime);
    }

    /// <summary>
    /// Renders the debug info panel background.
    /// </summary>
    protected override void OnRender(SpriteBatch spriteBatch)
    {
        // Draw background
        var bounds = new Rectangle(
            (int)Position.X,
            (int)Position.Y,
            (int)Size.X,
            (int)Size.Y
        );

        spriteBatch.Draw(
            SquidVoxEngineContext.WhitePixel,
            bounds,
            _backgroundColor
        );

        // Draw border
        if (_borderWidth > 0)
        {
            DrawBorder(spriteBatch, bounds);
        }
    }

    private void UpdateLabels()
    {
        _chunksLoadedLabel.Text = $"Chunks: {ChunksLoaded}";
        _entitiesRenderedLabel.Text = $"Entities: {EntitiesRendered}";
        _memoryUsageLabel.Text = $"Memory: {FormatBytes(MemoryUsageBytes)}";
        _drawCallsLabel.Text = $"Draw Calls: {DrawCalls}";
        _positionLabel.Text = $"Position: {CurrentPosition.X:F1}, {CurrentPosition.Y:F1}, {CurrentPosition.Z:F1}";
        _biomeLabel.Text = $"Biome: {CurrentBiome}";
    }

    private void DrawBorder(SpriteBatch spriteBatch, Rectangle bounds)
    {
        // Top
        spriteBatch.Draw(
            SquidVoxEngineContext.WhitePixel,
            new Rectangle(bounds.X, bounds.Y, bounds.Width, _borderWidth),
            _borderColor
        );

        // Bottom
        spriteBatch.Draw(
            SquidVoxEngineContext.WhitePixel,
            new Rectangle(bounds.X, bounds.Y + bounds.Height - _borderWidth, bounds.Width, _borderWidth),
            _borderColor
        );

        // Left
        spriteBatch.Draw(
            SquidVoxEngineContext.WhitePixel,
            new Rectangle(bounds.X, bounds.Y, _borderWidth, bounds.Height),
            _borderColor
        );

        // Right
        spriteBatch.Draw(
            SquidVoxEngineContext.WhitePixel,
            new Rectangle(bounds.X + bounds.Width - _borderWidth, bounds.Y, _borderWidth, bounds.Height),
            _borderColor
        );
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:F2} {sizes[order]}";
    }
}
