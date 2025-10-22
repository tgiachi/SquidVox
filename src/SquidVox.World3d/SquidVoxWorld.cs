using DryIoc;
using FontStashSharp;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using SquidVox.Core.Collections;
using SquidVox.Core.Context;
using SquidVox.Core.Data.Scripts;
using SquidVox.Core.Interfaces.Services;
using SquidVox.Core.Utils;
using SquidVox.GameObjects.UI.Controls;
using SquidVox.Voxel.GameObjects;
using SquidVox.Voxel.Primitives;
using SquidVox.Voxel.Types;
using SquidVox.World3d.GameObjects;
using SquidVox.World3d.Rendering;
using SquidVox.World3d.Scripts;

namespace SquidVox.World3d;

/// <summary>
/// Represents the main game world for SquidVox.
/// </summary>
public class SquidVoxWorld : Game
{
    private readonly ILogger _logger = Log.ForContext<SquidVoxWorld>();
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private readonly IContainer _container;
    private readonly RenderLayerCollection _renderLayers = new();
    private IInputManager _inputManager;


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

    /// <summary>
    /// Initializes the game.
    /// </summary>
    /// <summary>
    /// Initializes the game.
    /// </summary>
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
        assetsManager.LoadEffect("Effects/ChunkBlock");
        assetsManager.LoadEffect("Effects/DynamicSky");
        assetsManager.LoadEffect("Effects/Clouds");

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        FontSystemDefaults.FontResolutionFactor = 2.0f;
        FontSystemDefaults.KernelWidth = 2;
        FontSystemDefaults.KernelHeight = 2;

        _renderLayers.Add(new ImGuiRenderLayer(this));
        _renderLayers.Add(new GameObject2dRenderLayer());
        _renderLayers.Add(new SceneRenderLayer());
        _renderLayers.Add(new GameObject3dRenderLayer());

        _inputManager = _container.Resolve<IInputManager>();
        // TODO: Restore when InputContext is implemented
        // _inputManager.CurrentContext = InputContext.Gameplay3D;

        var scriptEngine = _container.Resolve<IScriptEngineService>();

        scriptEngine.OnScriptError += OnScriptError;

        scriptEngine.StartAsync().GetAwaiter().GetResult();

        _renderLayers.GetLayer<GameObject2dRenderLayer>().AddGameObject(new FpsComponent());

        _renderLayers.GetLayer<GameObject3dRenderLayer>()
            .AddGameObject(
                new CameraGameObject()
                {
                    FlyMode = false,
                    EnableInput = true,
                }
            );

        _renderLayers.GetLayer<ImGuiRenderLayer>()
            .AddDebugger(
                new LuaImGuiDebuggerObject(
                    "camera",
                    () =>
                    {
                        var camera = _renderLayers.GetLayer<GameObject3dRenderLayer>().GetComponent<CameraGameObject>();
                        var worldGameObject = _renderLayers.GetLayer<GameObject3dRenderLayer>()
                            .GetComponent<WorldGameObject>();

                        ImGui.Text("Camera position: " + camera.Position);
                        ImGui.Separator();
                        ImGui.Text("Camera rotation: " + camera.Rotation);

                        var ambientLight = worldGameObject.AmbientLight.ToNumerics();
                        var lightDir = worldGameObject.LightDirection.ToNumerics();

                        // Editor ImGui
                        if (ImGui.ColorEdit3("Ambient Light", ref ambientLight))
                        {
                            worldGameObject.AmbientLight = new Vector3(
                                ambientLight.X,
                                ambientLight.Y,
                                ambientLight.Z
                            );
                        }

                        if (ImGui.SliderFloat3("Light Direction", ref lightDir, -2f, 2f))
                        {
                            worldGameObject.LightDirection = new Vector3(
                                lightDir.X,
                                lightDir.Y,
                                lightDir.Z
                            );
                        }
                    }
                )
            );


        var worldManager = new WorldGameObject(_renderLayers.GetComponent<CameraGameObject>());

        worldManager.ChunkGenerator = CreateFlatChunkAsync;
        worldManager.EnableWireframe = false;


        var skyPanorama = new DynamicSkyGameObject(_renderLayers.GetComponent<CameraGameObject>());

        //skyPanorama.DebugMode = true;


        var clouds = (new CloudsGameObject(_renderLayers.GetComponent<CameraGameObject>()));

        clouds.GenerateRandomClouds(
            count: 20,
            minPosition: new Vector3(-200, 80, -200),
            maxPosition: new Vector3(200, 120, 200),
            minSize: new Vector3(8, 4, 8),
            maxSize: new Vector3(20, 10, 20)
        );

        _renderLayers.GetLayer<GameObject3dRenderLayer>().AddGameObject(skyPanorama);

        _renderLayers.GetLayer<GameObject3dRenderLayer>().AddGameObject(worldManager);
        _renderLayers.GetLayer<GameObject3dRenderLayer>().AddGameObject(clouds);


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
            var gameObjectLayer = _renderLayers.GetLayer<GameObject2dRenderLayer>();
            gameObjectLayer.RemoveGameObject(errorDialog);
        };

        // Add to UI layer
        var gameObjectLayer = _renderLayers.GetLayer<GameObject2dRenderLayer>();
        gameObjectLayer.AddGameObject(errorDialog);
    }

    protected override void Update(GameTime gameTime)
    {
        // if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
        //     Keyboard.GetState().IsKeyDown(Keys.Escape))
        // {
        //     Exit();
        // }

        SquidVoxGraphicContext.GameTime = gameTime;

        _inputManager.Update(gameTime);

        _inputManager.DistributeInput(gameTime);



        _renderLayers.UpdateAll(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(SquidVoxGraphicContext.ClearColor);

        _renderLayers.RenderAll(_spriteBatch);

        base.Draw(gameTime);
    }

    private static Task<ChunkEntity> CreateFlatChunkAsync(int chunkX, int chunkY, int chunkZ)
    {
        var chunkOrigin = new System.Numerics.Vector3(
            chunkX * ChunkEntity.Size,
            chunkY * ChunkEntity.Height,
            chunkZ * ChunkEntity.Size
        );

        var chunk = new ChunkEntity(chunkOrigin);

        if (chunkY > 0)
        {
            return Task.FromResult(chunk);
        }

        long id = (chunkX * 1000000L) + (chunkZ * 1000L) + 1;

        var random = new Random((chunkX * 73856093) ^ (chunkZ * 19349663));

        for (int x = 0; x < ChunkEntity.Size; x++)
        {
            for (int z = 0; z < ChunkEntity.Size; z++)
            {
                bool isInLake = (x >= 4 && x <= 11 && z >= 4 && z <= 11);

                for (int y = 0; y < ChunkEntity.Height; y++)
                {
                    BlockType blockType = BlockType.Air;

                    if (y == 0)
                    {
                        blockType = BlockType.Bedrock;
                    }
                    else if (y < 60)
                    {
                        blockType = BlockType.Dirt;
                    }
                    else if (y == 60)
                    {
                        blockType = BlockType.Grass;
                    }
                    else if (y == 61)
                    {
                        if (isInLake)
                        {
                            blockType = BlockType.Water;
                        }
                        else
                        {
                            var rand = random.NextDouble();
                            if (rand < 0.15)
                            {
                                blockType = BlockType.TallGrass;
                            }
                            else if (rand < 0.20)
                            {
                                blockType = BlockType.Flower;
                            }
                        }
                    }

                    if (blockType != BlockType.Air)
                    {
                        chunk.SetBlock(x, y, z, new BlockEntity(id++, blockType));
                    }
                }
            }
        }

        return Task.FromResult(chunk);
    }
}
