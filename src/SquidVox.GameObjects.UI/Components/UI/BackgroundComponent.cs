using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SquidVox.Core.Context;
using SquidVox.Core.GameObjects;

namespace SquidVox.GameObjects.UI.Components.UI;

public class BackgroundComponent
{
    public Color BackgroundColor { get; set; } = Color.White;
    public Texture2D? BackgroundTexture { get; set; }
    public Rectangle? SourceRect { get; set; }
    public float Opacity { get; set; } = 1.0f;

    public void Draw(SpriteBatch spriteBatch, Base2dGameObject parent)
    {
        if (!parent.IsVisible) return;

        var bounds = new Rectangle(
            (int)parent.GetAbsolutePosition().X,
            (int)parent.GetAbsolutePosition().Y,
            (int)parent.Size.X,
            (int)parent.Size.Y
        );

        Color drawColor = BackgroundColor * Opacity;

        if (BackgroundTexture != null)
        {
            spriteBatch.Draw(BackgroundTexture, bounds, SourceRect, drawColor);
        }
        else
        {
            spriteBatch.Draw(SquidVoxGraphicContext.WhitePixel, bounds, drawColor);
        }
    }
}