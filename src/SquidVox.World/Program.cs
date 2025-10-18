using System.Globalization;
using ConsoleAppFramework;
using DryIoc;
using Serilog;
using SquidVox.Core.Data.Directories;
using SquidVox.Core.Enums;
using SquidVox.Core.Interfaces.Services;
using SquidVox.Core.Json;
using SquidVox.Lua.Scripting.Context;
using SquidVox.Lua.Scripting.Extensions.Scripts;
using SquidVox.Lua.Scripting.Services;
using SquidVox.World;
using SquidVox.World.Modules;
using SquidVox.World.Services;


await ConsoleApp.RunAsync(
    args,
    async (string? rootDirectory = null) =>
    {
        JsonUtils.RegisterJsonContext(SquidVoxLuaScriptJsonContext.Default);

        Log.Logger = new LoggerConfiguration().MinimumLevel.Debug()
            .WriteTo.Console(formatProvider: CultureInfo.DefaultThreadCurrentCulture)
            .CreateLogger();

        rootDirectory ??= Path.Combine(Directory.GetCurrentDirectory(), "SquidVoxData");

        var directoriesConfig = new DirectoriesConfig(rootDirectory, Enum.GetNames<DirectoryType>());

        var container = new Container();

        container.RegisterInstance(directoriesConfig);

        container.AddLuaScriptModule<ConsoleModule>();

        container.Register<IAssetManagerService, AssetManagerService>(Reuse.Singleton);

        container.Register<ISceneManager, SceneManagerService>(Reuse.Singleton);

        container.Register<IScriptEngineService, LuaScriptEngineService>(Reuse.Singleton);

        using var world = new SquidVoxWorld(container);
        world.Run();
    }
);
