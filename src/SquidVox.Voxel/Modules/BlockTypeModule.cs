using SquidVox.Core.Attributes.Scripts;
using SquidVox.Voxel.Types;

namespace SquidVox.Voxel.Modules;

/// <summary>
/// Module that exposes BlockType enum values to Lua scripts.
/// </summary>
[ScriptModule("block_type", "Provides access to block type constants.")]
public class BlockTypeModule
{
    public BlockType Air => BlockType.Air;
    public BlockType Dirt => BlockType.Dirt;
    public BlockType Grass => BlockType.Grass;
    public BlockType Stone => BlockType.Stone;
    public BlockType Water => BlockType.Water;
    public BlockType Sand => BlockType.Sand;
    public BlockType Wood => BlockType.Wood;
    public BlockType Leaves => BlockType.Leaves;
    public BlockType Glass => BlockType.Glass;
    public BlockType Brick => BlockType.Brick;
    public BlockType Bedrock => BlockType.Bedrock;
    public BlockType Snow => BlockType.Snow;
    public BlockType Lava => BlockType.Lava;
    public BlockType Flower => BlockType.Flower;
    public BlockType TallGrass => BlockType.TallGrass;
    public BlockType Pumpkin => BlockType.Pumpkin;
}
