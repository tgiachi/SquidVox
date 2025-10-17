using DryIoc;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using SquidVox.Core.Data.Graphics;
using SquidVox.Core.Interfaces.Services;
using SquidVox.World.Context;
using TrippyGL;

namespace SquidVox.World;

/// <summary>
/// Represents the main world class for SquidVox, handling the game loop and window management.
/// </summary>
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

    private readonly IContainer _container;

    public SquidVoxWorld(IContainer container)
    {
        _container = container;

        _container.Resolve<IAssetManagerService>();
    }

    /// <summary>
    /// Starts the game loop and runs the application.
    /// </summary>
    public void Run()
    {
        WindowOptions windowOpts = WindowOptions.Default;
        windowOpts.Title = "SquidVox World";
        windowOpts.API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.Debug, new APIVersion(3, 3));

        SquidVoxGraphicContext.Window  = Window.Create(windowOpts);
        SquidVoxGraphicContext.Window .Load += Window_Load;
        SquidVoxGraphicContext.Window .Render += Window_Render;
        SquidVoxGraphicContext.Window .FramebufferResize += Window_FramebufferResize;
        SquidVoxGraphicContext.Window .Closing += Window_Closing;

        SquidVoxGraphicContext.Window.Run();
    }

    /// <summary>
    /// Handles the window load event, initializing graphics resources.
    /// </summary>
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

    /// <summary>
    /// Handles the window render event, updating and rendering the game.
    /// </summary>
    /// <param name="delta">The time elapsed since the last frame.</param>
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

    /// <summary>
    /// Handles the window framebuffer resize event.
    /// </summary>
    /// <param name="size">The new size of the framebuffer.</param>
    private void Window_FramebufferResize(Vector2D<int> size)
    {
        SquidVoxGraphicContext.GraphicsDevice.SetViewport(0, 0, (uint)size.X, (uint)size.Y);

        OnResize?.Invoke(size);
        // Resize code here
    }

    /// <summary>
    /// Handles the window closing event.
    /// </summary>
    private void Window_Closing()
    {
        OnWindowClosing?.Invoke();
        // Dispose all resources here
    }

    /// <summary>
    /// Disposes of resources used by the SquidVoxWorld.
    /// </summary>
    public void Dispose()
    {
        SquidVoxGraphicContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
