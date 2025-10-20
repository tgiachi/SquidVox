using FontStashSharp;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Graphics;
using Serilog;
using SquidVox.Core.Context;
using SquidVox.Core.Enums;
using SquidVox.Core.Interfaces.Services;

namespace SquidVox.World3d.Services;

/// <summary>
/// Implements the asset management service.
/// </summary>
public class AssetManagerService : IAssetManagerService
{
    private ContentManager _contentManager;
    private readonly ILogger _logger = Log.ForContext<AssetManagerService>();
    private readonly Dictionary<string, Texture2D> _textures = new();
    private readonly Dictionary<string, FontSystem> _fontSystems = new();
    private readonly Dictionary<string, DynamicSpriteFont> _loadedFonts = new();
    private readonly Dictionary<string, Effect> _effects = new();

    private readonly Dictionary<string, Texture2DAtlas> _textureAtlases = new();


    private readonly int[] _initialFontSize = [16, 24, 32, 48, 64, 96, 128];

    public void SetContentManager(ContentManager contentManager)
    {
        _contentManager = contentManager;
    }

    /// <summary>
    /// Loads an asset from file.
    /// </summary>
    /// <param name="assetType">The type of asset to load.</param>
    /// <param name="fileName">The file name of the asset.</param>
    /// <param name="name">The name to assign to the asset.</param>
    public void LoadAssetFromFile(AssetType assetType, string fileName, string name)
    {
        switch (assetType)
        {
            case AssetType.Texture:
                LoadTextureFromFile(fileName, name);
                break;
            case AssetType.Font:
                LoadFontFromFile(fileName, name);
                break;
            default:
                _logger.Warning("Asset type {Type} not supported yet", assetType);
                break;
        }
    }

    /// <summary>
    /// Gets a texture by name.
    /// </summary>
    /// <param name="name">The name of the texture.</param>
    /// <returns>The texture if found, otherwise null.</returns>
    public Texture2D GetTexture(string name)
    {
        if (_textures.TryGetValue(name, out var texture))
        {
            return texture;
        }

        _logger.Warning("Texture {Name} not found", name);
        return null;
    }


    /// <summary>
    /// Gets a tile from a texture atlas by index.
    /// </summary>
    /// <param name="atlasName">The name of the texture atlas.</param>
    /// <param name="tileIndex">The index of the tile.</param>
    /// <returns>The texture tile if found, otherwise null.</returns>
    public Texture2DRegion GetTextureAtlasTile(string atlasName, int tileIndex)
    {
        if (_textureAtlases.TryGetValue(atlasName, out var atlas))
        {
            return atlas[tileIndex];

            _logger.Warning("Tile index {Index} out of range for atlas {Atlas}", tileIndex, atlasName);
            return null;
        }

        _logger.Warning("Texture atlas {Name} not found", atlasName);
        return null;
    }

    /// <summary>
    /// Gets a font by name and size.
    /// </summary>
    /// <param name="name">The name of the font.</param>
    /// <param name="size">The size of the font.</param>
    /// <returns>The font if found, otherwise null.</returns>
    public DynamicSpriteFont GetFont(string name, int size = 32)
    {
        if (_loadedFonts.TryGetValue($"{name}_{size}", out var loadedFont))
        {
            return loadedFont;
        }

        if (_fontSystems.TryGetValue(name, out var fontSystem))
        {
            var preCachedFont = fontSystem.GetFont(size);

            _loadedFonts[$"{name}_{size}"] = preCachedFont;
            return preCachedFont;
        }

        _logger.Warning("Font {Name} not found", name);
        return null;
    }

    /// <summary>
    /// Gets an effect by name.
    /// </summary>
    /// <param name="name">The name of the effect.</param>
    /// <returns>The effect if found, otherwise null.</returns>
    public Effect GetEffect(string name)
    {
        if (_effects.TryGetValue(name, out var effect))
        {
            return effect;
        }

        _logger.Warning("Effect {Name} not found", name);
        return null;
    }

    /// <summary>
    /// Loads an effect from content.
    /// </summary>
    /// <param name="name">The name of the effect.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public void LoadEffect(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (_contentManager == null)
        {
            throw new InvalidOperationException("ContentManager not set");
        }

