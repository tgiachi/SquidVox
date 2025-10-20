using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SquidVox.Core.Enums;
using SquidVox.Core.Interfaces.GameObjects;
using SquidVox.Core.Interfaces.Rendering;
using SquidVox.World3d.Context;
using SquidVox.World3d.ImGUI;

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

    private readonly ImGuiRenderer _imGuiRenderer;

    private readonly Lock _addRemoveLock = new();

    private readonly List<ISVoxDebuggerGameObject> _debuggers = new();


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
    ///
    /// </summary>
    public void AddDebugger<T>(T debugger) where T : ISVoxDebuggerGameObject
    {
        lock (_addRemoveLock)
        {
            _debuggers.Add(debugger);
        }
    }

    /// <summary>
    ///
    /// </summary>
    public bool RemoveDebugger<T>(T debugger) where T : ISVoxDebuggerGameObject
    {
        lock (_addRemoveLock)
        {
            return _debuggers.Remove(debugger);
        }
    }


    /// <summary>
    ///
    /// </summary>
    public ImGuiRenderLayer(Game game )
    {
        _imGuiRenderer = new ImGuiRenderer(game);
        _imGuiRenderer.RebuildFontAtlas();
    }

    /// <summary>
    /// Renders the ImGui debug UI.
    /// </summary>
    /// <param name="textureBatcher">TextureBatcher (not used by ImGui).</param>
    /// <param name="fontRenderer">Font renderer (not used by ImGui).</param>
    public void Render(SpriteBatch spriteBatch)
    {
        _imGuiRenderer.BeginLayout(_gameTime);

        lock (_addRemoveLock)
        {
            foreach (var debugger in _debuggers)
            {
                ImGui.Begin(debugger.WindowTitle);
                debugger.Draw();
            }
        }

        if (ShowDemoWindow)
        {
            ImGui.ShowDemoWindow();
        }

        _imGuiRenderer.EndLayout();

    }

    /// <summary>
    ///
    /// </summary>
    public void Update(GameTime gameTime)
    {
        _gameTime = gameTime;

    }

    public bool HasFocus { get; set; }
    public void HandleKeyboard(KeyboardState keyboardState, GameTime gameTime)
    {

    }

    public void HandleMouse(MouseState mouseState, GameTime gameTime)
    {
    }
}
