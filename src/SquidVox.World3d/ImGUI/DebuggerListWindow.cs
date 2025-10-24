using System.Collections.Generic;
using ImGuiNET;
using SquidVox.Core.Interfaces.GameObjects;

namespace SquidVox.World3d.ImGUI;

/// <summary>
/// A debugger window that displays a list of all available debuggers with toggle controls.
/// </summary>
public class DebuggerListWindow : ISVoxDebuggerGameObject
{
    private readonly List<ISVoxDebuggerGameObject> _debuggers;
    private bool _isVisible;

    /// <summary>
    /// Initializes a new instance of the DebuggerListWindow class.
    /// </summary>
    /// <param name="debuggers">The list of debuggers to display.</param>
    public DebuggerListWindow(List<ISVoxDebuggerGameObject> debuggers)
    {
        _debuggers = debuggers ?? throw new ArgumentNullException(nameof(debuggers));
        WindowTitle = "Debugger List";
        IsVisible = true; // Visible by default
    }

    /// <summary>
    /// Gets or sets whether the debugger window is visible.
    /// </summary>
    public bool IsVisible
    {
        get => _isVisible;
        set => _isVisible = value;
    }

    /// <summary>
    /// Gets the title of the debugger window.
    /// </summary>
    public string WindowTitle { get; }

    /// <summary>
    /// Draws the debugger list UI.
    /// </summary>
    public void Draw()
    {
        ImGui.Text("Available Debuggers");
        ImGui.Separator();

        if (_debuggers.Count == 0)
        {
            ImGui.TextDisabled("No debuggers available");
            return;
        }

        // Group controls
        ImGui.Spacing();
        if (ImGui.Button("Show All", new System.Numerics.Vector2(-1, 0)))
        {
            foreach (var debugger in _debuggers)
            {
                // Don't toggle self
                if (debugger != this)
                {
                    debugger.IsVisible = true;
                }
            }
        }

        if (ImGui.Button("Hide All", new System.Numerics.Vector2(-1, 0)))
        {
            foreach (var debugger in _debuggers)
            {
                // Don't toggle self
                if (debugger != this)
                {
                    debugger.IsVisible = false;
                }
            }
        }

        ImGui.Separator();
        ImGui.Spacing();

        // List all debuggers with checkboxes
        foreach (var debugger in _debuggers)
        {
            // Skip self to avoid recursion
            if (debugger == this)
            {
                continue;
            }

            var isVisible = debugger.IsVisible;
            var label = debugger.WindowTitle;

            // Add icon based on visibility
            var icon = isVisible ? "👁️" : "🚫";

            if (ImGui.Checkbox($"{icon}  {label}", ref isVisible))
            {
                debugger.IsVisible = isVisible;
            }

            // Show tooltip with debugger type
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text($"Type: {debugger.GetType().Name}");
                ImGui.Text($"Status: {(isVisible ? "Visible" : "Hidden")}");
                ImGui.EndTooltip();
            }
        }

        ImGui.Separator();
        ImGui.Spacing();

        // Statistics
        int visibleCount = 0;
        int totalCount = 0;

        foreach (var debugger in _debuggers)
        {
            if (debugger != this)
            {
                totalCount++;
                if (debugger.IsVisible)
                {
                    visibleCount++;
                }
            }
        }

        ImGui.TextDisabled($"Showing {visibleCount} of {totalCount} debuggers");
    }
}
