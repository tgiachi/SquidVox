using System.Numerics;
using ImGuiNET;

namespace SquidVox.World3d.ImGUI;

/// <summary>
/// Provides custom ImGui themes and styling utilities.
/// </summary>
public static class ImGuiTheme
{
    /// <summary>
    /// Available theme presets.
    /// </summary>
    public enum ThemePreset
    {
        /// <summary>
        /// Default ImGui dark theme.
        /// </summary>
        Dark,

        /// <summary>
        /// ImGui light theme.
        /// </summary>
        Light,

        /// <summary>
        /// Custom SquidVox dark blue theme.
        /// </summary>
        SquidVoxDark,

        /// <summary>
        /// Custom SquidVox oceanic theme.
        /// </summary>
        SquidVoxOceanic,

        /// <summary>
        /// High contrast dark theme for better visibility.
        /// </summary>
        HighContrast
    }

    /// <summary>
    /// Applies the specified theme preset to ImGui.
    /// </summary>
    /// <param name="preset">The theme preset to apply.</param>
    public static void ApplyTheme(ThemePreset preset)
    {
        switch (preset)
        {
            case ThemePreset.Dark:
                ApplyDarkTheme();
                break;
            case ThemePreset.Light:
                ApplyLightTheme();
                break;
            case ThemePreset.SquidVoxDark:
                ApplySquidVoxDarkTheme();
                break;
            case ThemePreset.SquidVoxOceanic:
                ApplySquidVoxOceanicTheme();
                break;
            case ThemePreset.HighContrast:
                ApplyHighContrastTheme();
                break;
        }

        ApplyCommonStyle();
    }

    /// <summary>
    /// Applies common style settings (padding, rounding, etc.) to all themes.
    /// </summary>
    private static void ApplyCommonStyle()
    {
        var style = ImGui.GetStyle();

        // Padding & Spacing
        style.WindowPadding = new Vector2(10, 10);
        style.FramePadding = new Vector2(8, 4);
        style.ItemSpacing = new Vector2(8, 6);
        style.ItemInnerSpacing = new Vector2(6, 4);
        style.IndentSpacing = 20.0f;
        style.ScrollbarSize = 16.0f;
        style.GrabMinSize = 12.0f;

        // Rounding
        style.WindowRounding = 6.0f;
        style.ChildRounding = 4.0f;
        style.FrameRounding = 4.0f;
        style.PopupRounding = 4.0f;
        style.ScrollbarRounding = 8.0f;
        style.GrabRounding = 3.0f;
        style.TabRounding = 4.0f;

        // Borders
        style.WindowBorderSize = 1.0f;
        style.FrameBorderSize = 1.0f;
        style.PopupBorderSize = 1.0f;
        style.TabBorderSize = 0.0f;

        // Alignment
        style.WindowTitleAlign = new Vector2(0.5f, 0.5f); // Center window titles
        style.ButtonTextAlign = new Vector2(0.5f, 0.5f); // Center button text
    }

    /// <summary>
    /// Applies the default dark theme (ImGui classic dark).
    /// </summary>
    private static void ApplyDarkTheme()
    {
        ImGui.StyleColorsDark();
    }

    /// <summary>
    /// Applies a light theme.
    /// </summary>
    private static void ApplyLightTheme()
    {
        ImGui.StyleColorsLight();
    }

    /// <summary>
    /// Applies a custom SquidVox dark blue theme.
    /// </summary>
    private static void ApplySquidVoxDarkTheme()
    {
        var colors = ImGui.GetStyle().Colors;

        // Background colors
        colors[(int)ImGuiCol.WindowBg] = new Vector4(0.10f, 0.12f, 0.15f, 1.00f);
        colors[(int)ImGuiCol.ChildBg] = new Vector4(0.08f, 0.10f, 0.13f, 1.00f);
        colors[(int)ImGuiCol.PopupBg] = new Vector4(0.10f, 0.12f, 0.15f, 0.98f);
        colors[(int)ImGuiCol.Border] = new Vector4(0.20f, 0.25f, 0.30f, 0.60f);
        colors[(int)ImGuiCol.BorderShadow] = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);

