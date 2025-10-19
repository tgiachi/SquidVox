using FontStashSharp.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SquidVox.Core.Scenes.Transitions;

/// <summary>
/// A simple fade transition that fades out the current scene and fades in the new scene.
/// </summary>
public class FadeTransition : SceneTransition
{
    private readonly Color _fadeColor;

    /// <summary>
    /// Initializes a new instance of the FadeTransition class.
    /// </summary>
    /// <param name="duration">Duration of the transition in seconds (default: 0.5 seconds).</param>
    /// <param name="fadeColor">Color to fade to (default: black).</param>
    public FadeTransition(float duration = 0.5f, Color? fadeColor = null)
        : base(duration)
    {
        _fadeColor = fadeColor ?? Color.Black;
    }

    /// <summary>
    /// Renders the fade transition.
    /// </summary>
    /// <param name="spriteBatch">SpriteBatch for rendering textures.</param>
    public override void Render(SpriteBatch spriteBatch)
    {
        // First half: render from scene fading out
        if (Progress < 0.5f && FromScene != null)
        {
            FromScene.Render(spriteBatch);

            // Fade out overlay
            var alpha = (byte)(Progress * 2f * 255f);
            // TODO: Render a fullscreen quad with _fadeColor and alpha
            // This would require a fullscreen quad primitive or render target
        }
        // Second half: render to scene fading in
        else if (ToScene != null)
        {
            ToScene.Render(spriteBatch);

            // Fade in overlay
            var alpha = (byte)((1f - (Progress - 0.5f) * 2f) * 255f);
            // TODO: Render a fullscreen quad with _fadeColor and alpha
        }
    }
}
