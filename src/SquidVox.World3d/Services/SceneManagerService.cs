using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Serilog;
using SquidVox.Core.Interfaces.Scenes;
using SquidVox.Core.Interfaces.Services;

namespace SquidVox.World3d.Services;

/// <summary>
/// Implements the scene management service.
/// </summary>
public class SceneManagerService : ISceneManager
{
    private readonly ILogger _logger = Log.ForContext<SceneManagerService>();
    private readonly Dictionary<string, ISVoxScene> _registeredScenes = new();

    /// <summary>
    /// Gets the currently active scene.
    /// </summary>
    public ISVoxScene? CurrentScene { get; private set; }

    /// <summary>
    /// Gets a value indicating whether a scene transition is in progress.
    /// </summary>
    public bool IsTransitioning { get; private set; }

    /// <summary>
    /// Gets the current scene transition, if any.
    /// </summary>
    public ISVoxSceneTransition? CurrentTransition { get; private set; }

    /// <summary>
    /// Registers a scene with the scene manager.
    /// </summary>
    /// <param name="scene">The scene to register.</param>
    public void RegisterScene(ISVoxScene scene)
    {
        if (_registeredScenes.ContainsKey(scene.Name))
        {
            _logger.Warning("Scene {Name} is already registered, replacing it", scene.Name);
        }

        _registeredScenes[scene.Name] = scene;
        _logger.Debug("Registered scene {Name}", scene.Name);
    }

    /// <summary>
    /// Unregisters a scene from the scene manager.
    /// </summary>
    /// <param name="sceneName">The name of the scene to unregister.</param>
    /// <returns>True if the scene was unregistered, false if not found.</returns>
    public bool UnregisterScene(string sceneName)
    {
        if (_registeredScenes.Remove(sceneName))
        {
            _logger.Debug("Unregistered scene {Name}", sceneName);
            return true;
        }

        _logger.Warning("Scene {Name} not found for unregistration", sceneName);
        return false;
    }

    /// <summary>
    /// Gets a registered scene by name.
    /// </summary>
    /// <param name="sceneName">The name of the scene.</param>
    /// <returns>The scene if found, otherwise null.</returns>
    public ISVoxScene? GetScene(string sceneName)
    {
        if (_registeredScenes.TryGetValue(sceneName, out var scene))
        {
            return scene;
        }

        _logger.Warning("Scene {Name} not found", sceneName);
        return null;
    }

    /// <summary>
    /// Changes to a different scene immediately without a transition.
    /// </summary>
    /// <param name="sceneName">The name of the scene to change to.</param>
    public void ChangeScene(string sceneName)
    {
        var scene = GetScene(sceneName);
        if (scene != null)
        {
            ChangeScene(scene);
        }
    }

    /// <summary>
    /// Changes to a different scene immediately without a transition.
    /// </summary>
    /// <param name="scene">The scene to change to.</param>
    public void ChangeScene(ISVoxScene scene)
    {
        if (IsTransitioning)
        {
            _logger.Warning("Cannot change scene while a transition is in progress");
            return;
        }

        _logger.Information("Changing scene from {From} to {To}", CurrentScene?.Name ?? "none", scene.Name);

        // Unload current scene
        CurrentScene?.Unload();

        // Set new scene and load it
        CurrentScene = scene;
        CurrentScene.Load();
    }

    /// <summary>
    /// Changes to a different scene with a transition effect.
    /// </summary>
    /// <param name="sceneName">The name of the scene to change to.</param>
    /// <param name="transition">The transition to use.</param>
    public void ChangeScene(string sceneName, ISVoxSceneTransition transition)
    {
        var scene = GetScene(sceneName);
        if (scene != null)
        {
            ChangeScene(scene, transition);
        }
    }

    /// <summary>
    /// Changes to a different scene with a transition effect.
    /// </summary>
    /// <param name="scene">The scene to change to.</param>
    /// <param name="transition">The transition to use.</param>
    public void ChangeScene(ISVoxScene scene, ISVoxSceneTransition transition)
    {
        if (IsTransitioning)
        {
            _logger.Warning("Cannot change scene while a transition is in progress");
            return;
        }

        _logger.Information(
            "Changing scene from {From} to {To} with transition",
            CurrentScene?.Name ?? "none",
            scene.Name
        );

        IsTransitioning = true;
        CurrentTransition = transition;

        // Load the new scene but don't unload the old one yet (for transition rendering)
        scene.Load();

        // Start the transition
        transition.Start(CurrentScene, scene);

        // Subscribe to transition completion
        transition.Completed += OnTransitionCompleted;
    }

    /// <summary>
    /// Updates the current scene or transition.
    /// </summary>
    /// <param name="gameTime">Game timing information.</param>
    public void Update(GameTime gameTime)
    {
        if (IsTransitioning && CurrentTransition != null)
        {
            CurrentTransition.Update(gameTime);
        }
        else if (CurrentScene != null)
        {
            CurrentScene.Update(gameTime);
        }
    }

    /// <summary>
    /// Renders the current scene or transition.
    /// </summary>
    /// <param name="textureBatcher">TextureBatcher for rendering textures.</param>
    /// <param name="fontRenderer">Font renderer for drawing text.</param>
    public void Render(SpriteBatch spriteBatch)
    {
        if (IsTransitioning && CurrentTransition != null)
        {
            CurrentTransition.Render(spriteBatch);
        }
        else if (CurrentScene != null)
        {
            CurrentScene.Render(spriteBatch);
        }
    }

    /// <summary>
    /// Handles keyboard input for the current scene.
    /// </summary>
    /// <param name="keyboard">The keyboard device.</param>
    /// <param name="gameTime">Game timing information.</param>
    public void HandleKeyboard(KeyboardState keyboard, GameTime gameTime)
    {
        // Don't handle input during transitions
        if (!IsTransitioning && CurrentScene != null)
        {
            CurrentScene.HandleKeyboard(keyboard, gameTime);
        }
    }

    /// <summary>
    /// Handles mouse input for the current scene.
    /// </summary>
    /// <param name="mouse">The mouse device.</param>
    /// <param name="gameTime">Game timing information.</param>
    public void HandleMouse(MouseState mouse, GameTime gameTime)
    {
        // Don't handle input during transitions
        if (!IsTransitioning && CurrentScene != null)
        {
            CurrentScene.HandleMouse(mouse, gameTime);
        }
    }

    /// <summary>
    /// Called when a scene transition completes.
    /// </summary>
    private void OnTransitionCompleted(object? sender, EventArgs e)
    {
        if (CurrentTransition == null)
        {
            return;
        }

        _logger.Debug("Transition completed");

        // Unsubscribe from transition event
        CurrentTransition.Completed -= OnTransitionCompleted;

        // Unload the old scene
        if (CurrentTransition.FromScene != null)
        {
            CurrentTransition.FromScene.Unload();
        }

        // Set the new scene as current
        CurrentScene = CurrentTransition.ToScene;

        // Dispose the transition
        CurrentTransition.Dispose();
        CurrentTransition = null;

        IsTransitioning = false;
    }

    /// <summary>
    /// Disposes of the scene manager and all registered scenes.
    /// </summary>
    public void Dispose()
    {
        _logger.Debug("Disposing scene manager");

        // Dispose current transition if active
        if (CurrentTransition != null)
        {
            CurrentTransition.Completed -= OnTransitionCompleted;
            CurrentTransition.Dispose();
            CurrentTransition = null;
        }

        // Unload current scene
        CurrentScene?.Unload();

        // Clear registered scenes
        _registeredScenes.Clear();

        GC.SuppressFinalize(this);
    }
}
