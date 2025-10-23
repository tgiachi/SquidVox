using System.Numerics;
using ImGuiNET;
using SquidVox.Core.Interfaces.GameObjects;
using SquidVox.Core.Interfaces.Services;

namespace SquidVox.World3d.Scripts;

/// <summary>
///     ImGui window for displaying performance metrics and graphs
/// </summary>
public class PerformanceProfilerDebugger : ISVoxDebuggerGameObject
{
    private readonly IPerformanceProfilerService _profilerService;
    private bool _showDetails = true;
    private bool _showGraphs = true;
    private bool _showMemoryInfo = true;

    public PerformanceProfilerDebugger(IPerformanceProfilerService profilerService)
    {
        _profilerService = profilerService;
    }

    /// <summary>
    ///     Gets the window title
    /// </summary>
    public string WindowTitle => "Performance Profiler";

    /// <summary>
    ///     Draws the performance profiler window
    /// </summary>
    public void Draw()
    {
        var summary = _profilerService.GetMetricsSummary();

        // Main metrics display
        DrawMainMetrics(summary);

        ImGui.Separator();

        // Options
        ImGui.Checkbox("Show Details", ref _showDetails);
        ImGui.SameLine();
        ImGui.Checkbox("Show Graphs", ref _showGraphs);
        ImGui.SameLine();
        ImGui.Checkbox("Show Memory", ref _showMemoryInfo);

        if (ImGui.Button("Reset Metrics"))
        {
            _profilerService.ResetMetrics();
        }

        ImGui.Separator();

        // Detailed metrics
        if (_showDetails)
        {
            DrawDetailedMetrics(summary);
        }

        // Performance graphs
        if (_showGraphs)
        {
            DrawPerformanceGraphs();
        }

        // Memory information
        if (_showMemoryInfo)
        {
            DrawMemoryInfo(summary);
        }
    }

    private void DrawMainMetrics(Dictionary<string, object> summary)
    {
        // FPS Display with color coding
        var currentFps = (double)summary["Current FPS"];
        var fpsColor = GetFpsColor(currentFps);

        ImGui.TextColored(fpsColor, $"FPS: {currentFps:F1}");
        ImGui.SameLine();
        ImGui.Text($"(Avg: {summary["Average FPS"]:F1})");

        // Frame Time Display
        ImGui.Text($"Frame Time: {summary["Current Frame Time"]:F2}ms");
        ImGui.SameLine();
        ImGui.Text($"(Avg: {summary["Average Frame Time"]:F2}ms)");

        // Draw Time Display
        ImGui.Text($"Draw Time: {summary["Current Draw Time"]:F2}ms");
        ImGui.SameLine();
        ImGui.Text($"(Avg: {summary["Average Draw Time"]:F2}ms)");
    }

    private void DrawDetailedMetrics(Dictionary<string, object> summary)
    {
        if (ImGui.CollapsingHeader("Detailed Metrics"))
        {
            ImGui.Indent();

            ImGui.Text($"Min Frame Time: {summary["Min Frame Time"]:F2}ms");
            ImGui.Text($"Max Frame Time: {summary["Max Frame Time"]:F2}ms");
            ImGui.Text($"Total Frames: {summary["Total Frames"]}");

            // Frame time distribution
            var avgFrameTime = (double)summary["Average Frame Time"];
            var minFrameTime = (double)summary["Min Frame Time"];
            var maxFrameTime = (double)summary["Max Frame Time"];

            if (maxFrameTime > minFrameTime)
            {
                ImGui.Text("Frame Time Distribution:");
                ImGui.ProgressBar(
                    (float)((avgFrameTime - minFrameTime) / (maxFrameTime - minFrameTime)),
                    new Vector2(-1, 0),
                    $"{avgFrameTime:F2}ms"
                );
            }

            ImGui.Unindent();
        }
    }

