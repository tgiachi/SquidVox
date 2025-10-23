using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SquidVox.Core.Context;
using SquidVox.Core.GameObjects;
using SquidVox.Core.Interfaces.Services;

namespace SquidVox.GameObjects.UI.Controls;

/// <summary>
/// Chat heads-up display composed of a scrollable message log and a text input box.
/// </summary>
public sealed class ChatBoxGameObject : Base2dGameObject
{
    private const float InputHeight = 32f;
    private const float InputSpacing = 4f;
    private const float FadeOutDuration = 1f;
    private readonly ScrollableTextGameObject _messagesBox;
    private readonly TextBoxGameObject _inputBox;
    private readonly List<ChatMessage> _messages = new();
    private readonly List<string> _formattedMessages = new();
    private readonly Color _messagesBackgroundColor = new Color(0, 0, 0, 180);
    private readonly Color _messagesBorderColor = new Color(255, 255, 255, 100);
    private readonly Color _messagesTextColor = Color.White;
    private readonly Color _inputBackgroundColor = new Color(0, 0, 0, 200);
    private readonly Color _inputBorderColor = new Color(255, 255, 255, 150);
    private readonly Color _inputTextColor = Color.White;
    private readonly Color _inputPlaceholderColor = new Color(200, 200, 200, 128);
    private readonly string _fontName;
    private readonly int _fontSize;
    private bool _isInitialized;
    private float _fadeTimer;
    private float _currentAlpha = 1f;
    private KeyboardState _previousKeyboardState;

    /// <summary>
    /// Event raised when a chat message is sent.
    /// </summary>
    public event EventHandler<string>? MessageSent;

    /// <summary>
    /// Event raised when a chat command is executed.
    /// </summary>
    public event EventHandler<string>? CommandExecuted;

    /// <summary>
    /// Gets a value indicating whether the input box is currently active.
    /// </summary>
    public bool IsInputActive { get; private set; }

    /// <summary>
    /// Gets or sets the delay before the chat starts fading out in seconds.
    /// </summary>
    public float FadeDelay { get; set; } = 5f;

