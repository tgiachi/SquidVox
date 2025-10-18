using FontStashSharp.Interfaces;
using ImGuiNET;
using SquidVox.Core.Enums;
using SquidVox.Core.Interfaces.Rendering;
using SquidVox.World.Context;
using TrippyGL;

namespace SquidVox.World.Rendering;

/// <summary>
/// Render layer for ImGui debug UI.
/// Always renders on top at the DebugUI layer priority.
/// </summary>
public class ImGuiRenderLayer : IRenderableLayer
{
    /// <summary>
    /// Gets the rendering layer priority.
    /// </summary>
    public RenderLayer Layer => RenderLayer.DebugUI;

    /// <summary>
    /// Gets or sets whether this layer is enabled for rendering.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to show the ImGui demo window.
    /// </summary>
    public bool ShowDemoWindow { get; set; } = true;

    private float _deltaTime;

    /// <summary>
    /// Updates the ImGui layer with the current delta time.
    /// Call this before rendering to ensure proper ImGui state.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last frame in seconds.</param>
    public void Update(float deltaTime)
    {
        _deltaTime = deltaTime;
    }

    /// <summary>
    /// Renders the ImGui debug UI.
    /// </summary>
    /// <param name="textureBatcher">TextureBatcher (not used by ImGui).</param>
    /// <param name="fontRenderer">Font renderer (not used by ImGui).</param>
    public void Render(TextureBatcher textureBatcher, IFontStashRenderer fontRenderer)
    {
        SquidVoxGraphicContext.ImGuiController.Update(_deltaTime);

        if (ShowDemoWindow)
        {
            ImGui.ShowDemoWindow();
        }

        SquidVoxGraphicContext.ImGuiController.Render();
    }
}
