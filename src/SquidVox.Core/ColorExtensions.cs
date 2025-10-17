using System.Drawing;
using System.Numerics;
using TrippyGL;

namespace SquidVox.Core;

public static class ColorExtensions
{
    public static Vector2 ToSystemNumeric(Point p)
    {
        return new Vector2(p.X, p.Y);
    }

    public static Rectangle ToSystemDrawing(this Viewport r)
    {
        return new Rectangle(r.X, r.Y, (int)r.Width, (int)r.Height);
    }

    public static Viewport ToTrippy(this Rectangle r)
    {
        return new Viewport(r);
    }
}
