using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SquidVox.Core.Context;
using SquidVox.Core.GameObjects;
using SquidVox.Core.Interfaces.Services;

namespace SquidVox.GameObjects.UI.Controls;

/// <summary>
/// Provides a Quake-style drop-down console for command entry and history browsing.
/// </summary>
public sealed class QuakeConsoleGameObject : Base2dGameObject, IDisposable
{
    private const int ConsoleHeight = 320;
    private const float DefaultAnimationSpeed = 700f;
    private const float DefaultLineSpacing = 4f;
    private readonly List<ConsoleEntry> _entries = [];
    private readonly List<string> _history = [];
    private readonly List<string> _autoCompleteSuggestions = [];
    private IInputManager? _inputManager;
    private bool _isAnimating;
    private bool _isInitialized;
    private bool _isOpen;
    private bool _ownsFocus;
    private DynamicSpriteFont? _font;
    private string _inputBuffer = string.Empty;
    private KeyboardState _previousKeyboardState;
    private int _historyIndex = -1;
    private string _historyOriginal = string.Empty;
    private int _autoCompleteIndex = -1;
    private float _currentY = -ConsoleHeight;
    private float _targetY = -ConsoleHeight;
    private float _lineHeight = 16f;
    private int _maxLines = 1;

    /// <summary>
    /// Occurs when the user submits a command.
    /// </summary>
    public event EventHandler<string>? CommandSubmitted;

    /// <summary>
    /// Gets or sets the animation speed in pixels per second.
    /// </summary>
    public float AnimationSpeed { get; set; } = DefaultAnimationSpeed;

    /// <summary>
    /// Gets or sets the console background colour.
    /// </summary>
    public Color BackgroundColor { get; set; } = new(0, 0, 0, 200);

    /// <summary>
    /// Gets or sets the default foreground colour.
    /// </summary>
    public Color ForegroundColor { get; set; } = Color.White;

    /// <summary>
    /// Gets a modifiable list of welcome lines shown at initialisation.
    /// </summary>
    public List<string> WelcomeLines { get; } = new();

    /// <summary>
    /// Gets or sets the command prompt prefix.
    /// </summary>
    public string Prompt { get; set; } = "> ";

    /// <summary>
    /// Gets or sets the toggle key binding string.
    /// </summary>
    public string ToggleBinding { get; set; } = "OemTilde";

    /// <summary>
    /// Gets or sets the caret blink interval in seconds.
    /// </summary>
    public float CaretBlinkInterval { get; set; } = 0.5f;

    /// <summary>
    /// Gets or sets the delegate for auto-complete suggestions.
    /// </summary>
    public Func<string, IEnumerable<string>>? GetAutoCompleteSuggestions { get; set; }

    /// <summary>
    /// Initializes the console resources.
    /// </summary>
    /// <param name="assetManagerService">The asset manager service.</param>
    /// <param name="inputManager">The input manager service.</param>
    public void Initialize(IAssetManagerService assetManagerService, IInputManager inputManager)
    {
        if (_isInitialized)
        {
            return;
        }

        _inputManager = inputManager;
        _inputManager.BindKey(ToggleBinding, ToggleConsole);

        try
        {
            _font = assetManagerService.GetFont("Monocraft", 16);
        }
        catch
        {
            _font = null;
        }

        if (_font != null)
        {
            _lineHeight = _font.LineHeight + DefaultLineSpacing;
        }

        RecalculateLayout();
        foreach (var line in WelcomeLines)
        {
            AddLine(line, Color.LightGreen);
        }

        _previousKeyboardState = Keyboard.GetState();
        IsVisible = false;
        _isInitialized = true;
    }

    /// <summary>
    /// Adds a console line using the default colours.
    /// </summary>
    /// <param name="text">The line text.</param>
    public void AddLine(string text)
    {
        AddLine(text, ForegroundColor);
    }

    /// <summary>
    /// Adds a console line using a custom foreground colour.
    /// </summary>
    /// <param name="text">The line text.</param>
    /// <param name="foreground">The foreground colour.</param>
    public void AddLine(string text, Color foreground)
    {
        AddLine(text, foreground, null);
    }