        // Frame colors
        colors[(int)ImGuiCol.FrameBg] = new Vector4(0.15f, 0.18f, 0.22f, 1.00f);
        colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.20f, 0.25f, 0.30f, 1.00f);
        colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.25f, 0.30f, 0.35f, 1.00f);

        // Title bar
        colors[(int)ImGuiCol.TitleBg] = new Vector4(0.12f, 0.15f, 0.18f, 1.00f);
        colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.15f, 0.20f, 0.25f, 1.00f);
        colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(0.08f, 0.10f, 0.13f, 1.00f);

        // Menu bar
        colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.12f, 0.15f, 0.18f, 1.00f);

        // Scrollbar
        colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.08f, 0.10f, 0.13f, 1.00f);
        colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.25f, 0.30f, 0.35f, 1.00f);
        colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.30f, 0.35f, 0.40f, 1.00f);
        colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0.35f, 0.40f, 0.45f, 1.00f);

        // Check mark & Slider
        colors[(int)ImGuiCol.CheckMark] = new Vector4(0.30f, 0.60f, 0.90f, 1.00f);
        colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.30f, 0.60f, 0.90f, 1.00f);
        colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.40f, 0.70f, 1.00f, 1.00f);

        // Buttons
        colors[(int)ImGuiCol.Button] = new Vector4(0.20f, 0.40f, 0.70f, 1.00f);
        colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.30f, 0.50f, 0.80f, 1.00f);
        colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.40f, 0.60f, 0.90f, 1.00f);

        // Header
        colors[(int)ImGuiCol.Header] = new Vector4(0.20f, 0.40f, 0.70f, 0.80f);
        colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.30f, 0.50f, 0.80f, 1.00f);
        colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.40f, 0.60f, 0.90f, 1.00f);

        // Separator
        colors[(int)ImGuiCol.Separator] = new Vector4(0.20f, 0.25f, 0.30f, 1.00f);
        colors[(int)ImGuiCol.SeparatorHovered] = new Vector4(0.30f, 0.50f, 0.80f, 1.00f);
        colors[(int)ImGuiCol.SeparatorActive] = new Vector4(0.40f, 0.60f, 0.90f, 1.00f);

        // Resize grip
        colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.20f, 0.40f, 0.70f, 0.50f);
        colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.30f, 0.50f, 0.80f, 0.80f);
        colors[(int)ImGuiCol.ResizeGripActive] = new Vector4(0.40f, 0.60f, 0.90f, 1.00f);

        // Text
        colors[(int)ImGuiCol.Text] = new Vector4(0.95f, 0.95f, 0.95f, 1.00f);
        colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.50f, 0.50f, 0.50f, 1.00f);
        colors[(int)ImGuiCol.TextSelectedBg] = new Vector4(0.30f, 0.50f, 0.80f, 0.50f);
    }

    /// <summary>
    /// Applies a custom SquidVox oceanic theme (cyan/teal accents).
    /// </summary>
    private static void ApplySquidVoxOceanicTheme()
    {
        var colors = ImGui.GetStyle().Colors;

        // Background colors - deep ocean tones
        colors[(int)ImGuiCol.WindowBg] = new Vector4(0.08f, 0.12f, 0.16f, 1.00f);
        colors[(int)ImGuiCol.ChildBg] = new Vector4(0.06f, 0.10f, 0.14f, 1.00f);
        colors[(int)ImGuiCol.PopupBg] = new Vector4(0.08f, 0.12f, 0.16f, 0.98f);
        colors[(int)ImGuiCol.Border] = new Vector4(0.15f, 0.30f, 0.35f, 0.60f);
        colors[(int)ImGuiCol.BorderShadow] = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);

        // Frame colors
        colors[(int)ImGuiCol.FrameBg] = new Vector4(0.12f, 0.18f, 0.22f, 1.00f);
        colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.18f, 0.25f, 0.30f, 1.00f);
        colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.22f, 0.30f, 0.35f, 1.00f);

        // Title bar - ocean blue
        colors[(int)ImGuiCol.TitleBg] = new Vector4(0.10f, 0.15f, 0.20f, 1.00f);
        colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.12f, 0.22f, 0.30f, 1.00f);
        colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(0.06f, 0.10f, 0.14f, 1.00f);

        // Menu bar
        colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.10f, 0.15f, 0.20f, 1.00f);

        // Scrollbar
        colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.06f, 0.10f, 0.14f, 1.00f);
        colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.20f, 0.40f, 0.50f, 1.00f);
        colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.25f, 0.45f, 0.55f, 1.00f);
        colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0.30f, 0.50f, 0.60f, 1.00f);

        // Check mark & Slider - cyan/teal accents
        colors[(int)ImGuiCol.CheckMark] = new Vector4(0.20f, 0.70f, 0.80f, 1.00f);
        colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.20f, 0.70f, 0.80f, 1.00f);
        colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.30f, 0.80f, 0.90f, 1.00f);

        // Buttons - teal
        colors[(int)ImGuiCol.Button] = new Vector4(0.15f, 0.50f, 0.60f, 1.00f);
        colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.20f, 0.60f, 0.70f, 1.00f);
        colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.25f, 0.70f, 0.80f, 1.00f);

        // Header
        colors[(int)ImGuiCol.Header] = new Vector4(0.15f, 0.50f, 0.60f, 0.80f);
        colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.20f, 0.60f, 0.70f, 1.00f);
        colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.25f, 0.70f, 0.80f, 1.00f);

        // Separator
        colors[(int)ImGuiCol.Separator] = new Vector4(0.15f, 0.30f, 0.35f, 1.00f);
        colors[(int)ImGuiCol.SeparatorHovered] = new Vector4(0.20f, 0.60f, 0.70f, 1.00f);
        colors[(int)ImGuiCol.SeparatorActive] = new Vector4(0.25f, 0.70f, 0.80f, 1.00f);

        // Resize grip
        colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.15f, 0.50f, 0.60f, 0.50f);
        colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.20f, 0.60f, 0.70f, 0.80f);
        colors[(int)ImGuiCol.ResizeGripActive] = new Vector4(0.25f, 0.70f, 0.80f, 1.00f);

        // Text
        colors[(int)ImGuiCol.Text] = new Vector4(0.92f, 0.95f, 0.97f, 1.00f);
        colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.50f, 0.55f, 0.60f, 1.00f);
        colors[(int)ImGuiCol.TextSelectedBg] = new Vector4(0.20f, 0.60f, 0.70f, 0.50f);
    }

    /// <summary>
    /// Applies a high contrast dark theme for better visibility.
    /// </summary>
    private static void ApplyHighContrastTheme()
    {
        var colors = ImGui.GetStyle().Colors;

        // Background colors - very dark
        colors[(int)ImGuiCol.WindowBg] = new Vector4(0.05f, 0.05f, 0.05f, 1.00f);
        colors[(int)ImGuiCol.ChildBg] = new Vector4(0.03f, 0.03f, 0.03f, 1.00f);
        colors[(int)ImGuiCol.PopupBg] = new Vector4(0.05f, 0.05f, 0.05f, 0.98f);
        colors[(int)ImGuiCol.Border] = new Vector4(0.60f, 0.60f, 0.60f, 0.80f);
        colors[(int)ImGuiCol.BorderShadow] = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);

        // Frame colors - dark gray
        colors[(int)ImGuiCol.FrameBg] = new Vector4(0.15f, 0.15f, 0.15f, 1.00f);
        colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.25f, 0.25f, 0.25f, 1.00f);
        colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.35f, 0.35f, 0.35f, 1.00f);

        // Title bar
        colors[(int)ImGuiCol.TitleBg] = new Vector4(0.10f, 0.10f, 0.10f, 1.00f);
        colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.20f, 0.20f, 0.20f, 1.00f);
        colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(0.05f, 0.05f, 0.05f, 1.00f);

        // Menu bar
        colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.10f, 0.10f, 0.10f, 1.00f);

        // Scrollbar
        colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.05f, 0.05f, 0.05f, 1.00f);
        colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.40f, 0.40f, 0.40f, 1.00f);
        colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.50f, 0.50f, 0.50f, 1.00f);
        colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0.60f, 0.60f, 0.60f, 1.00f);

        // Check mark & Slider - bright white/yellow
        colors[(int)ImGuiCol.CheckMark] = new Vector4(1.00f, 1.00f, 0.00f, 1.00f);
        colors[(int)ImGuiCol.SliderGrab] = new Vector4(1.00f, 1.00f, 0.00f, 1.00f);
        colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(1.00f, 1.00f, 0.40f, 1.00f);

        // Buttons - bright
        colors[(int)ImGuiCol.Button] = new Vector4(0.30f, 0.30f, 0.30f, 1.00f);
        colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.50f, 0.50f, 0.50f, 1.00f);
        colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.70f, 0.70f, 0.70f, 1.00f);

        // Header
        colors[(int)ImGuiCol.Header] = new Vector4(0.30f, 0.30f, 0.30f, 0.80f);
        colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.50f, 0.50f, 0.50f, 1.00f);
        colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.70f, 0.70f, 0.70f, 1.00f);

        // Separator
        colors[(int)ImGuiCol.Separator] = new Vector4(0.60f, 0.60f, 0.60f, 1.00f);
        colors[(int)ImGuiCol.SeparatorHovered] = new Vector4(0.80f, 0.80f, 0.00f, 1.00f);
        colors[(int)ImGuiCol.SeparatorActive] = new Vector4(1.00f, 1.00f, 0.00f, 1.00f);

        // Resize grip
        colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.60f, 0.60f, 0.60f, 0.50f);
        colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.80f, 0.80f, 0.00f, 0.80f);
        colors[(int)ImGuiCol.ResizeGripActive] = new Vector4(1.00f, 1.00f, 0.00f, 1.00f);

        // Text - very bright
        colors[(int)ImGuiCol.Text] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
        colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.60f, 0.60f, 0.60f, 1.00f);
        colors[(int)ImGuiCol.TextSelectedBg] = new Vector4(1.00f, 1.00f, 0.00f, 0.50f);
    }
}
