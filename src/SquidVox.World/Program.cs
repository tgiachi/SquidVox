using System.Globalization;
using DryIoc;
using Serilog;
using SquidVox.Core.Data.Directories;
using SquidVox.Core.Enums;
using SquidVox.Core.Interfaces.Services;
using SquidVox.Core.Json;
using SquidVox.Lua.Scripting.Context;
using SquidVox.Lua.Scripting.Services;
using SquidVox.World.Services;

namespace SquidVox.World;

/// <summary>
/// Contains the entry point for the application.
/// </summary>
public static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    public static void Main()
    {
        JsonUtils.RegisterJsonContext(SquidVoxLuaScriptJsonContext.Default);

        Log.Logger = new LoggerConfiguration().MinimumLevel.Debug()
            .WriteTo.Console(formatProvider: CultureInfo.DefaultThreadCurrentCulture)
            .CreateLogger();

        var rootDirectory = Path.Combine(Directory.GetCurrentDirectory(), "SquidVoxData");

        var directoriesConfig = new DirectoriesConfig(rootDirectory, Enum.GetNames<DirectoryType>());

        var container = new Container();

        container.RegisterInstance(directoriesConfig);


        container.Register<IAssetManagerService, AssetManagerService>(Reuse.Singleton);
        container.Register<IScriptEngineService, LuaScriptEngineService>(Reuse.Singleton);


        using var world = new SquidVoxWorld(container);
        world.Run();

    }
}
