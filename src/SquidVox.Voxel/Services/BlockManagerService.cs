using Microsoft.Xna.Framework.Graphics;
using System.Text.RegularExpressions;
using Serilog;
using SquidVox.Core.Interfaces.Services;
using SquidVox.Voxel.Data.Entities;
using SquidVox.Voxel.Interfaces;
using SquidVox.Voxel.Types;

namespace SquidVox.Voxel.Services;

public partial class BlockManagerService : IBlockManagerService
{
    [GeneratedRegex(@"^([^#]+)#(\d+)(?:-(\d+))?$")]
    private static partial Regex TextureAtlasRegEx();

    private readonly ILogger _logger = Log.ForContext<BlockManagerService>();
    private readonly IAssetManagerService _assetManagerService;

    private readonly Dictionary<BlockType, List<BlockSideEntity>> _blockSideEntities = new();
    private readonly Dictionary<BlockType, BlockDefinitionData> _blockDefinitions = new();


    public BlockManagerService(IAssetManagerService assetManagerService)
    {
        _assetManagerService = assetManagerService;
    }

    public void AddBlockDefinition(string atlasName, BlockDefinitionData blockDefinitionData)
    {
        _logger.Information(
            "Adding block definition: {BlockType} faces: {FacesCount}",
            blockDefinitionData.BlockType,
            blockDefinitionData.Sides.Count
        );

        _blockSideEntities[blockDefinitionData.BlockType] = [];
        _blockDefinitions[blockDefinitionData.BlockType] = blockDefinitionData;

        foreach (var side in blockDefinitionData.Sides)
        {
            var match = TextureAtlasRegEx().Match(side.Value);
            if (match.Success)
            {
                // Format: atlas_name#tile_index or atlas_name#start-end
                var atlasNameFromSide = match.Groups[1].Value;
                var startIndex = int.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);

                if (match.Groups[3].Success)
                {
                    // Range format: store range info for random selection
                    var endIndex = int.Parse(match.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture);
                    _blockSideEntities[blockDefinitionData.BlockType].Add(new BlockSideEntity(side.Key, null, atlasNameFromSide, startIndex, endIndex));
                }
                else
                {
                    // Single index format: get texture immediately
                    var region = _assetManagerService.GetTextureAtlasTile(atlasNameFromSide, startIndex);
                    _blockSideEntities[blockDefinitionData.BlockType].Add(new BlockSideEntity(side.Key, region.Texture));
                }
            }
            else
            {
                // Direct texture name
                var texture = _assetManagerService.GetTexture(side.Value);
                _blockSideEntities[blockDefinitionData.BlockType].Add(new BlockSideEntity(side.Key, texture));
            }
        }
    }

    public Texture2D? GetBlockSide(BlockType blockType, BlockSide sideType)
    {
        if (_blockSideEntities.TryGetValue(blockType, out var sides))
        {
            var side = sides.Find(s => s.Side == sideType);
            if (side == null)
            {
                return null;
            }

            // If texture is already resolved, return it
            if (side.Texture != null)
            {
                return side.Texture;
            }

            // If it's a range, select random texture from atlas
            if (side.AtlasName != null && side.StartIndex.HasValue && side.EndIndex.HasValue)
            {
                var tileIndex = Random.Shared.Next(side.StartIndex.Value, side.EndIndex.Value + 1);
                var region = _assetManagerService.GetTextureAtlasTile(side.AtlasName, tileIndex);
                return region.Texture;
            }
        }

        _logger.Warning("Block type {BlockType} not found", blockType);
        return null;
    }

    /// <summary>
    /// Retrieves the raw block definition metadata for the specified block type.
    /// </summary>
    /// <param name="blockType">Type of block requested.</param>
    /// <returns>The stored block definition, or null when unknown.</returns>
    public BlockDefinitionData? GetBlockDefinition(BlockType blockType)
    {
        if (_blockDefinitions.TryGetValue(blockType, out var definition))
        {
            return definition;
        }

        _logger.Warning("Block definition {BlockType} not found", blockType);
        return null;
    }

    /// <summary>
    /// Determines whether the block type should be treated as transparent when rendering.
    /// </summary>
    /// <param name="blockType">Type of block requested.</param>
    /// <returns>True when the block is transparent or no definition is available.</returns>
    public bool IsTransparent(BlockType blockType)
    {
        return !_blockDefinitions.TryGetValue(blockType, out var definition) || definition.IsTransparent;
        // Default to transparent for unknown block types to prevent rendering artifacts.
    }
}

public record BlockSideEntity(BlockSide Side, Texture2D? Texture, string? AtlasName = null, int? StartIndex = null, int? EndIndex = null);
