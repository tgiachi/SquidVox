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
using SquidVox.Core.Notifications;
using SquidVox.GameObjects.UI.Notifications;
using SquidVox.Voxel.GameObjects;
using SquidVox.Voxel.Interfaces.Services;
using SquidVox.Voxel.Primitives;
using SquidVox.Voxel.Types;
using SquidVox.World3d.GameObjects;
using SquidVox.World3d.GameObjects.Debug;
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
    private QuakeConsoleGameObject? _console;
    private INotificationService? _notificationService;
    private TextureAtlasDebugger? _atlasDebugger;
    private LuaImGuiDebuggerObject? _atlasDebuggerObject;
    private IInputManager _inputManager;
    private IPerformanceProfilerService _performanceProfilerService;


    /// <summary>
    /// Initializes a new instance of the SquidVoxWorld class.
    /// </summary>
    /// <param name="container">The dependency injection container.</param>
    public SquidVoxWorld(IContainer container)
    {
        _container = container;
        _container.RegisterInstance(_renderLayers);
        _graphics = new GraphicsDeviceManager(this);
        SquidVoxEngineContext.GraphicsDeviceManager = _graphics;
        SquidVoxEngineContext.Window = Window;


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

        SquidVoxEngineContext.WhitePixel = new Texture2D(GraphicsDevice, 1, 1);
        SquidVoxEngineContext.WhitePixel.SetData([Color.White]);
        var assetsManager = _container.Resolve<IAssetManagerService>();
        _ = _container.Resolve<ITimerService>();
        _ = _container.Resolve<IChunkGeneratorService>();
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
        assetsManager.LoadEffect("Effects/Rain");
        assetsManager.LoadEffect("Effects/Snow");

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

        var assetManager = _container.Resolve<IAssetManagerService>();
        _notificationService = _container.Resolve<INotificationService>();
        _performanceProfilerService = _container.Resolve<IPerformanceProfilerService>();
        var notificationHud = new NotificationHudGameObject();
        notificationHud.Initialize(assetManager, _notificationService);
        _renderLayers.GetLayer<GameObject2dRenderLayer>().AddGameObject(notificationHud);

        var imguiLayer = _renderLayers.GetLayer<ImGuiRenderLayer>();
        _atlasDebugger = new TextureAtlasDebugger();
        _atlasDebuggerObject = _atlasDebugger.CreateDebugger();
        imguiLayer.AddDebugger(_atlasDebuggerObject);

        var performanceProfilerDebugger = new PerformanceProfilerDebugger(_performanceProfilerService);
        imguiLayer.AddDebugger(performanceProfilerDebugger);

        _console = new QuakeConsoleGameObject();
        _console.WelcomeLines.Add("SquidVox console ready.");
        _console.WelcomeLines.Add("Type 'help' for available commands.");
        _console.Initialize(assetManager, _inputManager);
        _console.CommandSubmitted += HandleConsoleCommand;
        _renderLayers.GetLayer<GameObject2dRenderLayer>().AddGameObject(_console);

        _renderLayers.GetLayer<GameObject2dRenderLayer>().AddGameObject(new FpsComponent());

        _renderLayers.GetLayer<GameObject3dRenderLayer>()
            .AddGameObject(
                new CameraGameObject()
                {
                    FlyMode = true,
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
                        var fogColor = worldGameObject.FogColor.ToNumerics();
                        var fogStart = worldGameObject.FogStart;
                        var fogEnd = worldGameObject.FogEnd;


                        var chunkDistance = worldGameObject.ChunkLoadDistance;

                        var viewDistance = worldGameObject.ViewRange;
                        if (ImGui.SliderFloat("View Distance", ref viewDistance, 1, 512))
                        {
                            worldGameObject.ViewRange = viewDistance;
                        }


                        var useGreedyMeshing = worldGameObject.UseGreedyMeshing;
                        if (ImGui.Checkbox("Use Greedy Meshing", ref useGreedyMeshing))
                        {
                            worldGameObject.UseGreedyMeshing = useGreedyMeshing;
                        }

                        var useFlyMode = camera.FlyMode;
                        if (ImGui.Checkbox("Use Fly Mode", ref useFlyMode))
                        {
                            camera.FlyMode = useFlyMode;
                        }


                        var enableWireframe = worldGameObject.EnableWireframe;

                        if (ImGui.Checkbox("Enable Wireframe", ref enableWireframe))
                        {
                            worldGameObject.EnableWireframe = enableWireframe;
                        }


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

                        if (ImGui.ColorEdit3("Fog Color", ref fogColor))
                        {
                            worldGameObject.FogColor = new Vector3(
                                fogColor.X,
                                fogColor.Y,
                                fogColor.Z
                            );
                        }

                        if (ImGui.SliderFloat("Fog Start", ref fogStart, 0f, 1000f))
                        {
                            worldGameObject.FogStart = fogStart;
                        }

                        if (ImGui.SliderFloat("Fog End", ref fogEnd, 0f, 2000f))
                        {
                            worldGameObject.FogEnd = fogEnd;
                        }

                        if (ImGui.SliderInt("Chunk Load Distance", ref chunkDistance, 1, 32))
                        {
                            worldGameObject.ChunkLoadDistance = chunkDistance;
                        }
                    }
                )
            );


        var worldManager = new WorldGameObject(_renderLayers.GetComponent<CameraGameObject>());

        worldManager.ChunkGenerator = CreateFlatChunkAsync;
        worldManager.EnableWireframe = false;
        worldManager.UseGreedyMeshing = true;


        var skyPanorama = new DynamicSkyGameObject(_renderLayers.GetComponent<CameraGameObject>());

        //skyPanorama.DebugMode = true;


        var clouds = (new CloudsGameObject(_renderLayers.GetComponent<CameraGameObject>()));

        clouds.GenerateRandomClouds(
            count: 100,
            minPosition: new Vector3(-200, 80, -200),
            maxPosition: new Vector3(200, 120, 200),
            minSize: new Vector3(8, 4, 8),
            maxSize: new Vector3(20, 10, 20)
        );

        _renderLayers.GetLayer<GameObject3dRenderLayer>().AddGameObject(skyPanorama);
        _renderLayers.GetLayer<GameObject3dRenderLayer>().AddGameObject(worldManager);
        _renderLayers.GetLayer<GameObject3dRenderLayer>().AddGameObject(clouds);
        // _renderLayers.GetLayer<GameObject3dRenderLayer>()
        //     .AddGameObject(
        //         new WeatherGameObject(_renderLayers.GetComponent<CameraGameObject>())
        //         {
        //             RainIntensity = 0.5f
        //         }
        //     );
        // _renderLayers.GetLayer<GameObject3dRenderLayer>()
        //     .AddGameObject(
        //         new SnowGameObject(_renderLayers.GetComponent<CameraGameObject>())
        //         {
        //             SnowIntensity = 0.5f
        //         }
        //     );
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

        var updateStartTime = DateTime.UtcNow;

        SquidVoxEngineContext.GameTime = gameTime;

        _inputManager.Update(gameTime);

        _inputManager.DistributeInput(gameTime);


        _renderLayers.UpdateAll(gameTime);

        base.Update(gameTime);

        var updateEndTime = DateTime.UtcNow;
        var updateTime = (updateEndTime - updateStartTime).TotalMilliseconds;
        _performanceProfilerService.UpdateUpdateTime(updateTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        var drawStartTime = DateTime.UtcNow;

        GraphicsDevice.Clear(SquidVoxEngineContext.ClearColor);

        _renderLayers.RenderAll(_spriteBatch);

        base.Draw(gameTime);

        var drawEndTime = DateTime.UtcNow;
        var drawTime = (drawEndTime - drawStartTime).TotalMilliseconds;
        _performanceProfilerService.UpdateDrawTime(drawTime);

        // Update frame time (total frame time)
        var frameTime = gameTime.ElapsedGameTime.TotalMilliseconds;
        _performanceProfilerService.UpdateFrameTime(frameTime);
    }

    protected override void UnloadContent()
    {
        if (_console != null)
        {
            _console.CommandSubmitted -= HandleConsoleCommand;
            _console.Dispose();
            _console = null;
        }

        if (_atlasDebuggerObject != null)
        {
            var imguiLayer = _renderLayers.GetLayer<ImGuiRenderLayer>();
            imguiLayer.RemoveDebugger(_atlasDebuggerObject);
            _atlasDebuggerObject = null;
        }

        _atlasDebugger?.Dispose();
        _atlasDebugger = null;

        base.UnloadContent();
    }

    private void HandleConsoleCommand(object? sender, string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return;
        }

        if (sender is not QuakeConsoleGameObject console)
        {
            return;
        }

        var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return;
        }

        if (string.Equals(parts[0], "notify", StringComparison.OrdinalIgnoreCase))
        {
            if (parts.Length == 1)
            {
                console.AddLine("Usage: notify [info|success|warning|error] <message>", Color.Yellow);
                return;
            }

            var notificationType = NotificationType.Info;
            var messageStartIndex = 1;

            if (TryParseNotificationType(parts[1], out var parsedType))
            {
                notificationType = parsedType;
                messageStartIndex = 2;
            }

            if (messageStartIndex >= parts.Length)
            {
                console.AddLine("notify: missing message text", Color.Yellow);
                return;
            }

            var message = string.Join(" ", parts[messageStartIndex..]);
            _notificationService?.ShowMessage(message, notificationType);
            console.AddLine($"Notification queued ({notificationType})", Color.LightBlue);
            return;
        }

        console.AddLine($"Unknown command: {command}", Color.OrangeRed);
    }

    private static bool TryParseNotificationType(string value, out NotificationType type)
    {
        switch (value.ToLowerInvariant())
        {
            case "info":
                type = NotificationType.Info;
                return true;
            case "success":
                type = NotificationType.Success;
                return true;
            case "warning":
                type = NotificationType.Warning;
                return true;
            case "error":
                type = NotificationType.Error;
                return true;
            default:
                type = NotificationType.Info;
                return false;
        }
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
