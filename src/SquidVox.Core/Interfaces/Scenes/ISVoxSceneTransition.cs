using SquidVox.Core.Interfaces.GameObjects;

namespace SquidVox.Core.Interfaces.Scenes;

/// <summary>
/// Defines the contract for scene transitions that handle the visual transition between scenes.
/// </summary>
public interface ISVoxSceneTransition : ISVoxUpdateable, ISVox2dRenderable, IDisposable
{
    /// <summary>
    /// Gets the scene being transitioned from.
    /// </summary>
    ISVoxScene? FromScene { get; }

    /// <summary>
    /// Gets the scene being transitioned to.
    /// </summary>
    ISVoxScene? ToScene { get; }

    /// <summary>
    /// Gets a value indicating whether the transition has completed.
    /// </summary>
    bool IsCompleted { get; }

    /// <summary>
    /// Event fired when the transition completes.
    /// </summary>
    event EventHandler? Completed;

    /// <summary>
    /// Starts the transition between two scenes.
    /// </summary>
    /// <param name="fromScene">The scene to transition from (null for initial scene).</param>
    /// <param name="toScene">The scene to transition to.</param>
    void Start(ISVoxScene? fromScene, ISVoxScene toScene);
}
