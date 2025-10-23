using Microsoft.Xna.Framework;

namespace SquidVox.Core.Data.Primitives;

public class PositionAndSize
{
    public Vector3 Position { get; }
    public Vector3 Size { get; }


    public PositionAndSize(Vector3 position, Vector3 size)
    {
        Position = position;
        Size = size;
    }

    public PositionAndSize(float x, float y, float z, float sizeX, float sizeY, float sizeZ)
    {
        Position = new Vector3(x, y, z);
        Size = new Vector3(sizeX, sizeY, sizeZ);
    }
}
