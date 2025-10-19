using System.Drawing;
using System.Numerics;
using Microsoft.Xna.Framework.Graphics;

namespace SquidVox.Core;

/// <summary>
/// Provides extension methods for color and graphics conversions.
/// </summary>
public static class ColorExtensions
{
    /// <summary>
    /// Converts a Point to a Vector2.
    /// </summary>
    /// <param name="p">The point to convert.</param>
    /// <returns>A Vector2 representation of the point.</returns>
    public static Vector2 ToSystemNumeric(Point p)
    {
        return new Vector2(p.X, p.Y);
    }

    /// <summary>
    /// Converts a Viewport to a Rectangle.
    /// </summary>
    /// <param name="r">The viewport to convert.</param>
    /// <returns>A Rectangle representation of the viewport.</returns>
    public static Rectangle ToSystemDrawing(this Viewport r)
    {
        return new Rectangle(r.X, r.Y, (int)r.Width, (int)r.Height);
    }


}
