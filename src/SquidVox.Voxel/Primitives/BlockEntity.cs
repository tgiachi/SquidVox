using System;
using Microsoft.Xna.Framework;
using SquidVox.Voxel.Types;

namespace SquidVox.Voxel.Primitives;

/// <summary>
/// Represents a single block instance within a chunk, including its identifier and type.
/// </summary>
public struct BlockEntity
{
    private const int BlockTypeShift = 0;
    private const int WaterLevelShift = 8;
    private const int LightLevelShift = 11;
    private const uint BlockTypeMask = 0xFFu;
    private const uint WaterLevelMask = 0x7u;
    private const uint LightLevelMask = 0xFu;
    private const byte MaxWaterLevel = 7;
    private const byte MaxLightLevel = 15;

    private uint _packedData;
    private uint _packedLightColor;

    /// <summary>
    /// Initializes a new <see cref="BlockEntity"/> with the provided identifier and type.
    /// </summary>
    /// <param name="blockType">The type of block represented by this entity.</param>
    public BlockEntity(BlockType blockType)
    {
        _packedData = 0;
        _packedLightColor = PackLightColor(Vector3.One);
        BlockType = blockType;
        WaterLevel = blockType == BlockType.Water ? MaxWaterLevel : (byte)0;
        LightLevel = 1;
    }

    private static float Clamp01(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        if (value > 1f)
        {
            return 1f;
        }

        return value;
    }

    private static uint PackLightColor(Vector3 color)
    {
        var r = (uint)(Clamp01(color.X) * 255f + 0.5f);
        var g = (uint)(Clamp01(color.Y) * 255f + 0.5f);
        var b = (uint)(Clamp01(color.Z) * 255f + 0.5f);

        return (r & 0xFFu) | ((g & 0xFFu) << 8) | ((b & 0xFFu) << 16);
    }

    private static Vector3 UnpackLightColor(uint packed)
    {
        var r = (packed & 0xFFu) / 255f;
        var g = ((packed >> 8) & 0xFFu) / 255f;
        var b = ((packed >> 16) & 0xFFu) / 255f;

        return new Vector3(r, g, b);
    }

    /// <summary>
    /// Gets or sets the semantic type for the block.
    /// </summary>
    public BlockType BlockType
    {
        readonly get => (BlockType)(_packedData & BlockTypeMask);
        set => _packedData = (_packedData & ~BlockTypeMask) | ((uint)value & BlockTypeMask);
    }

    /// <summary>
    /// Gets or sets the water level (0-7, where 7 is full source block, 0 is no water).
    /// </summary>
    public byte WaterLevel
    {
        readonly get => (byte)((_packedData >> WaterLevelShift) & WaterLevelMask);
        set
        {
            var clamped = Math.Min(value, MaxWaterLevel);
            _packedData = (_packedData & ~(WaterLevelMask << WaterLevelShift)) | ((uint)clamped << WaterLevelShift);
        }
    }

    /// <summary>
    /// Gets or sets the light level (0-15, where 15 is full brightness, 0 is dark).
    /// </summary>
    public byte LightLevel
    {
        readonly get => (byte)((_packedData >> LightLevelShift) & LightLevelMask);
        set
        {
            var clamped = Math.Min(value, MaxLightLevel);
            _packedData = (_packedData & ~(LightLevelMask << LightLevelShift)) | ((uint)clamped << LightLevelShift);
        }
    }

    /// <summary>
    /// Gets or sets the light color (RGB values 0-1).
    /// </summary>
    public Vector3 LightColor
    {
        readonly get => _packedLightColor == 0 ? Vector3.One : UnpackLightColor(_packedLightColor);
        set => _packedLightColor = PackLightColor(value);
    }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string representation of the block entity.</returns>
    public override readonly string ToString() => $"BlockEntity({BlockType})";
}
