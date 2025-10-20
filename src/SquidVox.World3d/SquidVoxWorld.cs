using DryIoc;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SquidVox.Core.Collections;
using SquidVox.Core.Interfaces.Services;
using SquidVox.Core.Utils;
using SquidVox.World3d.Context;
using SquidVox.World3d.GameObjects;
using SquidVox.World3d.ImGUI;
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
        _graphics = new GraphicsDeviceManager(this);
        SquidVoxGraphicContext.GraphicsDeviceManager = _graphics;

        SquidVoxGraphicContext.Window = Window;
        Content.RootDirectory = "Content";
        _container = container;
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // TODO: Add your initialization logic here

        var assetsManager = _container.Resolve<IAssetManagerService>();

        var defaultFont = ResourceUtils.GetEmbeddedResourceContent(
            "Assets.Fonts.Monocraft.ttf",
            typeof(SquidVoxWorld).Assembly
        );

        assetsManager.LoadFontFromBytes(defaultFont, "Monocraft");
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);


        _renderLayers.Add(new ImGuiRenderLayer(this));
        _renderLayers.Add(new GameObjectRenderLayer());
        _renderLayers.Add(new SceneRenderLayer(_container));

        _renderLayers.GetLayer<GameObjectRenderLayer>().AddGameObject(new TextGameObject()
        {
            Position = Vector2.One,
            FontSize = 33
        });

        var scriptEngine = _container.Resolve<IScriptEngineService>();
        scriptEngine.StartAsync().GetAwaiter().GetResult();


        // TODO: use this.Content to load your game content here
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

        // TODO: Add your update logic here

        _renderLayers.UpdateAll(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _renderLayers.RenderAll(_spriteBatch);

        // TODO: Add your drawing code here

        base.Draw(gameTime);
    }
}