    /// <summary>
    /// Adds a console line using custom colours.
    /// </summary>
    /// <param name="text">The line text.</param>
    /// <param name="foreground">The foreground colour.</param>
    /// <param name="background">The optional background colour.</param>
    public void AddLine(string text, Color foreground, Color? background)
    {
        if (_entries.Count >= _maxLines)
        {
            _entries.RemoveAt(0);
        }

        _entries.Add(new ConsoleEntry(text, foreground, background));
    }

    /// <summary>
    /// Toggles the console visibility.
    /// </summary>
    public void ToggleConsole()
    {
        if (_isAnimating)
        {
            return;
        }

        if (_isOpen)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    /// <summary>
    /// Shows the console.
    /// </summary>
    public void Show()
    {
        if (_isOpen || _isAnimating)
        {
            return;
        }

        CaptureFocus();
        _targetY = 0f;
        _isOpen = true;
        _isAnimating = true;
        IsVisible = true;
    }

    /// <summary>
    /// Hides the console.
    /// </summary>
    public void Hide()
    {
        if (!_isOpen || _isAnimating)
        {
            return;
        }

        _targetY = -ConsoleHeight;
        _isOpen = false;
        _isAnimating = true;
        ReleaseFocus();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_inputManager != null)
        {
            _inputManager.UnbindKey(ToggleBinding);
        }

        ReleaseFocus();
        _entries.Clear();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    protected override void OnUpdate(GameTime gameTime)
    {
        if (!_isInitialized)
        {
            return;
        }

        if (_isAnimating)
        {
            var delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var step = AnimationSpeed * delta;

            if (_currentY < _targetY)
            {
                _currentY = Math.Min(_currentY + step, _targetY);
            }
            else if (_currentY > _targetY)
            {
                _currentY = Math.Max(_currentY - step, _targetY);
            }

            Position = new Vector2(0f, _currentY);

            if (Math.Abs(_currentY - _targetY) < 0.5f)
            {
                _currentY = _targetY;
                Position = new Vector2(0f, _currentY);
                _isAnimating = false;

                if (!_isOpen)
                {
                    IsVisible = false;
                }
            }
        }
    }

    /// <inheritdoc />
    protected override void OnRender(SpriteBatch spriteBatch)
    {
        if (!_isInitialized || !IsVisible)
        {
            return;
        }

        var viewport = SquidVoxEngineContext.GraphicsDevice.Viewport;
        var backgroundRect = new Rectangle(0, (int)_currentY, viewport.Width, ConsoleHeight);
        spriteBatch.Draw(SquidVoxEngineContext.WhitePixel, backgroundRect, BackgroundColor);

        if (_font == null)
        {
            return;
        }

        var lineY = (int)_currentY + 10;
        for (var i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            var textPosition = new Vector2(10f, lineY);

            if (entry.Background.HasValue)
            {
                var measure = _font.MeasureString(entry.Text);
                var rect = new Rectangle(
                    (int)textPosition.X - 2,
                    (int)textPosition.Y - 2,
                    (int)measure.X + 4,
                    (int)measure.Y + 4
                );
                spriteBatch.Draw(SquidVoxEngineContext.WhitePixel, rect, entry.Background.Value);
            }

            spriteBatch.DrawString(_font, entry.Text, textPosition, entry.Foreground);
            lineY += (int)_lineHeight;
        }

        var inputText = Prompt + _inputBuffer;
        var inputPosition = new Vector2(10f, (int)_currentY + ConsoleHeight - 25f);
        spriteBatch.DrawString(_font, inputText, inputPosition, ForegroundColor);

        if (ShouldShowCaret())
        {
            var measure = _font.MeasureString(inputText);
            var caretPosition = new Vector2(inputPosition.X + measure.X, inputPosition.Y);
            spriteBatch.DrawString(_font, "_", caretPosition, ForegroundColor);
        }
    }

    /// <inheritdoc />
    protected override void OnHandleKeyboard(KeyboardState keyboardState, GameTime gameTime)
    {
        if (!_isInitialized)
        {
            return;
        }

        var justPressed = GetJustPressedKeys(keyboardState, _previousKeyboardState);

        foreach (var key in justPressed)
        {
            if (key == Keys.Enter)
            {
                SubmitCommand();
            }
            else if (key == Keys.Back && _inputBuffer.Length > 0)
            {
                 _inputBuffer = _inputBuffer[..^1];
                 ResetHistoryNavigation();
                 ResetAutoComplete();
            }
            else if (key == Keys.Up)
            {
                TraverseHistory(true);
            }
            else if (key == Keys.Down)
            {
                TraverseHistory(false);
            }
            else if (key == Keys.Tab)
            {
                HandleAutoComplete();
            }
            else
            {
                var shift = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);
                var value = ConvertKeyToString(key, shift);
                if (!string.IsNullOrEmpty(value))
                {
                     _inputBuffer += value;
                     ResetHistoryNavigation();
                     ResetAutoComplete();
                }
            }
        }

        _previousKeyboardState = keyboardState;
    }

