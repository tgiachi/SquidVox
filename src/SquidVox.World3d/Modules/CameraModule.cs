using SquidVox.Core.Attributes.Scripts;
using SquidVox.Core.Collections;
using SquidVox.Voxel.GameObjects;

namespace SquidVox.World3d.Modules;

[ScriptModule("camera", "Provides camera functionality for 3D rendering.")]
public class CameraModule
{
    private readonly RenderLayerCollection _renderLayers;
    private CameraGameObject _camera => _renderLayers.GetComponent<CameraGameObject>();
    private WorldGameObject _world => _renderLayers.GetComponent<WorldGameObject>();

    public CameraModule(RenderLayerCollection renderLayers)
    {
        _renderLayers = renderLayers;
    }

    [ScriptFunction("toggleWireframe", "Toggles the wireframe rendering mode for the world.")]
    public void ToggleWireframe()
    {
        _world.EnableWireframe = !_world.EnableWireframe;
    }

    [ScriptFunction("setWireframe", "Enables or disables wireframe rendering mode for the world.")]
    public void SetWireframe(bool enable)
    {
        _world.EnableWireframe = enable;
    }

    [ScriptFunction("setFov", "Sets the camera's field of view (FOV) in degrees.")]
    public void SetFieldOfView(float fovDegrees)
    {
        _camera.FieldOfView = fovDegrees;
    }

    [ScriptFunction("toggleInput", "Toggles the camera input handling on or off.")]
    public void ToggleInput()
    {
        _camera.EnableInput = !_camera.EnableInput;
    }

    [ScriptFunction("setInput", "Enables or disables camera input handling.")]
    public void SetInput(bool enable)
    {
        _camera.EnableInput = enable;
    }
}