        var effect = _contentManager.Load<Effect>(name);
        _effects[name] = effect;
        _logger.Information("Loaded effect {Name}", name);
    }

    /// <summary>
    /// Loads a font from file.
    /// </summary>
    /// <param name="fileName">The file name of the font.</param>
    /// <param name="name">The name to assign to the font.</param>
    public void LoadFontFromFile(string fileName, string name)
    {
        var fontData = File.ReadAllBytes(fileName);
        LoadFontFromBytes(fontData, name);
        _logger.Information("Loaded font {Name} from {File}", name, fileName);
    }

    /// <summary>
    /// Loads a font from a byte array.
    /// </summary>
    /// <param name="data">The byte array containing the font data.</param>
    /// <param name="name">The name to assign to the font.</param>
    public void LoadFontFromBytes(ReadOnlySpan<byte> data, string name)
    {
        var fontSystem = new FontSystem();
        fontSystem.AddFont(data.ToArray()); // FontSystem.AddFont requires byte[]
        _fontSystems[name] = fontSystem;

        foreach (var size in _initialFontSize)
        {
            _logger.Debug("Preloading {Name} font: {Size} ", name, size);
            _loadedFonts[$"{name}_{size}"] = fontSystem.GetFont(size);
        }

        _logger.Information("Loaded font {Name} from byte array", name);
    }

    /// <summary>
    /// Loads a texture from file.
    /// </summary>
    /// <param name="fileName">The file name of the texture.</param>
    /// <param name="name">The name to assign to the texture.</param>
    public void LoadTextureFromFile(string fileName, string name)
    {
        var texture = Texture2D.FromFile(SquidVoxGraphicContext.GraphicsDevice, fileName);
        _textures[name] = texture;
        _logger.Information("Loaded texture {Name} from {File}", name, fileName);
    }

    /// <summary>
    /// Loads a texture from a byte array.
    /// </summary>
    /// <param name="data">The byte array containing the image data.</param>
    /// <param name="name">The name to assign to the texture.</param>
    public void LoadTextureFromBytes(ReadOnlySpan<byte> data, string name)
    {
        var texture = Texture2D.FromStream(SquidVoxGraphicContext.GraphicsDevice, new MemoryStream(data.ToArray()));
        _textures[name] = texture;
        _logger.Information("Loaded texture {Name} from byte array ({Width}x{Height})", name, texture.Width, texture.Height);
    }


    /// <summary>
    /// Loads a texture atlas from file and splits it into individual tiles.
    /// </summary>
    /// <param name="fileName">The file name of the texture atlas.</param>
    /// <param name="name">The name to assign to the atlas.</param>
    /// <param name="tileWidth">The width of each tile in pixels.</param>
    /// <param name="tileHeight">The height of each tile in pixels.</param>
    /// <param name="spacing">The spacing between tiles in pixels.</param>
    /// <param name="margin">The margin around the atlas in pixels.</param>
    public void LoadTextureAtlasFromFile(
        string fileName, string name, int tileWidth, int tileHeight, int spacing = 0, int margin = 0
    )
    {
        LoadTextureFromFile(name + "_atlas", fileName);

        var texture = GetTexture(name + "_atlas");
        var textureAtlas = Texture2DAtlas.Create(name, texture, tileWidth, tileHeight, int.MaxValue, margin);
        _textureAtlases[name] = textureAtlas;
        _logger.Information(
            "Loaded texture atlas {Name} from {File} with {Count} tiles",
            name,
            fileName,
            textureAtlas.LongCount()
        );
    }

    /// <summary>
    /// Loads a texture atlas from a byte array and splits it into individual tiles.
    /// </summary>
    /// <param name="data">The byte array containing the atlas image data.</param>
    /// <param name="name">The name to assign to the atlas.</param>
    /// <param name="tileWidth">The width of each tile in pixels.</param>
    /// <param name="tileHeight">The height of each tile in pixels.</param>
    /// <param name="spacing">The spacing between tiles in pixels.</param>
    /// <param name="margin">The margin around the atlas in pixels.</param>
    public void LoadTextureAtlasFromBytes(
        ReadOnlySpan<byte> data, string name, int tileWidth, int tileHeight, int spacing = 0, int margin = 0
    )
    {
        LoadTextureFromBytes(data, name + "_atlas");
        var texture = GetTexture(name + "_atlas");
        _textureAtlases[name] = Texture2DAtlas.Create(name, texture, tileWidth, tileHeight, int.MaxValue, margin);
        _logger.Information(
            "Loaded texture atlas {Name} from byte array with {Count} tiles",
            name,
            _textureAtlases[name].LongCount()
        );
    }


    /// <summary>
    /// Disposes of the service resources.
    /// </summary>
    public void Dispose()
    {
        foreach (var texture in _textures.Values)
        {
            texture.Dispose();
        }

        _textures.Clear();

        foreach (var effect in _effects.Values)
        {
            effect.Dispose();
        }

        _effects.Clear();

        _textureAtlases.Clear();

        GC.SuppressFinalize(this);
    }
}
