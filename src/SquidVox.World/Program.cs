using System.Globalization;
using DryIoc;
using Serilog;
using SquidVox.Core.Data.Directories;
using SquidVox.Core.Enums;
using SquidVox.Core.Interfaces.Services;
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
        Log.Logger = new LoggerConfiguration().MinimumLevel.Debug()
            .WriteTo.Console(formatProvider: CultureInfo.DefaultThreadCurrentCulture)
            .CreateLogger();

        var rootDirectory = Path.Combine(Directory.GetCurrentDirectory(), "SquidVoxData");

        var directoriesConfig = new DirectoriesConfig(rootDirectory, Enum.GetNames<DirectoryType>());

        var container = new Container();

        container.RegisterInstance(directoriesConfig);


        container.Register<IAssetManagerService, AssetManagerService>(Reuse.Singleton);


        using var world = new SquidVoxWorld(container);
        world.Run();

    }
}
