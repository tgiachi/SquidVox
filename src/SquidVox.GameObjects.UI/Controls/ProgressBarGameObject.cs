using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Graphics;
using SquidVox.Core.Context;
using SquidVox.Core.GameObjects;

namespace SquidVox.GameObjects.UI.Controls;

/// <summary>
/// Progress bar game object for displaying progress or value within a range.
/// </summary>
public class ProgressBarGameObject : Base2dGameObject
{
    private float _value;
    private float _minimum;
    private float _maximum = 100f;

    /// <summary>
    /// Initializes a new instance of the ProgressBarGameObject class.
    /// </summary>
    /// <param name="width">The width of the progress bar.</param>
    /// <param name="height">The height of the progress bar.</param>
    public ProgressBarGameObject(float width = 200f, float height = 20f)
    {
        Size = new Vector2(width, height);

        BackgroundColor = new Color(200, 200, 200);
        ForegroundColor = new Color(0, 120, 215);
        BorderColor = Color.Gray;
        BorderWidth = 1;
    }

    /// <summary>
    /// Gets or sets the current value of the progress bar.
    /// </summary>
    public float Value
    {
        get => _value;
        set => _value = Math.Clamp(value, _minimum, _maximum);
    }

    /// <summary>
    /// Gets or sets the minimum value of the progress bar.
    /// </summary>
    public float Minimum
    {
        get => _minimum;
        set
        {
            _minimum = value;
            if (_value < _minimum)
            {
                _value = _minimum;
            }
        }
    }

    /// <summary>
    /// Gets or sets the maximum value of the progress bar.
    /// </summary>
    public float Maximum
    {
        get => _maximum;
        set
        {
            _maximum = value;
            if (_value > _maximum)
            {
                _value = _maximum;
            }
        }
    }

    /// <summary>
    /// Gets or sets the opacity of the progress bar.
    /// </summary>
    public float Opacity { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets the background color of the progress bar.
    /// </summary>
    public Color BackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the foreground color of the progress bar.
    /// </summary>
    public Color ForegroundColor { get; set; }

    /// <summary>
    /// Gets or sets the border color of the progress bar.
    /// </summary>
    public Color BorderColor { get; set; }

    /// <summary>
    /// Gets or sets the border width of the progress bar.
    /// </summary>
    public int BorderWidth { get; set; }

    /// <summary>
    /// Gets the percentage of the current value relative to the minimum and maximum.
    /// </summary>
    /// <returns>The percentage as a float between 0 and 1.</returns>
    public float GetPercentage()
    {
        if (_maximum - _minimum == 0)
        {
            return 0f;
        }

        return (_value - _minimum) / (_maximum - _minimum);
    }

    protected override void OnRender(SpriteBatch spriteBatch)
    {
        if (!IsVisible)
        {
            return;
        }

        var absolutePos = GetAbsolutePosition();
        var bounds = new Rectangle((int)absolutePos.X, (int)absolutePos.Y, (int)Size.X, (int)Size.Y);

        var innerBounds = new Rectangle(
            bounds.X + BorderWidth,
            bounds.Y + BorderWidth,
            bounds.Width - BorderWidth * 2,
            bounds.Height - BorderWidth * 2
        );

        spriteBatch.Draw(SquidVoxGraphicContext.WhitePixel, innerBounds, BackgroundColor * Opacity);

        var percentage = GetPercentage();
        if (percentage > 0)
        {
            var fillWidth = (int)(innerBounds.Width * percentage);
            var fillBounds = new Rectangle(innerBounds.X, innerBounds.Y, fillWidth, innerBounds.Height);
            spriteBatch.Draw(SquidVoxGraphicContext.WhitePixel, fillBounds, ForegroundColor * Opacity);
        }

        if (BorderWidth > 0)
        {
            DrawBorder(spriteBatch, bounds, BorderColor);
        }
    }

    private void DrawBorder(SpriteBatch spriteBatch, Rectangle bounds, Color borderColor)
    {
        var color = borderColor * Opacity;

        spriteBatch.Draw(SquidVoxGraphicContext.WhitePixel, new Rectangle(bounds.X, bounds.Y, bounds.Width, BorderWidth), color);
        spriteBatch.Draw(
            SquidVoxGraphicContext.WhitePixel,
            new Rectangle(bounds.X, bounds.Bottom - BorderWidth, bounds.Width, BorderWidth),
            color
        );
        spriteBatch.Draw(SquidVoxGraphicContext.WhitePixel, new Rectangle(bounds.X, bounds.Y, BorderWidth, bounds.Height), color);
        spriteBatch.Draw(
            SquidVoxGraphicContext.WhitePixel,
            new Rectangle(bounds.Right - BorderWidth, bounds.Y, BorderWidth, bounds.Height),
            color
        );
    }
}
