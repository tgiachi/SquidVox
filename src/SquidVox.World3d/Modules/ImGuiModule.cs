using SquidVox.Core.Attributes.Scripts;
using SquidVox.Core.Collections;
using SquidVox.World3d.Rendering;
using SquidVox.World3d.Scripts;

namespace SquidVox.World3d.Modules;

[ScriptModule("imgui", "ImGui Module for integrating ImGui functionality.")]
public class ImGuiModule
{
    private readonly RenderLayerCollection _renderLayerCollection;

    public ImGuiModule(RenderLayerCollection renderLayerCollection)
    {
        _renderLayerCollection = renderLayerCollection;
    }

    [ScriptFunction("show_demo_window", "Shows the ImGui demo window.")]
    public void ShowDemoWindow()
    {
        _renderLayerCollection.GetLayer<ImGuiRenderLayer>().ShowDemoWindow = true;
    }

    [ScriptFunction("hide_demo_window", "Hides the ImGui demo window.")]
    public void HideDemoWindow()
    {
        _renderLayerCollection.GetLayer<ImGuiRenderLayer>().ShowDemoWindow = false;
    }

    [ScriptFunction("create_debugger_obj", "Creates a new ImGui debugger window with the specified title and callback.")]
    public LuaImGuiDebuggerObject CreateDebugger(string windowTitle, Action callBack)
    {
        return new LuaImGuiDebuggerObject(windowTitle, callBack);
    }

    [ScriptFunction("add_debugger", "Adds an ImGui debugger window to the render layer.")]
    public void AddDebugger(LuaImGuiDebuggerObject debugger)
    {
        _renderLayerCollection.GetLayer<ImGuiRenderLayer>().AddDebugger(debugger);
    }
}
