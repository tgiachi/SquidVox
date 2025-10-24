using ImGuiNET;
using SquidVox.Core.Interfaces.GameObjects;
using SquidVox.World3d.Rendering;

namespace SquidVox.World3d.ImGUI;

/// <summary>
/// Debugger window for selecting and applying ImGui themes.
/// </summary>
public class ThemeSelectorDebugger : ISVoxDebuggerGameObject
{
    private readonly ImGuiRenderLayer _renderLayer;

    /// <summary>
    /// Initializes a new instance of the ThemeSelectorDebugger class.
    /// </summary>
    /// <param name="renderLayer">The ImGui render layer to apply themes to.</param>
    public ThemeSelectorDebugger(ImGuiRenderLayer renderLayer)
    {
        _renderLayer = renderLayer ?? throw new ArgumentNullException(nameof(renderLayer));
        WindowTitle = "Theme Selector";
        IsVisible = false;
    }

    /// <summary>
    /// Gets or sets whether the debugger window is visible.
    /// </summary>
    public bool IsVisible { get; set; }

    /// <summary>
    /// Gets the title of the debugger window.
    /// </summary>
    public string WindowTitle { get; }

    /// <summary>
    /// Draws the theme selector UI.
    /// </summary>
    public void Draw()
    {
        ImGui.Text("Select ImGui Theme");
        ImGui.Separator();

        var currentTheme = _renderLayer.CurrentTheme;
        var themeNames = Enum.GetNames<ImGuiTheme.ThemePreset>();
        var currentIndex = (int)currentTheme;

        ImGui.Text($"Current Theme: {currentTheme}");
        ImGui.Spacing();

        // Theme buttons
        foreach (var themeName in themeNames)
        {
            var themeValue = Enum.Parse<ImGuiTheme.ThemePreset>(themeName);
            var isSelected = currentTheme == themeValue;

            if (isSelected)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive]);
            }

            if (ImGui.Button(GetThemeDisplayName(themeValue), new System.Numerics.Vector2(-1, 30)))
            {
                _renderLayer.CurrentTheme = themeValue;
            }

            if (isSelected)
            {
                ImGui.PopStyleColor();
            }

            // Show theme description
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text(GetThemeDescription(themeValue));
                ImGui.EndTooltip();
            }

            ImGui.Spacing();
        }

        ImGui.Separator();
        ImGui.TextWrapped("Click on a theme to apply it instantly. Changes are applied globally to all ImGui windows.");
    }

    /// <summary>
    /// Gets a user-friendly display name for the theme.
    /// </summary>
    private static string GetThemeDisplayName(ImGuiTheme.ThemePreset theme)
    {
        return theme switch
        {
            ImGuiTheme.ThemePreset.Dark => "🌙 Dark (Classic)",
            ImGuiTheme.ThemePreset.Light => "☀️ Light",
            ImGuiTheme.ThemePreset.SquidVoxDark => "🎮 SquidVox Dark Blue",
            ImGuiTheme.ThemePreset.SquidVoxOceanic => "🌊 SquidVox Oceanic",
            ImGuiTheme.ThemePreset.HighContrast => "⚡ High Contrast",
            _ => theme.ToString()
        };
    }

    /// <summary>
    /// Gets a description for the theme.
    /// </summary>
    private static string GetThemeDescription(ImGuiTheme.ThemePreset theme)
    {
        return theme switch
        {
            ImGuiTheme.ThemePreset.Dark => "Classic ImGui dark theme with neutral gray tones",
            ImGuiTheme.ThemePreset.Light => "Light theme with bright background for daytime use",
            ImGuiTheme.ThemePreset.SquidVoxDark => "Custom dark theme with blue accents matching SquidVox branding",
            ImGuiTheme.ThemePreset.SquidVoxOceanic => "Deep oceanic theme with cyan/teal accents for a underwater feel",
            ImGuiTheme.ThemePreset.HighContrast => "High contrast theme with bright colors for maximum visibility",
            _ => "Custom theme"
        };
    }
}
