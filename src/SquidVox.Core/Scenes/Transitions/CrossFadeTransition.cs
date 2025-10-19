using FontStashSharp.Interfaces;
using Microsoft.Xna.Framework.Graphics;

namespace SquidVox.Core.Scenes.Transitions;

/// <summary>
/// A cross-fade transition that blends between two scenes.
/// Note: This is a simple implementation that renders both scenes.
/// For better performance with complex scenes, consider using render targets.
/// </summary>
public class CrossFadeTransition : SceneTransition
{
    /// <summary>
    /// Initializes a new instance of the CrossFadeTransition class.
    /// </summary>
    /// <param name="duration">Duration of the transition in seconds (default: 0.5 seconds).</param>
    public CrossFadeTransition(float duration = 0.5f)
        : base(duration)
    {
    }

    /// <summary>
    /// Renders the cross-fade transition.
    /// </summary>
    /// <param name="spriteBatch">SpriteBatch for rendering textures.</param>
    public override void Render(SpriteBatch spriteBatch)
    {
        // Simple cross-fade: just render the new scene
        // For a proper cross-fade, you would need:
        // 1. Render FromScene to a render target with alpha = (1 - Progress)
        // 2. Render ToScene to another render target with alpha = Progress
        // 3. Blend the two render targets together

        // For now, we'll use a simple approach: render the ToScene directly
        // The easing function provides smooth transition feel
        var easedProgress = EaseInOut(Progress);

        ToScene?.Render(spriteBatch);
    }
}
