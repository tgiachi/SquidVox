using DryIoc;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SquidVox.World3d.ImGUI;

namespace SquidVox.World3d.Context;

/// <summary>
/// Provides static access to graphics context resources such as window, GL, and input.
/// </summary>
public static class SquidVoxGraphicContext
{
    public static IContainer Container { get; set; }

    public static Color ClearColor { get; set; } = Color.CornflowerBlue;

    /// <summary>
    /// Gets or sets the graphics device.
    /// </summary>
    public static GraphicsDevice GraphicsDevice { get; set; }

    /// <summary>
    /// Gets or sets the 1x1 white pixel texture (useful for drawing solid color rectangles).
    /// </summary>
    public static Texture2D WhitePixel { get; set; }



    public static ImGuiRenderer ImGuiRenderer { get; set; }

    /// <summary>
    /// Gets the game time instance.
    /// </summary>
    public static GameTime GameTime { get; } = new ();

    /// <summary>
    /// Disposes of all graphics resources.
    /// </summary>
    public static void Dispose()
    {
        WhitePixel?.Dispose();
        GraphicsDevice?.Dispose();
    }

}
