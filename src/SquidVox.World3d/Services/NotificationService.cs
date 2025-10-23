using System;
using Microsoft.Xna.Framework;
using SquidVox.Core.Interfaces.Services;
using SquidVox.Core.Notifications;

namespace SquidVox.World3d.Services;

/// <summary>
/// Provides an implementation of notification publishing.
/// </summary>
public sealed class NotificationService : INotificationService
{
    private const float DefaultDuration = 3.0f;
    private const float WarningDuration = 4.0f;
    private const float ErrorDuration = 5.0f;
    private static readonly Color DefaultBackground = new Color(0, 0, 0, 180);
    private static readonly Color DefaultText = Color.White;
    private static readonly Color InfoBackground = new Color(0, 100, 200, 180);
    private static readonly Color SuccessBackground = new Color(0, 150, 0, 180);
    private static readonly Color WarningBackground = new Color(200, 150, 0, 180);
    private static readonly Color ErrorBackground = new Color(200, 0, 0, 180);
    private readonly Lock _lock = new();

    /// <inheritdoc />
    public event EventHandler<NotificationMessage>? NotificationRaised;

    /// <inheritdoc />
    public event EventHandler? NotificationsCleared;

    /// <inheritdoc />
    public void ShowMessage(
        string text,
        float? duration = null,
        Color? textColor = null,
        Color? backgroundColor = null,
        string? iconTextureName = null
    )
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var message = CreateMessage(
            text.Trim(),
            duration ?? DefaultDuration,
            textColor ?? DefaultText,
            backgroundColor ?? DefaultBackground,
            iconTextureName
        );
        Publish(message);
    }

    /// <inheritdoc />
    public void ShowMessage(string text, NotificationType type, float? duration = null, string? iconTextureName = null)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var (textColor, backgroundColor, defaultDuration) = GetDefaults(type);
        var message = CreateMessage(text.Trim(), duration ?? defaultDuration, textColor, backgroundColor, iconTextureName);
        Publish(message);
    }

    /// <inheritdoc />
    public void ShowInfo(string text, float? duration = null, string? iconTextureName = null)
    {
        ShowMessage(text, NotificationType.Info, duration, iconTextureName);
    }

    /// <inheritdoc />
    public void ShowSuccess(string text, float? duration = null, string? iconTextureName = null)
    {
        ShowMessage(text, NotificationType.Success, duration, iconTextureName);
    }

    /// <inheritdoc />
    public void ShowWarning(string text, float? duration = null, string? iconTextureName = null)
    {
        ShowMessage(text, NotificationType.Warning, duration, iconTextureName);
    }

    /// <inheritdoc />
    public void ShowError(string text, float? duration = null, string? iconTextureName = null)
    {
        ShowMessage(text, NotificationType.Error, duration, iconTextureName);
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_lock)
        {
            NotificationsCleared?.Invoke(this, EventArgs.Empty);
        }
    }

    private static NotificationMessage CreateMessage(
        string text,
        float duration,
        Color textColor,
        Color backgroundColor,
        string? iconTextureName
    )
    {
        var sanitizedIconName = string.IsNullOrWhiteSpace(iconTextureName)
            ? null
            : iconTextureName.Trim();

        return new NotificationMessage
        {
            Text = text,
            Duration = duration,
            TextColor = textColor,
            BackgroundColor = backgroundColor,
            IconTextureName = sanitizedIconName
        };
    }

    private static (Color TextColor, Color BackgroundColor, float Duration) GetDefaults(NotificationType type)
    {
        return type switch
        {
            NotificationType.Info => (Color.White, InfoBackground, DefaultDuration),
            NotificationType.Success => (Color.LightGreen, SuccessBackground, DefaultDuration),
            NotificationType.Warning => (Color.Yellow, WarningBackground, WarningDuration),
            NotificationType.Error => (Color.Red, ErrorBackground, ErrorDuration),
            _ => (DefaultText, DefaultBackground, DefaultDuration)
        };
    }

    private void Publish(NotificationMessage message)
    {
        lock (_lock)
        {
            var copy = CreateMessage(
                message.Text,
                message.Duration,
                message.TextColor,
                message.BackgroundColor,
                message.IconTextureName
            );
            copy.FadeInDuration = message.FadeInDuration;
            copy.FadeOutDuration = message.FadeOutDuration;
            NotificationRaised?.Invoke(this, copy);
        }
    }
}
