using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace SquidVox.Voxel.Data;

/// <summary>
/// Represents a chunk entity containing block data and lighting information.
/// </summary>
public class ChunkEntity
{
    /// <summary>
    /// The height of the chunk.
    /// </summary>
    public const int Height = 256;

    /// <summary>
    /// The width of the chunk.
    /// </summary>
    public const int Width = 16;

    /// <summary>
    /// The depth of the chunk.
    /// </summary>
    public const int Depth = 16;

    public const int Size = Width * Height * Depth;

    private readonly int _chunkX;
    private readonly int _chunkY;
    private readonly int _chunkZ;

    /// <summary>
    /// Gets the position of the chunk.
    /// </summary>
    public Vector3 Position => new Vector3(_chunkX, _chunkY, _chunkZ);

    /// <summary>
    /// Gets the center position of the chunk in world coordinates.
    /// </summary>
    public Vector3 Center => new Vector3(
        _chunkX * Width + Width / 2f,
        _chunkY * Height + Height / 2f,
        _chunkZ * Depth + Depth / 2f
    );

    /// <summary>
    /// Gets the world position of the chunk.
    /// </summary>
    public Vector3 WorldPosition => new Vector3(_chunkX * Width, _chunkY * Height, _chunkZ * Depth);

    /// <summary>
    /// Gets the total number of cells in the chunk.
    /// </summary>
    public static int Count => Width * Height * Depth;


    /// <summary>
    /// Calculates the index of a cell within the chunk.
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <param name="z">The z coordinate.</param>
    /// <returns>The index of the cell.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Index(int x, int y, int z) => (x & 0xF) | ((z & 0xF) << 4) | ((y & 0xFF) << 8);

    /// <summary>
    /// Calculates the index of a cell within the chunk from a vector position.
    /// </summary>
    /// <param name="pos">The position vector.</param>
    /// <returns>The index of the cell.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Index(Vector3 pos) => Index((int)pos.X, (int)pos.Y, (int)pos.Z);

    /// <summary>
    /// The array of block cells in the chunk.
    /// </summary>
    private readonly BlockCell[] Cells = new BlockCell[Count];

    /// <summary>
    /// Gets a reference to the block cell at the specified coordinates.
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <param name="z">The z coordinate.</param>
    /// <returns>A reference to the block cell.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref BlockCell At(int x, int y, int z) => ref Cells[Index(x, y, z)];

    /// <summary>
    /// The array of light values for the chunk.
    /// </summary>
    public readonly ushort[] Light = new ushort[Count];

    /// <summary>
    /// Sets the light value in the specified array at the given index.
    /// </summary>
    /// <param name="arr">The light array.</param>
    /// <param name="i">The index.</param>
    /// <param name="r">The red component.</param>
    /// <param name="g">The green component.</param>
    /// <param name="b">The blue component.</param>
    /// <param name="p">The padding component.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetLight(ushort[] arr, int i, byte r, byte g, byte b, byte p)
    {
        arr[i] = (ushort)(((r & 0xF) << 12) | ((g & 0xF) << 8) | ((b & 0xF) << 4) | (p & 0xF));
    }

    /// <summary>
    /// Gets the light components from a light value.
    /// </summary>
    /// <param name="v">The light value.</param>
    /// <param name="r">The red component.</param>
    /// <param name="g">The green component.</param>
    /// <param name="b">The blue component.</param>
    /// <param name="p">The padding component.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetLight(ushort v, out byte r, out byte g, out byte b, out byte p)
    {
        r = (byte)((v >> 12) & 0xF);
        g = (byte)((v >> 8) & 0xF);
        b = (byte)((v >> 4) & 0xF);
        p = (byte)(v & 0xF);
    }

    /// <summary>
    /// Sets the light at the specified position.
    /// </summary>
    /// <param name="pos">The position.</param>
    /// <param name="r">The red component.</param>
    /// <param name="g">The green component.</param>
    /// <param name="b">The blue component.</param>
    /// <param name="p">The padding component.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetLightAt(Vector3 pos, byte r, byte g, byte b, byte p)
    {
        SetLight(Light, Index(pos), r, g, b, p);
    }

    /// <summary>
    /// Gets the light components at the specified position.
    /// </summary>
    /// <param name="pos">The position.</param>
    /// <param name="r">The red component.</param>
    /// <param name="g">The green component.</param>
    /// <param name="b">The blue component.</param>
    /// <param name="p">The padding component.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetLightAt(Vector3 pos, out byte r, out byte g, out byte b, out byte p)
    {
        GetLight(Light[Index(pos)], out r, out g, out b, out p);
    }

    /// <summary>
    /// Sets the light at the specified position from a color.
    /// </summary>
    /// <param name="pos">The position.</param>
    /// <param name="color">The color.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetLightAt(Vector3 pos, Color color)
    {
        SetLight(Light, Index(pos), (byte)(color.R >> 4), (byte)(color.G >> 4), (byte)(color.B >> 4), (byte)(color.A >> 4));
    }

    /// <summary>
    /// Gets or sets the block cell at the specified coordinates.
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <param name="z">The z coordinate.</param>
    /// <returns>The block cell.</returns>
    public ref BlockCell this[int x, int y, int z] => ref At(x, y, z);

    /// <summary>
    /// Initializes a new instance of the ChunkEntity class.
    /// </summary>
    /// <param name="chunkX">The x coordinate of the chunk.</param>
    /// <param name="chunkY">The y coordinate of the chunk.</param>
    /// <param name="chunkZ">The z coordinate of the chunk.</param>
    public ChunkEntity(int chunkX, int chunkY, int chunkZ)
    {
        _chunkX = chunkX;
        _chunkY = chunkY;
        _chunkZ = chunkZ;
    }

    /// <summary>
    /// Initializes a new instance of the ChunkEntity class from a vector position.
    /// </summary>
    /// <param name="chunkPos">The chunk position.</param>
    public ChunkEntity(Vector3 chunkPos)
        : this((int)chunkPos.X, (int)chunkPos.Y, (int)chunkPos.Z)
    {
    }

    /// <summary>
    /// Initializes a new instance of the ChunkEntity class at the origin.
    /// </summary>
    public ChunkEntity()
        : this(0, 0, 0)
    {
    }
}
