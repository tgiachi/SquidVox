using DryIoc;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SquidVox.Core.Collections;
using SquidVox.Core.Context;
using SquidVox.Core.Interfaces.Services;
using SquidVox.Core.Utils;
using SquidVox.GameObjects.UI.Controls;
using SquidVox.World3d.Rendering;

namespace SquidVox.World3d;

/// <summary>
///
/// </summary>
public class SquidVoxWorld : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private readonly IContainer _container;
    private readonly RenderLayerCollection _renderLayers = new();

    /// <summary>
    ///
    /// </summary>
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

        var assetsManager = _container.Resolve<IAssetManagerService>();
        assetsManager.SetContentManager(Content);


        var defaultFont = ResourceUtils.GetEmbeddedResourceContent(
            "Assets.Fonts.Monocraft.ttf",
            typeof(SquidVoxWorld).Assembly
        );

        assetsManager.LoadFontFromBytes(defaultFont, "Monocraft");

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
        scriptEngine.StartAsync().GetAwaiter().GetResult();
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
