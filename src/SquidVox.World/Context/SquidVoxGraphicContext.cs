using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using SquidVox.Core.Data.Graphics;
using TrippyGL;

namespace SquidVox.World.Context;

/// <summary>
/// Provides static access to graphics context resources such as window, GL, and input.
/// </summary>
public class SquidVoxGraphicContext
{
    /// <summary>
    /// Gets or sets the graphics device.
    /// </summary>
    public static GraphicsDevice GraphicsDevice { get; set; }

    /// <summary>
    /// Gets or sets the window.
    /// </summary>
    public static IWindow Window { get; set; }

    /// <summary>
    /// Gets or sets the OpenGL context.
    /// </summary>
    public static GL GL { get; set; }

    /// <summary>
    /// Gets or sets the ImGui controller.
    /// </summary>
    public static ImGuiController ImGuiController { get; set; }

    /// <summary>
    /// Gets or sets the input context.
    /// </summary>
    public static IInputContext InputContext { get; set; }

    /// <summary>
    /// Gets the game time instance.
    /// </summary>
    public static GameTime GameTime { get; } = new ();

    /// <summary>
    /// Disposes of all graphics resources.
    /// </summary>
    public static void Dispose()
    {
        Window.Dispose();
        GraphicsDevice.Dispose();
        GL.Dispose();
        InputContext.Dispose();

    }

}
