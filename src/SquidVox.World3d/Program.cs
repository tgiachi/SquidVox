using System.Globalization;
using ConsoleAppFramework;
using DryIoc;
using Serilog;
using Serilog.Formatting.Compact;
using SquidVox.Core.Context;
using SquidVox.Core.Data.Directories;
using SquidVox.Core.Enums;
using SquidVox.Core.Extensions.Directories;
using SquidVox.Core.Interfaces.Services;
using SquidVox.Core.Json;
using SquidVox.JS.Scripting.Configs;
using SquidVox.JS.Scripting.Extensions.Scripts;
using SquidVox.JS.Scripting.Services;
using SquidVox.JS.Scripting.Utils;
using SquidVox.Voxel.Interfaces.Generation.Pipeline;
using SquidVox.Voxel.Interfaces.Services;
using SquidVox.Voxel.Json;
using SquidVox.Voxel.Modules;
using SquidVox.Voxel.Services;
using SquidVox.World3d;
using SquidVox.World3d.Modules;
using SquidVox.World3d.Services;

await ConsoleApp.RunAsync(
    args,
    async (string? rootDirectory = null, bool logToFile = false) =>
    {
        //JsonUtils.RegisterJsonContext(SquidVoxLuaScriptJsonContext.Default);
        JsonUtils.RegisterJsonContext(SquidVoxVoxelJsonContext.Default);

        var loggingConfiguration = new LoggerConfiguration().MinimumLevel.Debug()
            .WriteTo.Console(formatProvider: CultureInfo.DefaultThreadCurrentCulture);


        rootDirectory = rootDirectory.ResolvePathAndEnvs();

        rootDirectory ??= Path.Combine(Directory.GetCurrentDirectory(), "SquidVoxData");

        var directoriesConfig = new DirectoriesConfig(rootDirectory, Enum.GetNames<DirectoryType>());

        if (logToFile)
        {
            loggingConfiguration = loggingConfiguration.WriteTo.File(
                new CompactJsonFormatter(),
                path: Path.Combine(directoriesConfig[DirectoryType.Logs], "squidvox_world_.log"),
                rollingInterval: RollingInterval.Day
            );
        }

        Log.Logger = loggingConfiguration.CreateLogger();

        var container = new Container();
        SquidVoxEngineContext.Container = container;
        container.RegisterInstance(directoriesConfig);

        container
            .AddScriptModule<EngineModule>()
            .AddScriptModule<MathModule>()
            .AddScriptModule<ConsoleModule>()
            .AddScriptModule<WindowModule>()
            .AddScriptModule<ImGuiModule>()
            .AddScriptModule<AssetManagerModule>()
            .AddScriptModule<InputManagerModule>()
            .AddScriptModule<RenderLayerModule>()
            .AddScriptModule<BlockManagerModule>()
            .AddScriptModule<BlockTypeModule>()
            .AddScriptModule<GameTimeModule>()
            .AddScriptModule<GenerationModule>()
            .AddScriptModule<CameraModule>()
            ;

        TypeScriptDocumentationGenerator.AddInterfaceToGenerate(typeof(IGeneratorContext));

        container.RegisterInstance(new ScriptEngineConfig());

        container.Register<IAssetManagerService, AssetManagerService>(Reuse.Singleton);
        container.Register<ISceneManager, SceneManagerService>(Reuse.Singleton);
        container.Register<IScriptEngineService, JsScriptEngineService>(Reuse.Singleton);
        container.Register<IInputManager, InputManagerService>(Reuse.Singleton);
        container.Register<IBlockManagerService, BlockManagerService>(Reuse.Singleton);
        container.Register<IChunkGeneratorService, ChunkGeneratorService>(Reuse.Singleton);
        container.Register<ITimerService, TimerService>(Reuse.Singleton);
        container.Register<INotificationService, NotificationService>(Reuse.Singleton);
        container.Register<IPerformanceProfilerService, PerformanceProfilerService>(Reuse.Singleton);
        container.Register<IVersionService, VersionService>(Reuse.Singleton);


        using var game = new SquidVoxWorld(container);
        game.Run();
    }
);
