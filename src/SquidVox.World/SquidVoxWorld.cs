using DryIoc;
using Serilog;
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
    private readonly ILogger _logger = Log.ForContext<SquidVoxWorld>();


    private TextureBatcher _textureBatcher;

    /// <summary>
    /// Delegate for handling update events.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    public delegate void OnUpdateHandler(GameTime gameTime);

    /// <summary>
    /// Delegate for handling render events.
    /// </summary>
    public delegate void OnRenderHandler();

    /// <summary>
    /// Delegate for handling window closing events.
    /// </summary>
    public delegate void OnWindowClosingHandler();

    /// <summary>
    /// Delegate for handling resize events.
    /// </summary>
    /// <param name="size">The new size of the window.</param>
    public delegate void OnResizeHandler(Vector2D<int> size);

    public event OnUpdateHandler OnUpdate;

    public event OnRenderHandler OnRender;

    public event OnWindowClosingHandler OnWindowClosing;

    public event OnResizeHandler OnResize;

    private readonly IContainer _container;

    /// <summary>
    /// Initializes a new instance of the SquidVoxWorld class.
    /// </summary>
    /// <param name="container">The dependency injection container.</param>
    public SquidVoxWorld(IContainer container)
    {
        _logger.Debug("Initializing world");
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

        SquidVoxGraphicContext.Window = Window.Create(windowOpts);
        SquidVoxGraphicContext.Window.Load += Window_Load;
        SquidVoxGraphicContext.Window.Render += Window_Render;
        SquidVoxGraphicContext.Window.FramebufferResize += Window_FramebufferResize;
        SquidVoxGraphicContext.Window.Closing += Window_Closing;

        _logger.Debug("Running SquidVox World");

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

        // Create 1x1 white pixel texture for drawing solid color rectangles
        SquidVoxGraphicContext.WhitePixel = new Texture2D(SquidVoxGraphicContext.GraphicsDevice, 1, 1);
        Span<Color4b> whitePixelData = stackalloc Color4b[1];
        whitePixelData[0] = Color4b.White;
        SquidVoxGraphicContext.WhitePixel.SetData<Color4b>(whitePixelData);

        _textureBatcher = new TextureBatcher(SquidVoxGraphicContext.GraphicsDevice, 512U);

        _container.Resolve<IScriptEngineService>();

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

        _logger.Debug("Disposing SquidVoxWorld resources.");

        // Dispose all resources and close the window
        SquidVoxGraphicContext.Dispose();
        SquidVoxGraphicContext.Window.Close();
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
