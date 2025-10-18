using System.Text.Json.Serialization;
using SquidVox.Lua.Scripting.Data;

namespace SquidVox.Lua.Scripting.Context;

[JsonSerializable(typeof(LuarcConfig))]
[JsonSerializable(typeof(LuarcRuntimeConfig))]
[JsonSerializable(typeof(LuarcWorkspaceConfig))]
[JsonSerializable(typeof(LuarcDiagnosticsConfig))]
[JsonSerializable(typeof(LuarcCompletionConfig))]
[JsonSerializable(typeof(LuarcFormatConfig))]
public partial class SquidVoxLuaScriptJsonContext : JsonSerializerContext
{
}
