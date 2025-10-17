using System.Globalization;
using DryIoc;
using Serilog;
using SquidVox.Core.Interfaces.Services;
using SquidVox.World.Services;

namespace SquidVox.World;

public static class Program
{
    public static void Main()
    {
        var container = new Container();

        Log.Logger = new LoggerConfiguration().MinimumLevel.Debug()
            .WriteTo.Console(formatProvider: CultureInfo.DefaultThreadCurrentCulture)
            .CreateLogger();


        container.Register<IAssetManagerService, AssetManagerService>(Reuse.Singleton);


        using var world = new SquidVoxWorld(container);
        world.Run();
    }
}