    private void CaptureFocus()
    {
        if (_inputManager != null && !_ownsFocus)
        {
            _inputManager.PushFocusStack(this);
            _ownsFocus = true;
        }
    }

    private void ReleaseFocus()
    {
        if (_inputManager != null && _ownsFocus)
        {
            _inputManager.PopFocusStack();
            _ownsFocus = false;
        }
    }

    private void SubmitCommand()
    {
        var command = _inputBuffer.Trim();
        AddLine(Prompt + command, ForegroundColor);

        if (!string.IsNullOrEmpty(command))
        {
            AppendHistory(command);
            CommandSubmitted?.Invoke(this, command);
            if (HandleInternalCommand(command))
            {
                _inputBuffer = string.Empty;
                return;
            }
        }

        _inputBuffer = string.Empty;
        ResetHistoryNavigation();
        ResetAutoComplete();
    }

    private void AppendHistory(string command)
    {
        _history.Remove(command);
        _history.Add(command);
        if (_history.Count > 100)
        {
            _history.RemoveAt(0);
        }
    }

    private void TraverseHistory(bool up)
    {
        if (_history.Count == 0)
        {
            return;
        }

        if (_historyIndex == -1)
        {
            _historyOriginal = _inputBuffer;
        }

        if (up)
        {
            _historyIndex = _historyIndex <= 0 ? _history.Count - 1 : _historyIndex - 1;
        }
        else if (_historyIndex == -1)
        {
            return;
        }
        else
        {
            _historyIndex++;
            if (_historyIndex >= _history.Count)
            {
                _historyIndex = -1;
                _inputBuffer = _historyOriginal;
                return;
            }
        }

        if (_historyIndex >= 0 && _historyIndex < _history.Count)
        {
            _inputBuffer = _history[_historyIndex];
        }
    }

    private void ResetHistoryNavigation()
    {
        _historyIndex = -1;
        _historyOriginal = string.Empty;
    }

    private void ResetAutoComplete()
    {
        _autoCompleteIndex = -1;
        _autoCompleteSuggestions.Clear();
    }

    private void HandleAutoComplete()
    {
        if (GetAutoCompleteSuggestions == null)
        {
            return;
        }

        if (_autoCompleteIndex == -1)
        {
            _autoCompleteSuggestions.Clear();
            _autoCompleteSuggestions.AddRange(GetAutoCompleteSuggestions(_inputBuffer));
            if (_autoCompleteSuggestions.Count == 0)
            {
                return;
            }
            _autoCompleteIndex = 0;
        }
        else
        {
            _autoCompleteIndex = (_autoCompleteIndex + 1) % _autoCompleteSuggestions.Count;
        }

        _inputBuffer = _autoCompleteSuggestions[_autoCompleteIndex];
        ResetHistoryNavigation();
    }

    private bool HandleInternalCommand(string command)
    {
        var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        var keyword = parts[0].ToLowerInvariant();
        var handled = true;

        switch (keyword)
        {
            case "help":
                ShowHelp();
                break;
            case "clear":
                _entries.Clear();
                break;
            case "exit":
            case "close":
                Hide();
                break;
            case "show":
                Show();
                break;
            case "hide":
                Hide();
                break;
            case "echo":
                var echo = parts.Length > 1 ? string.Join(" ", parts[1..]) : string.Empty;
                AddLine(string.IsNullOrWhiteSpace(echo) ? "echo: missing arguments" : echo, ForegroundColor);
                break;
            case "history":
                ShowHistory();
                break;
            default:
                handled = false;
                break;
        }

        if (handled)
        {
            ResetHistoryNavigation();
        }

        return handled;
    }

