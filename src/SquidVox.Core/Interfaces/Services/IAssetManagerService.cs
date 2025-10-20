using FontStashSharp;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Graphics;
using SquidVox.Core.Enums;

namespace SquidVox.Core.Interfaces.Services;

/// <summary>
/// Defines the contract for asset management services.
/// </summary>
public interface IAssetManagerService : IDisposable
{

    void SetContentManager(ContentManager contentManager);

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
    /// Gets a tile from a texture atlas by index.
    /// </summary>
    /// <param name="atlasName">The name of the texture atlas.</param>
    /// <param name="tileIndex">The index of the tile.</param>
    /// <returns>The texture tile if found, otherwise null.</returns>
    Texture2DRegion GetTextureAtlasTile(string atlasName, int tileIndex);



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
    /// Loads a font from a byte array.
    /// </summary>
    /// <param name="data">The byte array containing the font data.</param>
    /// <param name="name">The name to assign to the font.</param>
    void LoadFontFromBytes(ReadOnlySpan<byte> data, string name);

    /// <summary>
    /// Loads a texture from file.
    /// </summary>
    /// <param name="fileName">The file name of the texture.</param>
    /// <param name="name">The name to assign to the texture.</param>
    void LoadTextureFromFile(string fileName, string name);

    /// <summary>
    /// Loads a texture from a byte array.
    /// </summary>
    /// <param name="data">The byte array containing the image data.</param>
    /// <param name="name">The name to assign to the texture.</param>
    void LoadTextureFromBytes(ReadOnlySpan<byte> data, string name);


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

    /// <summary>
    /// Loads a texture atlas from a byte array and splits it into individual tiles.
    /// </summary>
    /// <param name="data">The byte array containing the atlas image data.</param>
    /// <param name="name">The name to assign to the atlas.</param>
    /// <param name="tileWidth">The width of each tile in pixels.</param>
    /// <param name="tileHeight">The height of each tile in pixels.</param>
    /// <param name="spacing">The spacing between tiles in pixels.</param>
    /// <param name="margin">The margin around the atlas in pixels.</param>
    void LoadTextureAtlasFromBytes(ReadOnlySpan<byte> data, string name, int tileWidth, int tileHeight, int spacing = 0, int margin = 0);

    /// <summary>
    /// Gets an effect by name.
    /// </summary>
    /// <param name="name">The name of the effect.</param>
    /// <returns>The effect if found, otherwise null.</returns>
    Effect GetEffect(string name);

    /// <summary>
    /// Loads an effect from content.
    /// </summary>
    /// <param name="name">The name of the effect.</param>
    void LoadEffect(string name);
}
