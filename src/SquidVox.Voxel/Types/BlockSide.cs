namespace SquidVox.Voxel.Types;

public enum BlockSide : byte
{
    Top,
    Bottom,
    North,
    South,
    East,
    West
}

public static class BlockSideExtensions
{

    private static readonly BlockSide[] _allSides =
    [
        BlockSide.Top,
        BlockSide.Bottom,
        BlockSide.North,
        BlockSide.South,
        BlockSide.East,
        BlockSide.West
    ];

    public static readonly Dictionary<BlockSide, (int X, int Y, int Z)> NeighborOffsets = new()
    {
        { BlockSide.Top, (0, 1, 0) },
        { BlockSide.Bottom, (0, -1, 0) },
        { BlockSide.North, (0, 0, -1) },
        { BlockSide.South, (0, 0, 1) },
        { BlockSide.East, (1, 0, 0) },
        { BlockSide.West, (-1, 0, 0) }
    };

    public static BlockSide[] AllSides()
    {
        return _allSides;
    }
}
