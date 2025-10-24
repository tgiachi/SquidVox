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

    private ImGuiTheme.ThemePreset _currentTheme = ImGuiTheme.ThemePreset.SquidVoxDark;

    private DebuggerListWindow? _debuggerListWindow;


    /// <summary>
    /// Gets or sets whether this layer is enabled for rendering.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to show the ImGui demo window.
    /// </summary>
    public bool ShowDemoWindow { get; set; }

    /// <summary>
    /// Gets or sets the current ImGui theme.
    /// Changing this will immediately apply the new theme.
    /// </summary>
    public ImGuiTheme.ThemePreset CurrentTheme
    {
        get => _currentTheme;
        set
        {
            _currentTheme = value;
            ImGuiTheme.ApplyTheme(_currentTheme);
        }
    }


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
    /// Binds a texture for use with ImGui image widgets.
    /// </summary>
    /// <param name="texture">The texture to bind.</param>
    /// <returns>An ImGui texture identifier.</returns>
    public IntPtr BindTexture(Texture2D texture)
    {
        return _imGuiRenderer.BindTexture(texture);
    }

    /// <summary>
    /// Releases a previously bound texture identifier.
    /// </summary>
    /// <param name="textureId">The ImGui texture identifier.</param>
    public void UnbindTexture(IntPtr textureId)
    {
        _imGuiRenderer.UnbindTexture(textureId);
    }

    /// <summary>
    /// Loads a custom font from a file path.
    /// Must be called before the first render.
    /// </summary>
    /// <param name="fontPath">The path to the font file (.ttf).</param>
    /// <param name="fontSize">The font size in pixels.</param>
    /// <returns>True if the font was loaded successfully, false otherwise.</returns>
    public bool LoadCustomFont(string fontPath, float fontSize = 16.0f)
    {
        var result = _imGuiRenderer.LoadCustomFont(fontPath, fontSize);
        if (result)
        {
            _imGuiRenderer.RebuildFontAtlas();
        }
        return result;
    }

    /// <summary>
    /// Loads a custom font from embedded bytes.
    /// Must be called before the first render.
    /// </summary>
    /// <param name="fontData">The font file data as bytes.</param>
    /// <param name="fontSize">The font size in pixels.</param>
    /// <returns>True if the font was loaded successfully, false otherwise.</returns>
    public bool LoadCustomFontFromMemory(byte[] fontData, float fontSize = 16.0f)
    {
        var result = _imGuiRenderer.LoadCustomFontFromMemory(fontData, fontSize);
        if (result)
        {
            _imGuiRenderer.RebuildFontAtlas();
        }
        return result;
    }


    /// <summary>
    /// Initializes a new instance of the ImGuiRenderLayer class.
    /// </summary>
    /// <param name="game">The game instance.</param>
    public ImGuiRenderLayer(Game game)
    {
        _imGuiRenderer = new ImGuiRenderer(game);
        _imGuiRenderer.RebuildFontAtlas();

        // Apply default theme
        ImGuiTheme.ApplyTheme(_currentTheme);

        // Create and add debugger list window
        _debuggerListWindow = new DebuggerListWindow(_debuggers);
        _debuggers.Add(_debuggerListWindow);
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
                if (debugger.IsVisible)
                {
                    ImGui.Begin(debugger.WindowTitle);
                    debugger.Draw();
                    ImGui.End();
                }
            }

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

    /// <summary>
    /// Gets all components in the layer.
    /// </summary>
    /// <returns>An enumerable of all components in the layer.</returns>
    public IEnumerable<ISVoxDebuggerGameObject> GetAllComponents()
    {
        lock (_addRemoveLock)
        {
            return _debuggers.ToArray();
        }
    }

    /// <summary>
    /// Gets the first component of the specified type from this layer.
    /// </summary>
    /// <typeparam name="T">The type of the component to get.</typeparam>
    /// <returns>The first component of the specified type if found, otherwise null.</returns>
    public T? GetComponent<T>() where T : class
    {
        lock (_addRemoveLock)
        {
            foreach (var debugger in _debuggers)
            {
                if (debugger is T component)
                {
                    return component;
                }
            }
        }

        return null;
    }

    public void Dispose()
    {
        _imGuiRenderer.Dispose();
        GC.SuppressFinalize(this);
    }
}
