using System.Reflection;
using SquidVox.Core.Data.Internal.Version;
using SquidVox.Core.Interfaces.Services;

namespace SquidVox.World3d.Services;

/// <summary>
/// Implements the version service for retrieving application version information.
/// </summary>
public class VersionService : IVersionService
{
    public VersionInfoData GetVersionInfo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "0.0.0";
        var appName = assembly.GetName().Name ?? "SquidVox.World3d";
        const string codeName = "Oceanus";

        return new VersionInfoData(appName, codeName, version);
    }
}
