using FontStashSharp;
using TrippyGL;

namespace SquidVox.World.Extensions;

/// <summary>
/// Provides extension methods for color conversions in the world context.
/// </summary>
public static class ColorExtensions
{
    /// <summary>
    /// Converts an FSColor to a Color4b.
    /// </summary>
    /// <param name="c">The FSColor to convert.</param>
    /// <returns>A Color4b representation of the color.</returns>
    public static Color4b ToTrippy(this FSColor c)
    {
        return new Color4b(c.R, c.G, c.B, c.A);
    }

}
