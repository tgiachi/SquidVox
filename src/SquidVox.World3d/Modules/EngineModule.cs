using SquidVox.Core.Attributes.Scripts;
using SquidVox.Core.Interfaces.Services;

namespace SquidVox.World3d.Modules;

[ScriptModule("engine", "Provides core engine functionalities.")]
public class EngineModule
{
    private readonly IVersionService _versionService;

    public EngineModule(IVersionService versionService)
    {
        _versionService = versionService;
    }

    [ScriptFunction(helpText: "Retrieves the current engine version.")]
    public string GetVersion()
    {
        var versionInfo = _versionService.GetVersionInfo();
        return versionInfo.Version;
    }
}
