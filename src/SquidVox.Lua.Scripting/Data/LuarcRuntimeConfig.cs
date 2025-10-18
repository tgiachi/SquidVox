using System.Text.Json.Serialization;

namespace SquidVox.Lua.Scripting.Data;

/// <summary>
///     Runtime configuration for Lua Language Server
/// </summary>
public class LuarcRuntimeConfig
{
    [JsonPropertyName("version")] public string Version { get; set; } = "Lua 5.4";

    [JsonPropertyName("path")] public string[] Path { get; set; } = [];
}
