using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SquidVox.Core.Enums;
using SquidVox.Core.Interfaces.GameObjects;
using SquidVox.Core.Interfaces.Rendering;
using SquidVox.World3d.ImGUI;

namespace SquidVox.World3d.Rendering;

/// <summary>
/// Render layer for ImGui debug UI.
/// Always renders on top at the DebugUI layer priority.
/// </summary>
public class ImGuiRenderLayer : IRenderableLayer, IDisposable
{
    /// <summary>
    /// Gets the rendering layer priority.
    /// </summary>
    public RenderLayer Layer => RenderLayer.DebugUI;

    private readonly ImGuiRenderer _imGuiRenderer;

    private readonly Lock _addRemoveLock = new();

    private readonly List<ISVoxDebuggerGameObject> _debuggers = [];


    /// <summary>
    /// Gets or sets whether this layer is enabled for rendering.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to show the ImGui demo window.
    /// </summary>
    public bool ShowDemoWindow { get; set; }


    private GameTime _gameTime;

    /// <summary>
    /// Adds a debugger to the render layer.
    /// </summary>
    /// <typeparam name="T">The type of the debugger.</typeparam>
    /// <param name="debugger">The debugger to add.</param>
    public void AddDebugger<T>(T debugger) where T : ISVoxDebuggerGameObject
    {
        lock (_addRemoveLock)
        {
            _debuggers.Add(debugger);
        }
    }

    /// <summary>
    /// Removes a debugger from the render layer.
    /// </summary>
    /// <typeparam name="T">The type of the debugger.</typeparam>
    /// <param name="debugger">The debugger to remove.</param>
    /// <returns>True if the debugger was removed, false otherwise.</returns>
    public bool RemoveDebugger<T>(T debugger) where T : ISVoxDebuggerGameObject
    {
        lock (_addRemoveLock)
        {
            return _debuggers.Remove(debugger);
        }
    }


    /// <summary>
    /// Initializes a new instance of the ImGuiRenderLayer class.
    /// </summary>
    /// <param name="game">The game instance.</param>
    public ImGuiRenderLayer(Game game)
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
            ImGui.End();
        }

        if (ShowDemoWindow)
        {
            ImGui.ShowDemoWindow();
        }

        _imGuiRenderer.EndLayout();
    }

    /// <summary>
    /// Updates the render layer.
    /// </summary>
    /// <param name="gameTime">The game time.</param>
    public void Update(GameTime gameTime)
    {
        _gameTime = gameTime;
    }

    /// <summary>
    /// Gets or sets whether the render layer has input focus.
    /// </summary>
    public bool HasFocus { get; set; }

    /// <summary>
    /// Handles keyboard input.
    /// </summary>
    /// <param name="keyboardState">The keyboard state.</param>
    /// <param name="gameTime">The game time.</param>
    public void HandleKeyboard(KeyboardState keyboardState, GameTime gameTime)
    {
    }

    /// <summary>
    /// Handles mouse input.
    /// </summary>
    /// <param name="mouseState">The mouse state.</param>
    /// <param name="gameTime">The game time.</param>
    public void HandleMouse(MouseState mouseState, GameTime gameTime)
    {
    }

    public void Dispose()
    {
        _imGuiRenderer.Dispose();
        GC.SuppressFinalize(this);
    }
}
