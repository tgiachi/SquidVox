using DryIoc;
using Serilog;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using SquidVox.Core.Collections;
using SquidVox.Core.Data.Graphics;
using SquidVox.Core.Extensions.Collections;
using SquidVox.Core.Interfaces.GameObjects;
using SquidVox.Core.Interfaces.Rendering;
using SquidVox.Core.Interfaces.Services;
using SquidVox.Core.Utils;
using SquidVox.World.Context;
using SquidVox.World.Fonts;
using SquidVox.World.GameObjects;
using SquidVox.World.Rendering;
using TrippyGL;

namespace SquidVox.World;

/// <summary>
/// Represents the main world class for SquidVox, handling the game loop and window management.
/// </summary>
public class SquidVoxWorld : IDisposable
{
    private readonly ILogger _logger = Log.ForContext<SquidVoxWorld>();

    private TextureBatcher _textureBatcher;
    private FontStashRenderer _fontRenderer;
    private ImGuiRenderLayer _imguiLayer;
    private readonly SvoxGameObjectCollection<ISVox2dDrawableGameObject> _gameObjects = new();

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

        InitDefaultAssets();
    }

    private void InitDefaultAssets()
    {
        var assetsManager = _container.Resolve<IAssetManagerService>();

        var defaultFont = ResourceUtils.GetEmbeddedResourceContent(
            "Assets.Fonts.Monocraft.ttf",
            typeof(SquidVoxWorld).Assembly
        );

        assetsManager.LoadFontFromBytes(defaultFont, "Monocraft");
    }

    /// <summary>
    /// Registers a custom render layer.
    /// Layers are automatically sorted by their Layer priority.
    /// </summary>
    /// <param name="layer">The layer to register.</param>
    public void RegisterRenderLayer(IRenderableLayer layer)
    {
        RenderLayers.Add(layer);
        _logger.Debug("Registered render layer at priority {Layer}", layer.Layer);
    }

    /// <summary>
    /// Gets the render layer collection.
    /// </summary>
    public RenderLayerCollection RenderLayers { get; } = new();

    /// <summary>
    /// Gets the game object collection for managing 2D game objects.
    /// Game objects added to this collection will be automatically updated and rendered.
    /// </summary>
    public SvoxGameObjectCollection<ISVox2dDrawableGameObject> GameObjects => _gameObjects;

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

        _fontRenderer = new FontStashRenderer(SquidVoxGraphicContext.GraphicsDevice);

        // Initialize default render layers
        InitializeRenderLayers();

        var scriptEngine = _container.Resolve<IScriptEngineService>();

        scriptEngine.StartAsync().GetAwaiter().GetResult();

        _gameObjects.Add(new TextGameObject() { Position = Vector2D<float>.One, FontSize = 30});

        Window_FramebufferResize(SquidVoxGraphicContext.Window.FramebufferSize);


    }

    /// <summary>
    /// Initializes the default render layers.
    /// </summary>
    private void InitializeRenderLayers()
    {
        // Scene layer (World2D priority)
        var sceneManager = _container.Resolve<ISceneManager>();
        var sceneLayer = new SceneRenderLayer(sceneManager);
        RegisterRenderLayer(sceneLayer);

        // Game objects layer (World2D priority)
        var gameObjectLayer = new GameObjectRenderLayer(_gameObjects);
        RegisterRenderLayer(gameObjectLayer);

        // ImGui layer (DebugUI priority - always on top)
        _imguiLayer = new ImGuiRenderLayer();
        RegisterRenderLayer(_imguiLayer);

        _logger.Information(
            "Initialized {Count} render layers ({Enabled} enabled)",
            RenderLayers.Count,
            RenderLayers.GetEnabledCount()
        );
    }

    /// <summary>
    /// Handles the window render event, updating and rendering the game.
    /// </summary>
    /// <param name="delta">The time elapsed since the last frame.</param>
    private void Window_Render(double delta)
    {
        // Update phase
        SquidVoxGraphicContext.GameTime.Update(delta);

        // Update scene manager
        var sceneManager = _container.Resolve<ISceneManager>();
        sceneManager.Update(SquidVoxGraphicContext.GameTime);

        // Update all game objects
        _gameObjects.UpdateAll(SquidVoxGraphicContext.GameTime);

        // Custom update event
        OnUpdate?.Invoke(SquidVoxGraphicContext.GameTime);

        // Update ImGui layer with delta time
        _imguiLayer.Update((float)delta);

        // Render phase - clear buffer
        SquidVoxGraphicContext.GraphicsDevice.ClearColor = SquidVoxGraphicContext.ClearColor;
        SquidVoxGraphicContext.GraphicsDevice.Clear(ClearBuffers.Color);

        _fontRenderer.Begin();
      //  _textureBatcher.Begin();
        // Render all layers in priority order
        RenderLayers.RenderAll(_textureBatcher, _fontRenderer);

        // Custom render event (for backward compatibility)
        OnRender?.Invoke();
   //     _textureBatcher.End();
        _fontRenderer.End();
    }

    /// <summary>
    /// Handles the window framebuffer resize event.
    /// </summary>
    /// <param name="size">The new size of the framebuffer.</param>
    private void Window_FramebufferResize(Vector2D<int> size)
    {
        SquidVoxGraphicContext.GraphicsDevice.SetViewport(0, 0, (uint)size.X, (uint)size.Y);



        // Update font renderer viewport
        _fontRenderer?.OnViewportChanged();

        OnResize?.Invoke(size);
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
        _textureBatcher?.Dispose();
        _fontRenderer?.Dispose();
        SquidVoxGraphicContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
