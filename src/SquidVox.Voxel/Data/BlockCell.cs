using System.Runtime.InteropServices;
using SquidVox.Voxel.Types;

namespace SquidVox.Voxel.Data;

/// <summary>
/// Represents a block cell containing type and metadata information.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct BlockCell
{
    /// <summary>
    /// Gets or sets the block type.
    /// </summary>
    public BlockType Type;

    /// <summary>
    /// Gets or sets the block metadata.
    /// </summary>
    public BlockMetadata Metadata;

    /// <summary>
    /// Initializes a new instance of the BlockCell struct.
    /// </summary>
    /// <param name="type">The block type.</param>
    /// <param name="metadata">The block metadata.</param>
    public BlockCell(BlockType type, BlockMetadata metadata = default)
    {
        Type = type;
        Metadata = metadata;
    }

    /// <summary>
    /// Initializes a new instance of the BlockCell struct with metadata from a byte.
    /// </summary>
    /// <param name="type">The block type.</param>
    /// <param name="metadata">The metadata value as a byte.</param>
    public BlockCell(BlockType type, byte metadata)
        : this(type, new BlockMetadata(metadata))
    {
    }

}
