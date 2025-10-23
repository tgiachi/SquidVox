using System;
using System.Collections.Generic;
using System.IO;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Graphics;
using Serilog;
using SquidVox.Core.Context;
using SquidVox.Core.Enums;
using SquidVox.Core.Interfaces.Services;
using System.Linq;

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
    private Texture2D _fallbackTexture = null!;
    private readonly int[] _initialFontSize = { 16, 24, 32, 48, 64, 96, 128 };

    public void SetContentManager(ContentManager contentManager)
    {
        _contentManager = contentManager;
        CreateFallbackTexture();
    }

    private void CreateFallbackTexture()
    {
        _fallbackTexture = new Texture2D(SquidVoxEngineContext.GraphicsDevice, 16, 16);
        var colors = new Color[16 * 16];
        for (var y = 0; y < 16; y++)
        {
            for (var x = 0; x < 16; x++)
            {
                var isBlack = (x / 4 + y / 4) % 2 == 0;
                colors[y * 16 + x] = isBlack ? Color.Black : Color.White;
            }
        }

        _fallbackTexture.SetData(colors);
        _logger.Information("Created fallback checkerboard texture");
    }

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

    public Texture2D GetTexture(string name)
    {
        if (_textures.TryGetValue(name, out var texture))
        {
            return texture;
        }

        _logger.Warning("Texture {Name} not found", name);
        return _fallbackTexture;
    }

    public Texture2DRegion GetTextureAtlasTile(string atlasName, int tileIndex)
    {
        if (_textureAtlases.TryGetValue(atlasName, out var atlas))
        {
            var count = (int)atlas.LongCount();
            if ((uint)tileIndex >= (uint)count)
            {
                _logger.Warning("Tile index {Index} out of range for atlas {Atlas}", tileIndex, atlasName);
                return null;
            }

            return atlas[tileIndex];
        }

        _logger.Warning("Texture atlas {Name} not found", atlasName);
        return null;
    }

    public Texture2D? CreateTextureFromRegion(Texture2DRegion? region)
    {
        if (region == null)
        {
            _logger.Warning("Cannot create texture: region is null");
            return null;
        }

        var bounds = region.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            _logger.Warning(
                "Cannot create texture: region has invalid dimensions {Width}x{Height}",
                bounds.Width,
                bounds.Height
            );
            return null;
        }

        var texture = new Texture2D(region.Texture.GraphicsDevice, bounds.Width, bounds.Height);
        var data = new Color[bounds.Width * bounds.Height];
        region.Texture.GetData(0, bounds, data, 0, data.Length);
        texture.SetData(data);
        return texture;
    }

    public Texture2DAtlas? GetTextureAtlas(string atlasName)
    {
        return _textureAtlases.TryGetValue(atlasName, out var atlas) ? atlas : null;
    }

    public DynamicSpriteFont GetFont(string name, int size = 32)
    {
        var cacheKey = $"{name}_{size}";
        if (_loadedFonts.TryGetValue(cacheKey, out var font))
        {
            return font;
        }

        if (_fontSystems.TryGetValue(name, out var fontSystem))
        {
            var newFont = fontSystem.GetFont(size);
            _loadedFonts[cacheKey] = newFont;
            return newFont;
        }

        _logger.Warning("Font {Name} not found", name);
        return null;
    }

    public void LoadFontFromFile(string fileName, string name)
    {
        using var stream = File.OpenRead(fileName);
        LoadFontInternal(stream, name);
    }

    public void LoadFontFromBytes(ReadOnlySpan<byte> data, string name)
    {
        using var stream = new MemoryStream(data.ToArray());
        LoadFontInternal(stream, name);
    }

    private void LoadFontInternal(Stream stream, string name)
    {
        var fontSystem = new FontSystem();
        fontSystem.AddFont(stream);
        foreach (var size in _initialFontSize)
        {
            fontSystem.GetFont(size);
        }

        _fontSystems[name] = fontSystem;
        _logger.Information("Loaded font {Name}", name);
    }

    public void LoadTextureFromFile(string fileName, string name, bool replaceMagentaWithTransparent = false)
    {
        using var stream = File.OpenRead(fileName);
        var texture = Texture2D.FromStream(SquidVoxEngineContext.GraphicsDevice, stream);

        if (replaceMagentaWithTransparent)
        {
            ReplaceMagentaWithTransparent(texture);
        }

        _textures[name] = texture;
        _logger.Information("Loaded texture {Name} from {File}", name, fileName);
    }

    public void LoadTextureFromBytes(ReadOnlySpan<byte> data, string name, bool replaceMagentaWithTransparent = false)
    {
        using var stream = new MemoryStream(data.ToArray());
        var texture = Texture2D.FromStream(SquidVoxEngineContext.GraphicsDevice, stream);

        if (replaceMagentaWithTransparent)
        {
            ReplaceMagentaWithTransparent(texture);
        }

        _textures[name] = texture;
        _logger.Information(
            "Loaded texture {Name} from byte array ({Width}x{Height})",
            name,
            texture.Width,
            texture.Height
        );
    }

    public void LoadTextureAtlasFromFile(
        string fileName,
        string name,
        int tileWidth,
        int tileHeight,
        int spacing = 0,
        int margin = 0,
        bool replaceMagentaWithTransparent = true
    )
    {
        LoadTextureFromFile(fileName, $"{name}_atlas", replaceMagentaWithTransparent);
        var texture = GetTexture($"{name}_atlas");
        _textureAtlases[name] = Texture2DAtlas.Create(name, texture, tileWidth, tileHeight, int.MaxValue, margin);
        _logger.Information(
            "Loaded texture atlas {Name} from {File} with {Count} tiles",
            name,
            fileName,
            _textureAtlases[name].LongCount()
        );
    }

    public void LoadTextureAtlasFromBytes(
        ReadOnlySpan<byte> data,
        string name,
        int tileWidth,
        int tileHeight,
        int spacing = 0,
        int margin = 0,
        bool replaceMagentaWithTransparent = true
    )
    {
        LoadTextureFromBytes(data, $"{name}_atlas", replaceMagentaWithTransparent);
        var texture = GetTexture($"{name}_atlas");
        _textureAtlases[name] = Texture2DAtlas.Create(name, texture, tileWidth, tileHeight, int.MaxValue, margin);
        _logger.Information(
            "Loaded texture atlas {Name} from byte array with {Count} tiles",
            name,
            _textureAtlases[name].LongCount()
        );
    }

    private void ReplaceMagentaWithTransparent(Texture2D texture)
    {
        var pixelCount = texture.Width * texture.Height;
        var data = new Color[pixelCount];
        texture.GetData(data);

        var replaced = 0;
        for (var i = 0; i < data.Length; i++)
        {
            if (data[i].R == 255 && data[i].G == 0 && data[i].B == 255)
            {
                data[i] = Color.Transparent;
                replaced++;
            }
        }

        texture.SetData(data);
        if (replaced > 0)
        {
            _logger.Debug("Replaced {Count} magenta pixels with transparent in texture", replaced);
        }
    }

    public List<string> GetAtlasTextureNames()
    {
        return [.. _textureAtlases.Keys];
    }

    public List<string> GetAtlasNames()
    {
        return GetAtlasTextureNames();
    }

    public Effect GetEffect(string name)
    {
        if (_effects.TryGetValue(name, out var effect))
        {
            return effect;
        }

        var loadedEffect = _contentManager.Load<Effect>(name);
        _effects[name] = loadedEffect;
        return loadedEffect;
    }

    public void LoadEffect(string name)
    {
        var effect = _contentManager.Load<Effect>(name);
        _effects[name] = effect;
        _logger.Information("Loaded effect {Name}", name);
    }

    public void Dispose()
    {
        foreach (var texture in _textures.Values)
        {
            texture.Dispose();
        }

        foreach (var font in _fontSystems.Values)
        {
            font.Dispose();
        }

        foreach (var effect in _effects.Values)
        {
            effect.Dispose();
        }


        _textures.Clear();
        _fontSystems.Clear();
        _loadedFonts.Clear();
        _effects.Clear();
        _textureAtlases.Clear();
        _fallbackTexture?.Dispose();
        GC.SuppressFinalize(this);
    }
}
