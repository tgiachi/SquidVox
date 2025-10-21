using SquidVox.Core.Attributes.Scripts;
using SquidVox.Core.Interfaces.Services;

namespace SquidVox.World3d.Modules;

[ScriptModule("assets", "Provides asset management functionalities.")]
public class AssetManagerModule
{
    private readonly IAssetManagerService _assetManagerService;

    public AssetManagerModule(IAssetManagerService assetManagerService)
    {
        _assetManagerService = assetManagerService;
    }

    [ScriptFunction("load_font", "Loads a font from a file.")]
    public void LoadFont(string name, string filename)
    {
        _assetManagerService.LoadFontFromFile(filename, name);
    }

    [ScriptFunction("load_texture", "Loads a texture from a file.")]
    public void LoadTexture(string name, string filename)
    {
        _assetManagerService.LoadTextureFromFile(filename, name);
    }
}
