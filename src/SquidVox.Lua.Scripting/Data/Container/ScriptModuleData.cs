namespace SquidVox.Lua.Scripting.Data.Container;

/// <summary>
///     Record containing data about a script module for internal processing
/// </summary>
/// <param name="ModuleType">The .NET type of the script module</param>
public record ScriptModuleData(Type ModuleType);
