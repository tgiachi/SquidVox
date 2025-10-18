using FontStashSharp;
using SquidVox.Core.Enums;
using TrippyGL;
using ShaderType = SquidVox.Core.Enums.ShaderType;

namespace SquidVox.Core.Interfaces.Services;

/// <summary>
/// Defines the contract for asset management services.
/// </summary>
public interface IAssetManagerService : IDisposable
{
    /// <summary>
    /// Loads an asset from file.
    /// </summary>
    /// <param name="assetType">The type of asset to load.</param>
    /// <param name="fileName">The file name of the asset.</param>
    /// <param name="name">The name to assign to the asset.</param>
    void LoadAssetFromFile(AssetType assetType, string fileName, string name);

    /// <summary>
    /// Gets a texture by name.
    /// </summary>
    /// <param name="name">The name of the texture.</param>
    /// <returns>The texture if found, otherwise null.</returns>
    Texture2D GetTexture(string name);

    /// <summary>
    /// Gets a shader program by name.
    /// </summary>
    /// <param name="name">The name of the shader program.</param>
    /// <returns>The shader program if found, otherwise null.</returns>
    ShaderProgram GetShader(string name);

    

    /// <summary>

    /// Gets a tile from a texture atlas by index.

    /// </summary>

    /// <param name="atlasName">The name of the texture atlas.</param>

    /// <param name="tileIndex">The index of the tile.</param>

    /// <returns>The texture tile if found, otherwise null.</returns>

    Texture2D GetTextureAtlasTile(string atlasName, int tileIndex);

    

    /// <summary>
    /// Gets a font by name and size.
    /// </summary>
    /// <param name="name">The name of the font.</param>
    /// <param name="size">The size of the font.</param>
    /// <returns>The font if found, otherwise null.</returns>
    DynamicSpriteFont GetFont(string name, int size = 32);

    /// <summary>
    /// Loads a font from file.
    /// </summary>
    /// <param name="fileName">The file name of the font.</param>
    /// <param name="name">The name to assign to the font.</param>
    void LoadFontFromFile(string fileName, string name);

    /// <summary>
    /// Loads a texture from file.
    /// </summary>
    /// <param name="fileName">The file name of the texture.</param>
    /// <param name="name">The name to assign to the texture.</param>
    void LoadTextureFromFile(string fileName, string name);

    /// <summary>
    /// Loads a shader from file.
    /// </summary>
    /// <param name="filaNames">The file names for the shader.</param>
    /// <param name="shaderType">The type of shader.</param>
    /// <param name="name">The name to assign to the shader.</param>
    void LoadShaderFromFile(string filaNames, ShaderType shaderType, string name);

    /// <summary>
    /// Loads a texture atlas from file and splits it into individual tiles.
    /// </summary>
    /// <param name="fileName">The file name of the texture atlas.</param>
    /// <param name="name">The name to assign to the atlas.</param>
    /// <param name="tileWidth">The width of each tile in pixels.</param>
    /// <param name="tileHeight">The height of each tile in pixels.</param>
    /// <param name="spacing">The spacing between tiles in pixels.</param>
    /// <param name="margin">The margin around the atlas in pixels.</param>
    void LoadTextureAtlasFromFile(string fileName, string name, int tileWidth, int tileHeight, int spacing = 0, int margin = 0);
}