    /// <summary>
    /// Gets or sets a value indicating whether the chat stays visible at all times.
    /// </summary>
    public bool AlwaysVisible { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of messages kept in the chat history.
    /// </summary>
    public int MaxMessages { get; set; } = 100;

    /// <summary>
    /// Gets or sets the overall size of the chat component.
    /// </summary>
    public override Vector2 Size
    {
        get => base.Size;
        set
        {
            if (base.Size == value)
            {
                return;
            }

            base.Size = value;
            UpdateLayout();
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatBoxGameObject"/> class.
    /// </summary>
    /// <param name="position">Optional screen position.</param>
    /// <param name="size">Optional size of the component.</param>
    /// <param name="fontName">Font used for messages and input.</param>
    /// <param name="fontSize">Font size used for rendering.</param>
    public ChatBoxGameObject(
        Vector2? position = null,
        Vector2? size = null,
        string fontName = "DefaultMono",
        int fontSize = 14)
    {
        Position = position ?? new Vector2(12f, 12f);
        Size = size ?? new Vector2(400f, 280f);
        Name = "ChatBox";
        _fontName = fontName;
        _fontSize = fontSize;

        _messagesBox = new ScrollableTextGameObject(Size.X, Math.Max(0f, Size.Y - InputHeight - InputSpacing), fontName, fontSize)
        {
            Name = "ChatMessages",
            IsVisible = true,
            TextColor = _messagesTextColor,
            BackgroundColor = _messagesBackgroundColor,
            BorderColor = _messagesBorderColor,
            Padding = 8,
            ScrollSpeed = 40f
        };

        _inputBox = new TextBoxGameObject(Size.X, InputHeight, fontName, fontSize)
        {
            Name = "ChatInput",
            IsVisible = false,
            BackgroundColor = _inputBackgroundColor,
            BorderColor = _inputBorderColor,
            ForegroundColor = _inputTextColor,
            TextColor = _inputTextColor,
            PlaceholderColor = _inputPlaceholderColor,
            FocusedBorderColor = _inputBorderColor,
            BorderWidth = 1,
            MaxLength = 256,
            Opacity = 1f
        };

        AddChild(_messagesBox);
        AddChild(_inputBox);

        _fadeTimer = FadeDelay;
        UpdateLayout();
    }

    /// <summary>
    /// Adds a message to the chat log.
    /// </summary>
    /// <param name="message">Content of the message.</param>
    /// <param name="type">Type of the message.</param>
    public void AddMessage(string message, ChatMessageType type = ChatMessageType.Normal)
    {
        var chatMessage = new ChatMessage
        {
            Text = message,
            Type = type,
            Timestamp = DateTime.Now
        };

        _messages.Add(chatMessage);
        _formattedMessages.Add(FormatMessage(chatMessage));

        if (_messages.Count > MaxMessages)
        {
            _messages.RemoveAt(0);
            _formattedMessages.RemoveAt(0);
        }

        UpdateMessagesText();
        ResetVisibility();
    }

    /// <summary>
    /// Adds a system message to the chat log.
    /// </summary>
    /// <param name="message">Content of the system message.</param>
    public void AddSystemMessage(string message)
    {
        AddMessage($"[SYSTEM] {message}", ChatMessageType.System);
    }

    /// <summary>
    /// Adds an error message to the chat log.
    /// </summary>
    /// <param name="message">Content of the error message.</param>
    public void AddErrorMessage(string message)
    {
        AddMessage($"[ERROR] {message}", ChatMessageType.Error);
    }

    /// <summary>
    /// Clears all messages from the chat log.
    /// </summary>
    public void Clear()
    {
        _messages.Clear();
        _formattedMessages.Clear();
        _messagesBox.Text = string.Empty;
    }

    /// <summary>
    /// Updates the chat component state.
    /// </summary>
    /// <param name="gameTime">Game timing information.</param>
    public override void Update(GameTime gameTime)
    {
        EnsureInitialized();

        var keyboardState = Keyboard.GetState();
        var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (!IsInputActive && IsKeyPressed(Keys.T, keyboardState))
        {
            OpenInput();
        }
        else if (IsInputActive && IsKeyPressed(Keys.Escape, keyboardState))
        {
            CloseInput();
        }

        UpdateVisibility(deltaTime);
        ApplyVisualState();

        _previousKeyboardState = keyboardState;

        base.Update(gameTime);
    }

    private void EnsureInitialized()
    {
        if (_isInitialized)
        {
            return;
        }

        var assetManager = SquidVoxEngineContext.GetService<IAssetManagerService>();

        _messagesBox.Initialize(_fontName, _fontSize);
        _inputBox.Initialize(assetManager, SquidVoxEngineContext.GraphicsDevice);
        _inputBox.PlaceholderText = "Press T to chat...";
        _inputBox.Text = string.Empty;
        _inputBox.EnterPressed += HandleInputEnterPressed;

        _isInitialized = true;
    }

    private void HandleInputEnterPressed(object? sender, EventArgs e)
    {
        SubmitMessage();
    }

    private void SubmitMessage()
    {
        var message = _inputBox.Text?.Trim();

        if (string.IsNullOrEmpty(message))
        {
            CloseInput();
            return;
        }

        if (message.StartsWith("/", StringComparison.Ordinal))
        {
            CommandExecuted?.Invoke(this, message);
        }
        else
        {
            MessageSent?.Invoke(this, message);
            AddMessage($"<You> {message}", ChatMessageType.Player);
        }

        CloseInput();
    }

    private void OpenInput()
    {
        EnsureInitialized();
        IsInputActive = true;
        HasFocus = true;
        _inputBox.IsVisible = true;
        _inputBox.HasFocus = true;
        _inputBox.IsFocused = true;
        _inputBox.Text = string.Empty;
        _inputBox.CursorPosition = 0;
        _inputBox.PlaceholderText = string.Empty;
        ResetVisibility();
    }

    private void CloseInput()
    {
        IsInputActive = false;
        HasFocus = false;
        _inputBox.IsVisible = false;
        _inputBox.IsFocused = false;
        _inputBox.HasFocus = false;
        _inputBox.Text = string.Empty;
        _inputBox.PlaceholderText = "Press T to chat...";
        _fadeTimer = FadeDelay;
        _currentAlpha = 1f;
        _messagesBox.IsVisible = true;
    }

    private void UpdateMessagesText()
    {
        _messagesBox.Text = string.Join('\n', _formattedMessages);
        _messagesBox.ScrollToBottom();
    }

    private void ResetVisibility()
    {
        _fadeTimer = FadeDelay;
        _currentAlpha = 1f;
        _messagesBox.IsVisible = true;
    }

    private void UpdateVisibility(float deltaTime)
    {
        if (AlwaysVisible || IsInputActive)
        {
            _fadeTimer = FadeDelay;
            _currentAlpha = 1f;
            _messagesBox.IsVisible = true;
            return;
        }

        if (_fadeTimer > 0f)
        {
            _fadeTimer = Math.Max(0f, _fadeTimer - deltaTime);
            _currentAlpha = 1f;
            return;
        }

        if (_currentAlpha > 0f)
        {
            _currentAlpha = Math.Max(0f, _currentAlpha - deltaTime / FadeOutDuration);
        }

        _messagesBox.IsVisible = _currentAlpha > 0.01f;
    }

    private void ApplyVisualState()
    {
        var alpha = AlwaysVisible ? 1f : _currentAlpha;

        _messagesBox.BackgroundColor = ApplyAlpha(_messagesBackgroundColor, alpha);
        _messagesBox.BorderColor = ApplyAlpha(_messagesBorderColor, alpha);
        _messagesBox.TextColor = ApplyAlpha(_messagesTextColor, alpha);

        if (!IsInputActive)
        {
            return;
        }

        _inputBox.BackgroundColor = _inputBackgroundColor;
        _inputBox.BorderColor = _inputBorderColor;
        _inputBox.TextColor = _inputTextColor;
        _inputBox.PlaceholderColor = _inputPlaceholderColor;
    }

    private void UpdateLayout()
    {
        var messageHeight = Math.Max(0f, Size.Y - InputHeight - InputSpacing);

        if (_messagesBox == null || _inputBox == null)
        {
            return;
        }

        _messagesBox.Position = Vector2.Zero;
        _messagesBox.Size = new Vector2(Size.X, messageHeight);

        _inputBox.Position = new Vector2(0f, messageHeight + InputSpacing);
        _inputBox.Size = new Vector2(Size.X, InputHeight);
    }

    private static Color ApplyAlpha(Color color, float alpha)
    {
        var clamped = Math.Clamp(alpha, 0f, 1f);
        var scaledAlpha = (int)MathF.Round(color.A * clamped);
        return new Color(color.R, color.G, color.B, scaledAlpha);
    }

    private static string FormatMessage(ChatMessage message)
    {
        var timestamp = message.Timestamp.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        return message.Type switch
        {
            ChatMessageType.System => $"[{timestamp}] {message.Text}",
            ChatMessageType.Error => $"[{timestamp}] {message.Text}",
            ChatMessageType.Server => $"[{timestamp}] <Server> {message.Text}",
            ChatMessageType.Player => $"[{timestamp}] {message.Text}",
            ChatMessageType.Info => $"[{timestamp}] {message.Text}",
            _ => $"[{timestamp}] {message.Text}"
        };
    }

    private bool IsKeyPressed(Keys key, KeyboardState currentState)
    {
        return currentState.IsKeyDown(key) && !_previousKeyboardState.IsKeyDown(key);
    }
}

/// <summary>
/// Represents a chat message recorded by the chat component.
/// </summary>
public sealed class ChatMessage
{
    /// <summary>
    /// Gets or sets the raw text of the message.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the message.
    /// </summary>
    public ChatMessageType Type { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the message.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Describes the various classifications available for chat messages.
/// </summary>
public enum ChatMessageType
{
    /// <summary>
    /// Standard player or world message.
    /// </summary>
    Normal,

    /// <summary>
    /// System informational message.
    /// </summary>
    System,

    /// <summary>
    /// Error message highlighted in the chat.
    /// </summary>
    Error,

    /// <summary>
    /// Player-authored message.
    /// </summary>
    Player,

    /// <summary>
    /// Message broadcast by the server.
    /// </summary>
    Server,

    /// <summary>
    /// Informational message displayed to the player.
    /// </summary>
    Info
}
