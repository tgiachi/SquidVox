using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using SquidVox.World.Context;
using TrippyGL;

namespace SquidVox.World;

public class SquidVoxWorld : IDisposable
{
    public void Run()
    {
        WindowOptions windowOpts = WindowOptions.Default;
        windowOpts.Title = "SquidVox World";
        windowOpts.API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.Debug, new APIVersion(3, 3));

        using IWindow myWindow = Window.Create(windowOpts);
        myWindow.Load += Window_Load;
        myWindow.Render += Window_Render;
        myWindow.FramebufferResize += Window_FramebufferResize;
        myWindow.Closing += Window_Closing;

        SquidVoxGraphicContext.Window = myWindow;

        myWindow.Run();
    }

    private static void Window_Load()
    {
        SquidVoxGraphicContext.GL = SquidVoxGraphicContext.Window.CreateOpenGL();
        SquidVoxGraphicContext.GraphicsDevice = new GraphicsDevice(SquidVoxGraphicContext.GL);
        SquidVoxGraphicContext.InputContext = SquidVoxGraphicContext.Window.CreateInput();
        SquidVoxGraphicContext.ImGuiController = new ImGuiController(
            SquidVoxGraphicContext.GL,
            SquidVoxGraphicContext.Window,
            SquidVoxGraphicContext.InputContext
        );

        Window_FramebufferResize(SquidVoxGraphicContext.Window.FramebufferSize);
    }

    private static void Window_Render(double delta)
    {
        SquidVoxGraphicContext.GameTime.Update(delta);

        SquidVoxGraphicContext.GraphicsDevice.ClearColor = Color4b.CornflowerBlue;

        SquidVoxGraphicContext.GraphicsDevice.Clear(ClearBuffers.Color);

        SquidVoxGraphicContext.ImGuiController.Update((float)delta);

        ImGuiNET.ImGui.ShowDemoWindow();
        SquidVoxGraphicContext.ImGuiController.Render();
    }

    private static void Window_FramebufferResize(Vector2D<int> size)
    {
        SquidVoxGraphicContext.GraphicsDevice.SetViewport(0, 0, (uint)size.X, (uint)size.Y);

        // Resize code here
    }

    private static void Window_Closing()
    {
        // Dispose all resources here
    }

    public void Dispose()
    {
        SquidVoxGraphicContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
