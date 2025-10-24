using System.IO;
using ImGuiNET;
using SquidVox.Core.Interfaces.GameObjects;
using SquidVox.World3d.Rendering;

namespace SquidVox.World3d.ImGUI;

/// <summary>
/// Debugger window for selecting and applying ImGui themes and fonts.
/// </summary>
public class ThemeSelectorDebugger : ISVoxDebuggerGameObject
{
    private readonly ImGuiRenderLayer _renderLayer;
    private string _fontPath = "";
    private float _fontSize = 16.0f;
    private string _fontStatus = "Using default ImGui font";

    // Common font paths for different systems
    private readonly string[] _commonFontPaths = new[]
    {
        // Windows
        "C:/Windows/Fonts/segoeui.ttf",
        "C:/Windows/Fonts/arial.ttf",
        "C:/Windows/Fonts/consola.ttf",
        "C:/Windows/Fonts/courbd.ttf",

        // macOS
        "/System/Library/Fonts/Helvetica.ttc",
        "/System/Library/Fonts/SFNSDisplay.ttf",
        "/Library/Fonts/Arial.ttf",
        "/System/Library/Fonts/Menlo.ttc",

        // Linux
        "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
        "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
        "/usr/share/fonts/truetype/ubuntu/Ubuntu-R.ttf",
        "/usr/share/fonts/truetype/noto/NotoSans-Regular.ttf"
    };

    /// <summary>
    /// Initializes a new instance of the ThemeSelectorDebugger class.
    /// </summary>
    /// <param name="renderLayer">The ImGui render layer to apply themes to.</param>
    public ThemeSelectorDebugger(ImGuiRenderLayer renderLayer)
    {
        _renderLayer = renderLayer ?? throw new ArgumentNullException(nameof(renderLayer));
        WindowTitle = "Theme & Font Selector";
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
    /// Draws the theme and font selector UI.
    /// </summary>
    public void Draw()
    {
        if (ImGui.BeginTabBar("ThemeFontTabs"))
        {
            if (ImGui.BeginTabItem("🎨 Themes"))
            {
                DrawThemeSelector();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("🔤 Fonts"))
            {
                DrawFontSelector();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    /// <summary>
    /// Draws the theme selector section.
    /// </summary>
    private void DrawThemeSelector()
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
    /// Draws the font selector section.
    /// </summary>
    private void DrawFontSelector()
    {
        ImGui.Text("Custom Font Settings");
        ImGui.Separator();

        // Status
        ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.9f, 0.7f, 1.0f), _fontStatus);
        ImGui.Spacing();

        // Font path input
        ImGui.Text("Font Path:");
        ImGui.InputText("##fontpath", ref _fontPath, 512);

        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text("Enter the path to a .ttf font file");
            ImGui.EndTooltip();
        }

        // Font size slider
        ImGui.Text($"Font Size: {_fontSize:F1}px");
        if (ImGui.SliderFloat("##fontsize", ref _fontSize, 8.0f, 32.0f, "%.1f"))
        {
            // Clamp to reasonable values
            _fontSize = Math.Clamp(_fontSize, 8.0f, 32.0f);
        }

        ImGui.Spacing();

        // Apply button
        if (ImGui.Button("Apply Font", new System.Numerics.Vector2(-1, 30)))
        {
            if (string.IsNullOrWhiteSpace(_fontPath))
            {
                _fontStatus = "❌ Error: Font path is empty";
            }
            else if (!File.Exists(_fontPath))
            {
                _fontStatus = $"❌ Error: Font file not found at '{_fontPath}'";
            }
            else
            {
                var success = _renderLayer.LoadCustomFont(_fontPath, _fontSize);
                if (success)
                {
                    _fontStatus = $"✅ Font loaded: {Path.GetFileName(_fontPath)} ({_fontSize}px)";
                }
                else
                {
                    _fontStatus = "❌ Error: Failed to load font";
                }
            }
        }

        ImGui.Spacing();
        ImGui.Separator();

        // Reset to default
        if (ImGui.Button("Reset to Default Font", new System.Numerics.Vector2(-1, 30)))
        {
            _renderLayer.LoadCustomFont("", 16.0f); // Empty path triggers default
            _fontStatus = "✅ Reset to default ImGui font";
            _fontPath = "";
            _fontSize = 16.0f;
        }

        ImGui.Spacing();
        ImGui.Separator();

        // Common fonts section
        ImGui.Text("Common System Fonts:");
        ImGui.Spacing();

        if (ImGui.BeginChild("CommonFonts", new System.Numerics.Vector2(0, 200), ImGuiChildFlags.Borders))
        {
            foreach (var fontPath in _commonFontPaths)
            {
                if (File.Exists(fontPath))
                {
                    var fontName = Path.GetFileNameWithoutExtension(fontPath);
                    var isCurrentFont = _fontPath == fontPath;

                    if (isCurrentFont)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive]);
                    }

                    if (ImGui.Button($"📄 {fontName}###{fontPath}", new System.Numerics.Vector2(-1, 25)))
                    {
                        _fontPath = fontPath;
                    }

                    if (isCurrentFont)
                    {
                        ImGui.PopStyleColor();
                    }

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text(fontPath);
                        ImGui.EndTooltip();
                    }
                }
            }
        }
        ImGui.EndChild();

        ImGui.Spacing();
        ImGui.TextWrapped("⚠️ Note: Font changes require rebuilding the font atlas and will affect all ImGui windows.");
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
