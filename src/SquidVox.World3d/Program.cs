using System.Globalization;
using ConsoleAppFramework;
using DryIoc;
using ImGuiNET;
using Microsoft.Xna.Framework;
using MoonSharp.Interpreter;
using Serilog;
using Serilog.Formatting.Compact;
using SquidVox.Core.Context;
using SquidVox.Core.Data.Directories;
using SquidVox.Core.Enums;
using SquidVox.Core.Extensions.Directories;
using SquidVox.Core.Interfaces.Services;
using SquidVox.Core.Json;
using SquidVox.Lua.Scripting.Context;
using SquidVox.Lua.Scripting.Extensions.Scripts;
using SquidVox.Lua.Scripting.Services;
using SquidVox.Voxel.Contexts;
using SquidVox.Voxel.Data.Entities;
using SquidVox.Voxel.Interfaces;
using SquidVox.Voxel.Interfaces.Services;
using SquidVox.Voxel.Json;
using SquidVox.Voxel.Modules;
using SquidVox.Voxel.Services;
using SquidVox.World3d;
using SquidVox.World3d.Modules;
using SquidVox.World3d.Services;

await ConsoleApp.RunAsync(
    args,
    async (string? rootDirectory = null, bool logToFile = true) =>
    {
        JsonUtils.RegisterJsonContext(SquidVoxLuaScriptJsonContext.Default);
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
        SquidVoxGraphicContext.Container = container;
        container.RegisterInstance(directoriesConfig);

        container
            .AddLuaScriptModule<ConsoleModule>()
            .AddLuaScriptModule<WindowModule>()
            .AddLuaScriptModule<ImGuiModule>()
            .AddLuaScriptModule<AssetManagerModule>()
            .AddLuaScriptModule<InputManagerModule>()
            .AddLuaScriptModule<RenderLayerModule>()
            .AddLuaScriptModule<BlockManagerModule>()
            .AddLuaScriptModule<GameTimeModule>()
            .AddLuaScriptModule<GenerationModule>()
            ;


        // register custom userType for LUA

        UserData.RegisterType<Vector2>();
        UserData.RegisterType<Vector3>();
        UserData.RegisterType(typeof(ImGui));
        UserData.RegisterType<BlockDefinitionData>();
        UserData.RegisterType<GeneratorContext>();

        container.Register<IAssetManagerService, AssetManagerService>(Reuse.Singleton);
        container.Register<ISceneManager, SceneManagerService>(Reuse.Singleton);
        container.Register<IScriptEngineService, LuaScriptEngineService>(Reuse.Singleton);
        container.Register<IInputManager, InputManagerService>(Reuse.Singleton);
        container.Register<IBlockManagerService, BlockManagerService>(Reuse.Singleton);
        container.Register<IChunkGeneratorService, ChunkGeneratorService>(Reuse.Singleton);
        container.Register<ITimerService, TimerService>(Reuse.Singleton);


        using var game = new SquidVoxWorld(container);
        game.Run();
    }
);
