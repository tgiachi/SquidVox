using System.IO;
using FontStashSharp;
using Serilog;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SquidVox.Core.Enums;
using SquidVox.Core.Interfaces.Services;
using SquidVox.World.Context;
using TrippyGL;
using TrippyGL.ImageSharp;
using ShaderType = SquidVox.Core.Enums.ShaderType;

namespace SquidVox.World.Services;

/// <summary>
/// Implements the asset management service.
/// </summary>
public class AssetManagerService : IAssetManagerService
{
    private readonly ILogger _logger = Log.ForContext<AssetManagerService>();
    private readonly Dictionary<string, Texture2D> _textures = new();
    private readonly Dictionary<string, FontSystem> _fontSystems = new();
    private readonly Dictionary<string, DynamicSpriteFont> _loadedFonts = new();

    private readonly Dictionary<string, ShaderProgram> _shaderPrograms = new();
    private readonly Dictionary<string, List<Texture2D>> _textureAtlases = new();


    private readonly int[] _initialFontSize = [16, 24, 32, 48, 64, 96, 128];

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
    /// Gets a shader program by name.
    /// </summary>
    /// <param name="name">The name of the shader program.</param>
    /// <returns>The shader program if found, otherwise null.</returns>
    public ShaderProgram GetShader(string name)
    {
        if (_shaderPrograms.TryGetValue(name, out var shaderProgram))
        {
            return shaderProgram;
        }

        _logger.Warning("Shader {Name} not found", name);
        return null;
    }

