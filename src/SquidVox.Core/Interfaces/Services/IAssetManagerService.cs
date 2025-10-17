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

    ShaderProgram GetShader(string name);


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

    void LoadTextureAtlasFromFile(string fileName, string name, int tileWidth, int tileHeight, int spacing = 0, int margin = 0);
}
