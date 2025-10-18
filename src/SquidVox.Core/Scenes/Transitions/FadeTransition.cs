using FontStashSharp.Interfaces;
using TrippyGL;

namespace SquidVox.Core.Scenes.Transitions;

/// <summary>
/// A simple fade transition that fades out the current scene and fades in the new scene.
/// </summary>
public class FadeTransition : SceneTransition
{
    private readonly Color4b _fadeColor;

    /// <summary>
    /// Initializes a new instance of the FadeTransition class.
    /// </summary>
    /// <param name="duration">Duration of the transition in seconds (default: 0.5 seconds).</param>
    /// <param name="fadeColor">Color to fade to (default: black).</param>
    public FadeTransition(float duration = 0.5f, Color4b? fadeColor = null)
        : base(duration)
    {
        _fadeColor = fadeColor ?? Color4b.Black;
    }

    /// <summary>
    /// Renders the fade transition.
    /// </summary>
    /// <param name="textureBatcher">TextureBatcher for rendering textures.</param>
    /// <param name="fontRenderer">Font renderer for drawing text.</param>
    public override void Render(TextureBatcher textureBatcher, IFontStashRenderer fontRenderer)
    {
        // First half: render from scene fading out
        if (Progress < 0.5f && FromScene != null)
        {
            FromScene.Render(textureBatcher, fontRenderer);

            // Fade out overlay
            var alpha = (byte)(Progress * 2f * 255f);
            // TODO: Render a fullscreen quad with _fadeColor and alpha
            // This would require a fullscreen quad primitive or render target
        }
        // Second half: render to scene fading in
        else if (ToScene != null)
        {
            ToScene.Render(textureBatcher, fontRenderer);

            // Fade in overlay
            var alpha = (byte)((1f - (Progress - 0.5f) * 2f) * 255f);
            // TODO: Render a fullscreen quad with _fadeColor and alpha
        }
    }
}
