using System.Globalization;
using ConsoleAppFramework;
using DryIoc;
using ImGuiNET;
using Microsoft.Xna.Framework;
using MoonSharp.Interpreter;
using Serilog;
using SquidVox.Core.Context;
using SquidVox.Core.Data.Directories;
using SquidVox.Core.Enums;
using SquidVox.Core.Extensions.Directories;
using SquidVox.Core.Interfaces.Services;
using SquidVox.Core.Json;
using SquidVox.Lua.Scripting.Context;
using SquidVox.Lua.Scripting.Extensions.Scripts;
using SquidVox.Lua.Scripting.Services;
using SquidVox.World3d;
using SquidVox.World3d.Modules;
using SquidVox.World3d.Services;

await ConsoleApp.RunAsync(
    args,
    async (string? rootDirectory = null) =>
    {
        JsonUtils.RegisterJsonContext(SquidVoxLuaScriptJsonContext.Default);

        Log.Logger = new LoggerConfiguration().MinimumLevel.Debug()
            .WriteTo.Console(formatProvider: CultureInfo.DefaultThreadCurrentCulture)
            .CreateLogger();

        rootDirectory = rootDirectory.ResolvePathAndEnvs();

        rootDirectory ??= Path.Combine(Directory.GetCurrentDirectory(), "SquidVoxData");

        var directoriesConfig = new DirectoriesConfig(rootDirectory, Enum.GetNames<DirectoryType>());


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
            ;


        // register custom userType for LUA

        UserData.RegisterType<Vector2>();
        UserData.RegisterType<Vector3>();
        UserData.RegisterType(typeof(ImGui));

        container.Register<IAssetManagerService, AssetManagerService>(Reuse.Singleton);
        container.Register<ISceneManager, SceneManagerService>(Reuse.Singleton);
        container.Register<IScriptEngineService, LuaScriptEngineService>(Reuse.Singleton);
        container.Register<IInputManager, InputManagerService>(Reuse.Singleton);


        using var game = new SquidVoxWorld(container);
        game.Run();
    }
);
