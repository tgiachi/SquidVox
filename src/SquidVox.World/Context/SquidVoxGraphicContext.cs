using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using SquidVox.Core.Data.Graphics;
using TrippyGL;

namespace SquidVox.World.Context;

public class SquidVoxGraphicContext
{
    public static GraphicsDevice GraphicsDevice { get; set; }

    public static IWindow Window { get; set; }

    public static GL GL { get; set; }

    public static ImGuiController ImGuiController { get; set; }

    public static IInputContext InputContext { get; set; }

    public static GameTime GameTime { get; } = new ();

    public static void Dispose()
    {
        GraphicsDevice.Dispose();
        GL.Dispose();
        InputContext.Dispose();

    }

}