    private void ShowHelp()
    {
        AddLine("Available commands:", Color.Cyan);
        AddLine("  help      - Show this help message", ForegroundColor);
        AddLine("  clear     - Clear console output", ForegroundColor);
        AddLine("  history   - Show recent commands", ForegroundColor);
        AddLine("  echo text - Echo text to the console", ForegroundColor);
        AddLine("  show      - Open the console", ForegroundColor);
        AddLine("  hide      - Close the console", ForegroundColor);
        AddLine("  exit      - Close the console", ForegroundColor);
    }

    private void ShowHistory()
    {
        if (_history.Count == 0)
        {
            AddLine("No history entries yet.", Color.Gray);
            return;
        }

        AddLine("Recent commands:", Color.Cyan);
        var count = Math.Min(10, _history.Count);
        for (var i = _history.Count - count; i < _history.Count; i++)
        {
            AddLine($"  {_history[i]}", ForegroundColor);
        }
    }

    private void RecalculateLayout()
    {
        var viewport = SquidVoxEngineContext.GraphicsDevice.Viewport;
        Size = new Vector2(viewport.Width, ConsoleHeight);
        Position = new Vector2(0f, _currentY);
        _maxLines = (int)Math.Max(1, (ConsoleHeight - 40f) / _lineHeight);
        _entries.Clear();
    }

    private static IReadOnlyList<Keys> GetJustPressedKeys(KeyboardState current, KeyboardState previous)
    {
        var pressed = current.GetPressedKeys();
        var list = new List<Keys>(pressed.Length);
        for (var i = 0; i < pressed.Length; i++)
        {
            var key = pressed[i];
            if (!previous.IsKeyDown(key))
            {
                list.Add(key);
            }
        }

        return list;
    }

    private bool ShouldShowCaret()
    {
        var time = SquidVoxEngineContext.GameTime?.TotalGameTime.TotalSeconds;
        if (!time.HasValue || CaretBlinkInterval <= 0f)
        {
            return true;
        }

        var period = CaretBlinkInterval * 2.0;
        return time.Value % period < CaretBlinkInterval;
    }

    private static string ConvertKeyToString(Keys key, bool shift)
    {
        if (key >= Keys.A && key <= Keys.Z)
        {
            var @char = (char)('a' + (key - Keys.A));
            return shift ? @char.ToString().ToUpperInvariant() : @char.ToString();
        }

        if (key >= Keys.D0 && key <= Keys.D9)
        {
            if (shift)
            {
                return key switch
                {
                    Keys.D1 => "!",
                    Keys.D2 => "@",
                    Keys.D3 => "#",
                    Keys.D4 => "$",
                    Keys.D5 => "%",
                    Keys.D6 => "^",
                    Keys.D7 => "&",
                    Keys.D8 => "*",
                    Keys.D9 => "(",
                    Keys.D0 => ")",
                    _       => string.Empty
                };
            }

            return ((char)('0' + (key - Keys.D0))).ToString();
        }

        if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
        {
            return ((char)('0' + (key - Keys.NumPad0))).ToString();
        }

        return key switch
        {
            Keys.OemPeriod        => shift ? ">" : ".",
            Keys.OemComma         => shift ? "<" : ",",
            Keys.OemQuestion      => shift ? "?" : "/",
            Keys.OemSemicolon     => shift ? ":" : ";",
            Keys.OemQuotes        => shift ? "\"" : "'",
            Keys.OemOpenBrackets  => shift ? "{" : "[",
            Keys.OemCloseBrackets => shift ? "}" : "]",
            Keys.OemPipe          => shift ? "|" : "\\",
            Keys.OemMinus         => shift ? "_" : "-",
            Keys.OemPlus          => shift ? "+" : "=",
            Keys.Space            => " ",
            _                     => string.Empty
        };
    }


}
