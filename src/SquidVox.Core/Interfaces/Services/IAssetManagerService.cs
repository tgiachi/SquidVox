using FontStashSharp;
using SquidVox.Core.Enums;
using TrippyGL;

namespace SquidVox.Core.Interfaces.Services;

/// <summary>
/// Defines the contract for asset management services.
/// </summary>
public interface IAssetManagerService : IDisposable
{
    void LoadAssetFromFile(AssetType assetType, string fileName, string name);

    Texture2D GetTexture(string name);

    DynamicSpriteFont GetFont(string name, int size = 32);
}
