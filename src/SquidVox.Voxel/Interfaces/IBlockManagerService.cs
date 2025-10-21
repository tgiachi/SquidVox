using SquidVox.Voxel.Data.Entities;
using SquidVox.Voxel.Types;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Graphics;

namespace SquidVox.Voxel.Interfaces;

/// <summary>
/// Defines the contract for block management services.
/// </summary>
public interface IBlockManagerService
{
    /// <summary>
    /// Adds a block definition.
    /// </summary>
    /// <param name="blockDefinitionData">The block definition data.</param>
    void AddBlockDefinition(BlockDefinitionData blockDefinitionData);

    /// <summary>
    /// Gets the texture region for a specific block side.
    /// </summary>
    /// <param name="blockType">The type of the block.</param>
    /// <param name="sideType">The side of the block.</param>
    /// <returns>The texture region for the block side, or null if not found.</returns>
    Texture2DRegion? GetBlockSide(BlockType blockType, BlockSide sideType);

    /// <summary>
    /// Gets the raw block definition metadata for the specified block type.
    /// </summary>
    /// <param name="blockType">The type of block requested.</param>
    /// <returns>The stored block definition, or null when unknown.</returns>
    BlockDefinitionData? GetBlockDefinition(BlockType blockType);

    /// <summary>
    /// Determines whether the block type should be treated as transparent when rendering.
    /// </summary>
    /// <param name="blockType">The type of block requested.</param>
    /// <returns>True when the block is transparent or no definition is available.</returns>
    bool IsTransparent(BlockType blockType);
}
