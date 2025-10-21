using SquidVox.Core.Attributes.Scripts;
using SquidVox.Core.Interfaces.Scenes;
using SquidVox.Core.Interfaces.Services;
using SquidVox.Core.Scenes.Transitions;

namespace SquidVox.World3d.Modules;

[ScriptModule("scenes", "Scene Module for managing scenes.")]
public class SceneModule
{
    private readonly ISceneManager _sceneManager;

    public SceneModule(ISceneManager sceneManager)
    {
        _sceneManager = sceneManager;
    }

    [ScriptFunction("load_scene", "Loads a scene by its name.")]
    public void LoadScene(string sceneName)
    {
        _sceneManager.ChangeScene(sceneName, new FadeTransition());
    }

    [ScriptFunction("get_current_scene", "Gets the name of the current scene.")]
    public string GetCurrentScene()
    {
        return _sceneManager.CurrentScene.Name;
    }

    [ScriptFunction("get_current_scene_object", "Gets the current scene object.")]
    public ISVoxScene GetCurrentSceneObject()
    {
        return _sceneManager.CurrentScene;
    }
}
