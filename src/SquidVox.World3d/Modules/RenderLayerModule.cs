using SquidVox.Core.Attributes.Scripts;
using SquidVox.Core.Collections;
using SquidVox.World3d.Rendering;

namespace SquidVox.World3d.Modules;

[ScriptModule("global_render_layer", "Global Render Layer Module")]
public class RenderLayerModule
{
    private readonly RenderLayerCollection _renderLayerCollection;

    public RenderLayerModule(RenderLayerCollection renderLayerCollection)
    {
        _renderLayerCollection = renderLayerCollection;
    }

    [ScriptFunction("get_2d_render_layer", "Gets the 2D render layer.")]
    public GameObjectRenderLayer Get2dRenderLayer()
    {
        return _renderLayerCollection.GetLayer<GameObjectRenderLayer>();
    }
}
