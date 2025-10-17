using System.IO;
using FontStashSharp;
using Serilog;
using SquidVox.Core.Enums;
using SquidVox.Core.Interfaces.Services;
using SquidVox.World.Context;
using TrippyGL;
using TrippyGL.ImageSharp;

namespace SquidVox.World.Services;

/// <summary>
/// Implements the asset management service.
/// </summary>
public class AssetManagerService : IAssetManagerService
{
    private readonly ILogger _logger = Log.ForContext<AssetManagerService>();
    private readonly Dictionary<string, Texture2D> _textures = new();
    private readonly Dictionary<string, FontSystem> _fontSystems = new();

    public void LoadAssetFromFile(AssetType assetType, string fileName, string name)
    {
        switch (assetType)
        {
            case AssetType.Texture:
                var texture = Texture2DExtensions.FromFile(SquidVoxGraphicContext.GraphicsDevice, fileName, true);
                _textures[name] = texture;
                _logger.Information("Loaded texture {Name} from {File}", name, fileName);
                break;
            case AssetType.Font:
                var fontSystem = new FontSystem();
                fontSystem.AddFont(File.ReadAllBytes(fileName));
                _fontSystems[name] = fontSystem;
                _logger.Information("Loaded font {Name} from {File}", name, fileName);
                break;
            default:
                _logger.Warning("Asset type {Type} not supported yet", assetType);
                break;
        }
    }

    public Texture2D GetTexture(string name)
    {
        if (_textures.TryGetValue(name, out var texture))
        {
            return texture;
        }
        _logger.Warning("Texture {Name} not found", name);
        return null;
    }

    public DynamicSpriteFont GetFont(string name, int size = 32)
    {
        if (_fontSystems.TryGetValue(name, out var fontSystem))
        {
            return fontSystem.GetFont(size);
        }
        _logger.Warning("Font {Name} not found", name);
        return null;
    }

    public void Dispose()
    {
        foreach (var texture in _textures.Values)
        {
            texture.Dispose();
        }

        _textures.Clear();

        GC.SuppressFinalize(this);
    }
}
