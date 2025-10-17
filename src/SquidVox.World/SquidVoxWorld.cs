using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using SquidVox.Core.Data.Graphics;
using SquidVox.World.Context;
using TrippyGL;

namespace SquidVox.World;

public class SquidVoxWorld : IDisposable
{
    public delegate void OnUpdateHandler(GameTime gameTime);

    public delegate void OnRenderHandler();

    public delegate void OnWindowClosingHandler();

    public delegate void OnResizeHandler(Vector2D<int> size);

    public event OnUpdateHandler OnUpdate;
    public event OnRenderHandler OnRender;
    public event OnWindowClosingHandler OnWindowClosing;
    public event OnResizeHandler OnResize;

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

    private void Window_Load()
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

    private void Window_Render(double delta)
    {
        SquidVoxGraphicContext.GameTime.Update(delta);
        OnUpdate?.Invoke(SquidVoxGraphicContext.GameTime);

        SquidVoxGraphicContext.GraphicsDevice.ClearColor = Color4b.CornflowerBlue;

        SquidVoxGraphicContext.GraphicsDevice.Clear(ClearBuffers.Color);

        SquidVoxGraphicContext.ImGuiController.Update((float)delta);
        
        ImGuiNET.ImGui.ShowDemoWindow();


        OnRender?.Invoke();
        SquidVoxGraphicContext.ImGuiController.Render();
    }

    private void Window_FramebufferResize(Vector2D<int> size)
    {
        SquidVoxGraphicContext.GraphicsDevice.SetViewport(0, 0, (uint)size.X, (uint)size.Y);

        OnResize?.Invoke(size);
        // Resize code here
    }

    private void Window_Closing()
    {
        OnWindowClosing?.Invoke();
        // Dispose all resources here
    }

    public void Dispose()
    {
        SquidVoxGraphicContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
