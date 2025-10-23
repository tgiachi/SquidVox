namespace SquidVox.Core.Interfaces.Services;

/// <summary>
///     Service for collecting and managing performance metrics
/// </summary>
public interface IPerformanceProfilerService
{
    /// <summary>
    ///     Gets the current frame rate (FPS)
    /// </summary>
    double CurrentFps { get; }

    /// <summary>
    ///     Gets the average frame rate over the last few seconds
    /// </summary>
    double AverageFps { get; }

    /// <summary>
    ///     Gets the current frame time in milliseconds
    /// </summary>
    double CurrentFrameTime { get; }

    /// <summary>
    ///     Gets the average frame time in milliseconds
    /// </summary>
    double AverageFrameTime { get; }

    /// <summary>
    ///     Gets the current update time in milliseconds
    /// </summary>
    double CurrentUpdateTime { get; }

    /// <summary>
    ///     Gets the current draw time in milliseconds
    /// </summary>
    double CurrentDrawTime { get; }

    /// <summary>
    ///     Gets the average draw time in milliseconds
    /// </summary>
    double AverageDrawTime { get; }

    /// <summary>
    ///     Gets the minimum frame time recorded
    /// </summary>
    double MinFrameTime { get; }

    /// <summary>
    ///     Gets the maximum frame time recorded
    /// </summary>
    double MaxFrameTime { get; }

    /// <summary>
    ///     Gets the current memory usage in MB
    /// </summary>
    double MemoryUsageMb { get; }

    /// <summary>
    ///     Gets the total number of frames processed
    /// </summary>
    long TotalFrames { get; }

    /// <summary>
    ///     Gets performance history for graphing
    /// </summary>
    IReadOnlyList<double> FrameTimeHistory { get; }

    /// <summary>
    ///     Gets update time history for graphing
    /// </summary>
    IReadOnlyList<double> UpdateTimeHistory { get; }

    /// <summary>
    ///     Gets draw time history for graphing
    /// </summary>
    IReadOnlyList<double> DrawTimeHistory { get; }

    /// <summary>
    ///     Gets FPS history for graphing
    /// </summary>
    IReadOnlyList<double> FpsHistory { get; }

    /// <summary>
    ///     Updates frame timing metrics
    /// </summary>
    /// <param name="frameTime">Frame time in milliseconds</param>
    void UpdateFrameTime(double frameTime);

    /// <summary>
    ///     Updates update timing metrics
    /// </summary>
    /// <param name="updateTime">Update time in milliseconds</param>
    void UpdateUpdateTime(double updateTime);

    /// <summary>
    ///     Updates draw timing metrics
    /// </summary>
    /// <param name="drawTime">Draw time in milliseconds</param>
    void UpdateDrawTime(double drawTime);

    /// <summary>
    ///     Resets all performance metrics
    /// </summary>
    void ResetMetrics();

    /// <summary>
    ///     Gets a summary of current performance metrics
    /// </summary>
    /// <returns>Dictionary of metric name to value</returns>
    Dictionary<string, object> GetMetricsSummary();
}