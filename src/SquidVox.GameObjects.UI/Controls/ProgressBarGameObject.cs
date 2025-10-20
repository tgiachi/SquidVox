using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Graphics;
using SquidVox.Core.Context;
using SquidVox.Core.GameObjects;

namespace SquidVox.GameObjects.UI.Controls;

public class ProgressBarGameObject : Base2dGameObject
{
    private float _value;
    private float _minimum;
    private float _maximum = 100f;

    public ProgressBarGameObject(float width = 200f, float height = 20f)
    {
        Size = new Vector2(width, height);

        BackgroundColor = new Color(200, 200, 200);
        ForegroundColor = new Color(0, 120, 215);
        BorderColor = Color.Gray;
        BorderWidth = 1;
    }

    public float Value
    {
        get => _value;
        set => _value = Math.Clamp(value, _minimum, _maximum);
    }

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

    public float Opacity { get; set; } = 1.0f;
    public Color BackgroundColor { get; set; }
    public Color ForegroundColor { get; set; }
    public Color BorderColor { get; set; }
    public int BorderWidth { get; set; }

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
