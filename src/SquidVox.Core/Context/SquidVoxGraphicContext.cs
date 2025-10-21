using DryIoc;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SquidVox.Core.Context;

/// <summary>
/// Provides static access to graphics context resources such as window, GL, and input.
/// </summary>
public static class SquidVoxGraphicContext
{
    /// <summary>
    /// Gets or sets the IoC container.
    /// </summary>
    public static IContainer Container { get; set; }

    /// <summary>
    /// Gets or sets the clear color for the graphics device.
    /// </summary>
    public static Color ClearColor { get; set; } = Color.CornflowerBlue;

    /// <summary>
    /// Gets or sets the graphics device.
    /// </summary>
    public static GraphicsDevice GraphicsDevice => GraphicsDeviceManager.GraphicsDevice;

    /// <summary>
    /// Gets or sets the graphics device manager.
    /// </summary>
    public static GraphicsDeviceManager GraphicsDeviceManager { get; set; }

    /// <summary>
    /// Gets or sets the game window.
    /// </summary>
    public static GameWindow Window { get; set; }

    /// <summary>
    /// Gets or sets the 1x1 white pixel texture (useful for drawing solid color rectangles).
    /// </summary>
    public static Texture2D WhitePixel { get; set; } = null!;




    /// <summary>
    /// Gets the game time instance.
    /// </summary>
    public static GameTime GameTime { get; } = new();

    /// <summary>
    /// Disposes of all graphics resources.
    /// </summary>
    public static void Dispose()
    {
        WhitePixel?.Dispose();
        GraphicsDevice?.Dispose();
    }

}
