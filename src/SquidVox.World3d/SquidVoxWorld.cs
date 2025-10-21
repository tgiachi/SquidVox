using DryIoc;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SquidVox.Core.Collections;
using SquidVox.Core.Context;
using SquidVox.Core.Data.Scripts;
using SquidVox.Core.Interfaces.Services;
using SquidVox.Core.Utils;
using SquidVox.GameObjects.UI.Controls;
using SquidVox.World3d.Rendering;

namespace SquidVox.World3d;

/// <summary>
/// Represents the main game world for SquidVox.
/// </summary>
public class SquidVoxWorld : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private readonly IContainer _container;
    private readonly RenderLayerCollection _renderLayers = new();

    /// <summary>
    /// Initializes a new instance of the SquidVoxWorld class.
    /// </summary>
    /// <param name="container">The dependency injection container.</param>
    public SquidVoxWorld(IContainer container)
    {
        _container = container;
        _container.RegisterInstance(_renderLayers);
        _graphics = new GraphicsDeviceManager(this);
        SquidVoxGraphicContext.GraphicsDeviceManager = _graphics;
        SquidVoxGraphicContext.Window = Window;


        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // TODO: Add your initialization logic here

        SquidVoxGraphicContext.WhitePixel = new Texture2D(GraphicsDevice, 1, 1);
        SquidVoxGraphicContext.WhitePixel.SetData([Color.White]);
        var assetsManager = _container.Resolve<IAssetManagerService>();
        assetsManager.SetContentManager(Content);


        var defaultUiFont = ResourceUtils.GetEmbeddedResourceContent(
            "Assets.Fonts.Monocraft.ttf",
            typeof(SquidVoxWorld).Assembly
        );

        var defaultMonoFont = ResourceUtils.GetEmbeddedResourceContent(
            "Assets.Fonts.DefaultMonoFont.ttf",
            typeof(SquidVoxWorld).Assembly
        );

        assetsManager.LoadFontFromBytes(defaultUiFont, "DefaultMono");

        assetsManager.LoadFontFromBytes(defaultUiFont, "Monocraft");

        assetsManager.LoadEffect("Effects/ChunkBillboard");
        assetsManager.LoadEffect("Effects/ChunkFluid");
        assetsManager.LoadEffect("Effects/ChunkSolid");

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        FontSystemDefaults.FontResolutionFactor = 2.0f;
        FontSystemDefaults.KernelWidth = 2;
        FontSystemDefaults.KernelHeight = 2;

        _renderLayers.Add(new ImGuiRenderLayer(this));
        _renderLayers.Add(new GameObjectRenderLayer());
        _renderLayers.Add(new SceneRenderLayer());

        _renderLayers.GetLayer<GameObjectRenderLayer>()
            .AddGameObject(
                new LabelGameObject("Hello World")
                {
                    Position = new Vector2(50, 50),
                    FontSize = 24,
                    Color = Color.Red
                }
            );

        var scriptEngine = _container.Resolve<IScriptEngineService>();

        // Subscribe to script errors to show error dialog
        scriptEngine.OnScriptError += OnScriptError;

        scriptEngine.StartAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Handles Lua script errors by displaying an error dialog.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="errorInfo">The error information.</param>
    private void OnScriptError(object? sender, ScriptErrorInfo errorInfo)
    {
        // Create error dialog
        var assetManager = _container.Resolve<IAssetManagerService>();
        var errorDialog = new ScriptErrorGameObject(errorInfo);
        errorDialog.Initialize(assetManager, GraphicsDevice);

        // Handle dialog close event
        errorDialog.Closed += (s, e) =>
        {
            var gameObjectLayer = _renderLayers.GetLayer<GameObjectRenderLayer>();
            gameObjectLayer.RemoveGameObject(errorDialog);
        };

        // Add to UI layer
        var gameObjectLayer = _renderLayers.GetLayer<GameObjectRenderLayer>();
        gameObjectLayer.AddGameObject(errorDialog);
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        // Handle input for render layers
        _renderLayers.HandleKeyboardAll(Keyboard.GetState(), gameTime);
        _renderLayers.HandleMouseAll(Mouse.GetState(), gameTime);


        _renderLayers.UpdateAll(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(SquidVoxGraphicContext.ClearColor);

        _renderLayers.RenderAll(_spriteBatch);


        base.Draw(gameTime);
    }
}
