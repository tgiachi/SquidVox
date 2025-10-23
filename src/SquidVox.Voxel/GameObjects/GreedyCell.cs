using Microsoft.Xna.Framework;
using SquidVox.Voxel.Types;

namespace SquidVox.Voxel.GameObjects;

/// <summary>
/// Represents a cell in the greedy meshing algorithm.
/// </summary>
internal readonly record struct GreedyCell(
    BlockType BlockType,
    Color Color,
    Vector2 UvMin,
    Vector2 UvMax,
    float Height);