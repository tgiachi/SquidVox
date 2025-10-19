using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SquidVox.Core.Enums;
using SquidVox.Core.Interfaces.Rendering;
using SquidVox.World3d.Context;

namespace SquidVox.World3d.Rendering;

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


    private GameTime _gameTime;


    /// <summary>
    /// Renders the ImGui debug UI.
    /// </summary>
    /// <param name="textureBatcher">TextureBatcher (not used by ImGui).</param>
    /// <param name="fontRenderer">Font renderer (not used by ImGui).</param>
    public void Render(SpriteBatch spriteBatch)
    {
        SquidVoxGraphicContext.ImGuiRenderer.BeginLayout(_gameTime);

        if (ShowDemoWindow)
        {
            ImGui.ShowDemoWindow();
        }

        SquidVoxGraphicContext.ImGuiRenderer.EndLayout();

    }

    public void Update(GameTime gameTime)
    {
        _gameTime = gameTime;

    }
}
