using System.Runtime.InteropServices;

namespace SquidVox.Voxel.Data;

/// <summary>
/// Represents metadata for a block, containing additional information beyond the block type.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct BlockMetadata
{
    private byte _value;

    /// <summary>
    /// Initializes a new instance of the BlockMetadata struct with the specified value.
    /// </summary>
    /// <param name="value">The metadata value.</param>
    public BlockMetadata(byte value)
    {
        _value = value;
    }

    /// <summary>
    /// Initializes a new instance of the BlockMetadata struct by copying from another instance.
    /// </summary>
    /// <param name="other">The BlockMetadata instance to copy.</param>
    public BlockMetadata(BlockMetadata other)
    {
        _value = other._value;
    }

    /// <summary>
    /// Gets or sets the raw metadata value.
    /// </summary>
    public byte Value
    {
        get => _value;
        set => _value = value;
    }

    /// <summary>
    /// Gets a value indicating whether the metadata is empty (default).
    /// </summary>
    public bool IsEmpty => _value == 0;

    /// <summary>
    /// Sets a specific bit in the metadata.
    /// </summary>
    /// <param name="bit">The bit position to set (0-7).</param>
    public void SetBit(int bit)
    {
        if (bit < 0 || bit > 7) return;
        _value |= (byte)(1 << bit);
    }

    /// <summary>
    /// Clears a specific bit in the metadata.
    /// </summary>
    /// <param name="bit">The bit position to clear (0-7).</param>
    public void ClearBit(int bit)
    {
        if (bit < 0 || bit > 7)
        {
            return;
        }

        _value &= (byte)~(1 << bit);
    }

    /// <summary>
    /// Gets the value of a specific bit in the metadata.
    /// </summary>
    /// <param name="bit">The bit position to check (0-7).</param>
    /// <returns>True if the bit is set, otherwise false.</returns>
    public bool GetBit(int bit)
    {
        if (bit < 0 || bit > 7) return false;
        return (_value & (1 << bit)) != 0;
    }
}
