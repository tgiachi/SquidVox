using SquidVox.Core.Attributes.Scripts;
using SquidVox.Core.Data.Directories;
using SquidVox.Core.Enums;
using SquidVox.Core.Interfaces.Services;

namespace SquidVox.World3d.Modules;

/// <summary>
/// Represents a module for asset management in Lua scripting.
/// </summary>
[ScriptModule("assets", "Provides asset management functionalities.")]
public class AssetManagerModule
{
    private readonly IAssetManagerService _assetManagerService;


    private readonly DirectoriesConfig _directoriesConfig;

    /// <summary>
    /// Initializes a new instance of the AssetManagerModule class.
    /// </summary>
    /// <param name="assetManagerService">The asset manager service.</param>
    public AssetManagerModule(IAssetManagerService assetManagerService, DirectoriesConfig directoriesConfig)
    {
        _assetManagerService = assetManagerService;
        _directoriesConfig = directoriesConfig;
    }

    /// <summary>
    /// Loads a font from a file.
    /// </summary>
    /// <param name="name">The name to assign to the font.</param>
    /// <param name="filename">The file name of the font.</param>
    [ScriptFunction("load_font", "Loads a font from a file.")]
    public void LoadFont(string name, string filename)
    {
        _assetManagerService.LoadFontFromFile(Path.Combine(_directoriesConfig[DirectoryType.Assets], filename), name);
    }

    /// <summary>
    /// Loads a texture from a file.
    /// </summary>
    /// <param name="name">The name to assign to the texture.</param>
    /// <param name="filename">The file name of the texture.</param>
    [ScriptFunction("load_texture", "Loads a texture from a file.")]
    public void LoadTexture(string filename, string name)
    {
        _assetManagerService.LoadTextureFromFile(Path.Combine(_directoriesConfig[DirectoryType.Assets], filename), name);
    }


    [ScriptFunction("load_atlas", "Loads a texture atlas from a file.")]
    public void LoadAtlas(string filename, string name, int tileWidth, int tileHeight, int spacing = 0, int margin = 0)
    {
        _assetManagerService.LoadTextureAtlasFromFile(
            Path.Combine(_directoriesConfig[DirectoryType.Assets], filename),
            name,
            tileWidth,
            tileHeight,
            spacing,
            margin
        );
    }
}
