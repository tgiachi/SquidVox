using SquidVox.Core.Data.Internal.Version;

namespace SquidVox.Core.Interfaces.Services;

/// <summary>
///     Interface for the version service that provides version information.
/// </summary>
public interface IVersionService
{
    VersionInfoData GetVersionInfo();
}
