using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SquidVox.Core.Context;
using SquidVox.Core.GameObjects;

namespace SquidVox.GameObjects.UI.Components.UI;

/// <summary>
/// Component for rendering background textures or colors.
/// </summary>
public class BackgroundComponent
{
    /// <summary>
    /// Gets or sets the background color.
    /// </summary>
    public Color BackgroundColor { get; set; } = Color.White;

    /// <summary>
    /// Gets or sets the background texture.
    /// </summary>
    public Texture2D? BackgroundTexture { get; set; }

    /// <summary>
    /// Gets or sets the source rectangle for the texture.
    /// </summary>
    public Rectangle? SourceRect { get; set; }

    /// <summary>
    /// Gets or sets the opacity of the background.
    /// </summary>
    public float Opacity { get; set; } = 1.0f;

    /// <summary>
    /// Draws the background for the specified parent game object.
    /// </summary>
    /// <param name="spriteBatch">The sprite batch to use for drawing.</param>
    /// <param name="parent">The parent game object.</param>
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
            spriteBatch.Draw(SquidVoxEngineContext.WhitePixel, bounds, drawColor);
        }
    }
}
