using FontStashSharp;
using TrippyGL;

namespace SquidVox.World.Extensions;

public static class ColorExtensions
{
    public static Color4b ToTrippy(this FSColor c)
    {
        return new Color4b(c.R, c.G, c.B, c.A);
    }

}
