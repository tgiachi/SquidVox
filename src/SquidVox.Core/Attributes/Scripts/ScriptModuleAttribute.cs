namespace SquidVox.Core.Attributes.Scripts;

/// <summary>
///     Attribute to mark a class as a script module that will be exposed to JavaScript
/// </summary>
/// <param name="name">The name under which the module will be accessible in JavaScript</param>
[AttributeUsage(AttributeTargets.Class)]
/// <summary>
/// 
/// </summary>
public class ScriptModuleAttribute(string name, string? helpText = null) : Attribute
{
    /// <summary>
    /// 
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// 
    /// </summary>
    public string? HelpText { get; } = helpText;
}
