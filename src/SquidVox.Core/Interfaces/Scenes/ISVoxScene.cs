using SquidVox.Core.Collections;
using SquidVox.Core.Interfaces.GameObjects;

namespace SquidVox.Core.Interfaces.Scenes;

/// <summary>
/// Defines the contract for a scene in the SquidVox engine.
/// A scene represents a distinct state or screen in the application (e.g., menu, gameplay, settings).
/// </summary>
public interface ISVoxScene : ISVoxUpdateable, ISVox2dRenderable, ISVoxInputReceiver
{
    /// <summary>
    /// Gets the name of the scene.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the collection of 2D game objects in this scene.
    /// </summary>
    SvoxGameObjectCollection<ISVox2dDrawableGameObject> Components { get; }

    /// <summary>
    /// Gets the collection of 3D game objects in this scene.
    /// </summary>
    SvoxGameObjectCollection<ISVox3dDrawableGameObject> Components3d { get; }

    /// <summary>
    /// Called when the scene is loaded and becomes active.
    /// Use this to initialize scene-specific resources.
    /// </summary>
    void Load();

    /// <summary>
    /// Called when the scene is unloaded and becomes inactive.
    /// Use this to cleanup scene-specific resources.
    /// </summary>
    void Unload();
}
