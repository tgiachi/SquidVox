namespace SquidVox.Core.Data.Scripts;

/// <summary>
///     Metrics about script execution performance
/// </summary>
public class ScriptExecutionMetrics
{
    /// <summary>
    /// 
    /// </summary>
    public long ExecutionTimeMs { get; set; }
    /// <summary>
    /// 
    /// </summary>
    public long MemoryUsedBytes { get; set; }
    /// <summary>
    /// 
    /// </summary>
    public int StatementsExecuted { get; set; }
    /// <summary>
    /// 
    /// </summary>
    public int CacheHits { get; set; }
    /// <summary>
    /// 
    /// </summary>
    public int CacheMisses { get; set; }
    /// <summary>
    /// 
    /// </summary>
    public int TotalScriptsCached { get; set; }
}