    private void DrawPerformanceGraphs()
    {
        if (ImGui.CollapsingHeader("Performance Graphs"))
        {
            ImGui.Indent();

            // FPS Graph
            var fpsHistory = _profilerService.FpsHistory;
            if (fpsHistory.Count > 0)
            {
                ImGui.Text("FPS History");
                var fpsArray = fpsHistory.Select(x => (float)x).ToArray();
                var fpsMin = fpsArray.Length > 0 ? fpsArray.Min() : 0f;
                var fpsMax = fpsArray.Length > 0 ? fpsArray.Max() : 60f;

                ImGui.PlotLines(
                    "##FPS",
                    ref fpsArray[0],
                    fpsArray.Length,
                    0,
                    $"FPS: {fpsArray.LastOrDefault():F1}",
                    fpsMin * 0.9f,
                    fpsMax * 1.1f,
                    new Vector2(-1, 80)
                );
            }

            // Frame Time Graph
            var frameTimeHistory = _profilerService.FrameTimeHistory;
            if (frameTimeHistory.Count > 0)
            {
                ImGui.Text("Frame Time History");
                var frameTimeArray = frameTimeHistory.Select(x => (float)x).ToArray();

                ImGui.PlotLines(
                    "##FrameTime",
                    ref frameTimeArray[0],
                    frameTimeArray.Length,
                    0,
                    $"Frame Time: {frameTimeArray.LastOrDefault():F2}ms",
                    0f,
                    frameTimeArray.Max() * 1.1f,
                    new Vector2(-1, 80)
                );
            }

            // Draw Time Graph
            var drawTimeHistory = _profilerService.DrawTimeHistory;
            if (drawTimeHistory.Count > 0)
            {
                ImGui.Text("Draw Time History");
                var drawTimeArray = drawTimeHistory.Select(x => (float)x).ToArray();

                ImGui.PlotLines(
                    "##DrawTime",
                    ref drawTimeArray[0],
                    drawTimeArray.Length,
                    0,
                    $"Draw Time: {drawTimeArray.LastOrDefault():F2}ms",
                    0f,
                    drawTimeArray.Max() * 1.1f,
                    new Vector2(-1, 80)
                );
            }

            ImGui.Unindent();
        }
    }

    private void DrawMemoryInfo(Dictionary<string, object> summary)
    {
        if (ImGui.CollapsingHeader("Memory Information"))
        {
            ImGui.Indent();

            var memoryUsage = (double)summary["Memory Usage (MB)"];
            ImGui.Text($"Current Memory Usage: {memoryUsage:F2} MB");

            // Memory usage bar (assuming 1GB as reference)
            var memoryPercent = (float)(memoryUsage / 1024.0);
            var memoryColor = GetMemoryColor(memoryPercent);

            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, memoryColor);
            ImGui.ProgressBar(
                Math.Min(memoryPercent, 1.0f),
                new Vector2(-1, 0),
                $"{memoryUsage:F2} MB"
            );
            ImGui.PopStyleColor();

            if (ImGui.Button("Force GC"))
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            ImGui.Unindent();
        }
    }

    private static Vector4 GetFpsColor(double fps)
    {
        // Color code FPS: Green > 50, Yellow 30-50, Red < 30
        return fps switch
        {
            > 50 => new Vector4(0.0f, 1.0f, 0.0f, 1.0f), // Green
            > 30 => new Vector4(1.0f, 1.0f, 0.0f, 1.0f), // Yellow
            _    => new Vector4(1.0f, 0.0f, 0.0f, 1.0f)  // Red
        };
    }

    private static Vector4 GetMemoryColor(float memoryPercent)
    {
        // Color code memory: Green < 50%, Yellow 50-80%, Red > 80%
        return memoryPercent switch
        {
            < 0.5f => new Vector4(0.0f, 1.0f, 0.0f, 1.0f), // Green
            < 0.8f => new Vector4(1.0f, 1.0f, 0.0f, 1.0f), // Yellow
            _      => new Vector4(1.0f, 0.0f, 0.0f, 1.0f)  // Red
        };
    }
}