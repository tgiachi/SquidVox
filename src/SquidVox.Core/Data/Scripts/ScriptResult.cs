namespace SquidVox.Core.Data.Scripts;

/// <summary>
/// Represents the result of a script execution.
/// </summary>
public class ScriptResult
{
    /// <summary>
    /// 
    /// </summary>
    public bool Success { get; set; }
    /// <summary>
    /// 
    /// </summary>
    public string Message { get; set; }
    /// <summary>
    /// 
    /// </summary>
    public object? Data { get; set; }
}