    /// <summary>
    /// Gets a tile from a texture atlas by index.
    /// </summary>
    /// <param name="atlasName">The name of the texture atlas.</param>
    /// <param name="tileIndex">The index of the tile.</param>
    /// <returns>The texture tile if found, otherwise null.</returns>
    public Texture2D GetTextureAtlasTile(string atlasName, int tileIndex)
    {
        if (_textureAtlases.TryGetValue(atlasName, out var atlas))
        {
            if (tileIndex >= 0 && tileIndex < atlas.Count)
            {
                return atlas[tileIndex];
            }

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
        var texture = Texture2DExtensions.FromFile(SquidVoxGraphicContext.GraphicsDevice, fileName, true);
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
        using var image = Image.Load<Rgba32>(data);
        var texture = Texture2DExtensions.FromImage(SquidVoxGraphicContext.GraphicsDevice, image, true);
        _textures[name] = texture;
        _logger.Information("Loaded texture {Name} from byte array ({Width}x{Height})", name, image.Width, image.Height);
    }

    /// <summary>
    /// Loads a shader from file.
    /// </summary>
    /// <param name="filaNames">The file names for the shader.</param>
    /// <param name="shaderType">The type of shader.</param>
    /// <param name="name">The name to assign to the shader.</param>
    public void LoadShaderFromFile(string filaNames, ShaderType shaderType, string name)
    {
        var vertexFileName = filaNames + ".vert";
        var fragmentFileName = filaNames + ".frag";

        ShaderProgram shaderProgram = null;

        if (shaderType == ShaderType.VertexColor)
        {
            shaderProgram = ShaderProgram.FromFiles<VertexColor>(
                SquidVoxGraphicContext.GraphicsDevice,
                vertexFileName,
                fragmentFileName
            );
        }

        if (shaderType == ShaderType.VertexColorTexture)
        {
            shaderProgram = ShaderProgram.FromFiles<VertexColorTexture>(
                SquidVoxGraphicContext.GraphicsDevice,
                vertexFileName,
                fragmentFileName
            );
        }

        _shaderPrograms[name] = shaderProgram;
        _logger.Information(
            "Loaded shader program {Name} from {Vertex} and {Fragment} of type {Type}",
            name,
            vertexFileName,
            fragmentFileName,
            shaderType
        );
    }

    /// <summary>
    /// Loads a shader from byte arrays.
    /// </summary>
    /// <param name="vertexShaderSource">The vertex shader source code as byte array.</param>
    /// <param name="fragmentShaderSource">The fragment shader source code as byte array.</param>
    /// <param name="shaderType">The type of shader.</param>
    /// <param name="name">The name to assign to the shader.</param>
    public void LoadShaderFromBytes(ReadOnlySpan<byte> vertexShaderSource, ReadOnlySpan<byte> fragmentShaderSource, ShaderType shaderType, string name)
    {
        var vertexSource = System.Text.Encoding.UTF8.GetString(vertexShaderSource);
        var fragmentSource = System.Text.Encoding.UTF8.GetString(fragmentShaderSource);

        ShaderProgram shaderProgram = null;

        if (shaderType == ShaderType.VertexColor)
        {
            shaderProgram = ShaderProgram.FromCode<VertexColor>(
                SquidVoxGraphicContext.GraphicsDevice,
                vertexSource,
                fragmentSource
            );
        }

        if (shaderType == ShaderType.VertexColorTexture)
        {
            shaderProgram = ShaderProgram.FromCode<VertexColorTexture>(
                SquidVoxGraphicContext.GraphicsDevice,
                vertexSource,
                fragmentSource
            );
        }

        _shaderPrograms[name] = shaderProgram;
        _logger.Information("Loaded shader program {Name} from byte array of type {Type}", name, shaderType);
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
    public void LoadTextureAtlasFromFile(string fileName, string name, int tileWidth, int tileHeight, int spacing = 0, int margin = 0)
    {
        using var image = Image.Load<Rgba32>(fileName);
        var tiles = ProcessTextureAtlas(image, name, tileWidth, tileHeight, spacing, margin);
        _textureAtlases[name] = tiles;
        _logger.Information("Loaded texture atlas {Name} from {File} with {Count} tiles", name, fileName, tiles.Count);
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
    public void LoadTextureAtlasFromBytes(ReadOnlySpan<byte> data, string name, int tileWidth, int tileHeight, int spacing = 0, int margin = 0)
    {
        using var image = Image.Load<Rgba32>(data);
        var tiles = ProcessTextureAtlas(image, name, tileWidth, tileHeight, spacing, margin);
        _textureAtlases[name] = tiles;
        _logger.Information("Loaded texture atlas {Name} from byte array with {Count} tiles", name, tiles.Count);
    }

    /// <summary>
    /// Processes a texture atlas image and splits it into individual tiles.
    /// </summary>
    private List<Texture2D> ProcessTextureAtlas(Image<Rgba32> image, string name, int tileWidth, int tileHeight, int spacing, int margin)
    {
        var tilesX = (image.Width - margin * 2 + spacing) / (tileWidth + spacing);
        var tilesY = (image.Height - margin * 2 + spacing) / (tileHeight + spacing);

        var tiles = new List<Texture2D>();

        for (int y = 0; y < tilesY; y++)
        {
            for (int x = 0; x < tilesX; x++)
            {
                var startX = margin + x * (tileWidth + spacing);
                var startY = margin + y * (tileHeight + spacing);

                // Create a new texture for this tile
                var tileTexture = new Texture2D(SquidVoxGraphicContext.GraphicsDevice, (uint)tileWidth, (uint)tileHeight);

                // Extract pixel data for this tile
                var pixelData = new byte[tileWidth * tileHeight * 4];

                for (int ty = 0; ty < tileHeight; ty++)
                {
                    for (int tx = 0; tx < tileWidth; tx++)
                    {
                        var pixel = image[startX + tx, startY + ty];
                        var index = (ty * tileWidth + tx) * 4;
                        pixelData[index] = pixel.R;
                        pixelData[index + 1] = pixel.G;
                        pixelData[index + 2] = pixel.B;
                        pixelData[index + 3] = pixel.A;
                    }
                }

                // Upload pixel data to the texture
                tileTexture.SetData<byte>(pixelData, 0, 0, (uint)tileWidth, (uint)tileHeight, PixelFormat.Rgba);

                tiles.Add(tileTexture);
            }
        }

        _logger.Debug("Processed texture atlas {Name} into {Count} tiles ({TilesX}x{TilesY})",
            name, tiles.Count, tilesX, tilesY);

        return tiles;
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

        foreach (var atlas in _textureAtlases.Values)
        {
            foreach (var tile in atlas)
            {
                tile.Dispose();
            }
        }

        _textureAtlases.Clear();

        GC.SuppressFinalize(this);
    }
}
