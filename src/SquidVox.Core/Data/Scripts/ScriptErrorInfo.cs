namespace SquidVox.Core.Data.Scripts;

/// <summary>
///     Detailed information about a JavaScript execution error
/// </summary>
public class ScriptErrorInfo
{
    /// <summary>
    /// 
    /// </summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>
    /// 
    /// </summary>
    public string? StackTrace { get; set; }
    /// <summary>
    /// 
    /// </summary>
    public int? LineNumber { get; set; }
    /// <summary>
    /// 
    /// </summary>
    public int? ColumnNumber { get; set; }
    /// <summary>
    /// 
    /// </summary>
    public string? FileName { get; set; }
    /// <summary>
    /// 
    /// </summary>
    public string? ErrorType { get; set; }
    /// <summary>
    /// 
    /// </summary>
    public string? SourceCode { get; set; }

    /// <summary>
    ///     Original TypeScript file name if source maps are available
    /// </summary>
    public string? OriginalFileName { get; set; }

    /// <summary>
    ///     Original line number in TypeScript if source maps are available
    /// </summary>
    public int? OriginalLineNumber { get; set; }

    /// <summary>
    ///     Original column number in TypeScript if source maps are available
    /// </summary>
    public int? OriginalColumnNumber { get; set; }
}
