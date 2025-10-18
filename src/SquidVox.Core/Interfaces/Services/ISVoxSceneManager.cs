using SquidVox.Core.Interfaces.Scenes;

namespace SquidVox.Core.Interfaces.Services;

/// <summary>
/// Defines the contract for scene management services.
/// </summary>
public interface ISVoxSceneManager : IDisposable
{
    /// <summary>
    /// Gets the currently active scene.
    /// </summary>
    ISVoxScene? CurrentScene { get; }

    /// <summary>
    /// Gets a value indicating whether a scene transition is in progress.
    /// </summary>
    bool IsTransitioning { get; }

    /// <summary>
    /// Gets the current scene transition, if any.
    /// </summary>
    ISVoxSceneTransition? CurrentTransition { get; }

    /// <summary>
    /// Registers a scene with the scene manager.
    /// </summary>
    /// <param name="scene">The scene to register.</param>
    void RegisterScene(ISVoxScene scene);

    /// <summary>
    /// Unregisters a scene from the scene manager.
    /// </summary>
    /// <param name="sceneName">The name of the scene to unregister.</param>
    /// <returns>True if the scene was unregistered, false if not found.</returns>
    bool UnregisterScene(string sceneName);

    /// <summary>
    /// Gets a registered scene by name.
    /// </summary>
    /// <param name="sceneName">The name of the scene.</param>
    /// <returns>The scene if found, otherwise null.</returns>
    ISVoxScene? GetScene(string sceneName);

    /// <summary>
    /// Changes to a different scene immediately without a transition.
    /// </summary>
    /// <param name="sceneName">The name of the scene to change to.</param>
    void ChangeScene(string sceneName);

    /// <summary>
    /// Changes to a different scene immediately without a transition.
    /// </summary>
    /// <param name="scene">The scene to change to.</param>
    void ChangeScene(ISVoxScene scene);

    /// <summary>
    /// Changes to a different scene with a transition effect.
    /// </summary>
    /// <param name="sceneName">The name of the scene to change to.</param>
    /// <param name="transition">The transition to use.</param>
    void ChangeScene(string sceneName, ISVoxSceneTransition transition);

    /// <summary>
    /// Changes to a different scene with a transition effect.
    /// </summary>
    /// <param name="scene">The scene to change to.</param>
    /// <param name="transition">The transition to use.</param>
    void ChangeScene(ISVoxScene scene, ISVoxSceneTransition transition);
}
