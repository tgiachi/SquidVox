using System;
using Microsoft.Xna.Framework;
using SquidVox.Core.Notifications;

namespace SquidVox.Core.Interfaces.Services;

/// <summary>
/// Provides methods for publishing toast notifications.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Occurs when a notification is raised.
    /// </summary>
    event EventHandler<NotificationMessage>? NotificationRaised;

    /// <summary>
    /// Occurs when all notifications are cleared.
    /// </summary>
    event EventHandler? NotificationsCleared;

    /// <summary>
    /// Raises a custom notification.
    /// </summary>
    /// <param name="text">The notification text.</param>
    /// <param name="duration">The optional display duration.</param>
    /// <param name="textColor">The optional text colour.</param>
    /// <param name="backgroundColor">The optional background colour.</param>
    /// <param name="iconTextureName">The optional texture name for a leading icon.</param>
    void ShowMessage(
        string text,
        float? duration = null,
        Color? textColor = null,
        Color? backgroundColor = null,
        string? iconTextureName = null
    );

    /// <summary>
    /// Raises a notification for a predefined type.
    /// </summary>
    /// <param name="text">The notification text.</param>
    /// <param name="type">The notification type.</param>
    /// <param name="duration">The optional display duration.</param>
    /// <summary>
    /// Raises a notification for a predefined type.
    /// </summary>
    /// <param name="text">The notification text.</param>
    /// <param name="type">The notification type.</param>
    /// <param name="duration">The optional display duration.</param>
    /// <param name="iconTextureName">The optional texture name for a leading icon.</param>
    void ShowMessage(string text, NotificationType type, float? duration = null, string? iconTextureName = null);

    /// <summary>
    /// Raises an informational notification.
    /// </summary>
    /// <param name="text">The notification text.</param>
    /// <param name="duration">The optional display duration.</param>
    /// <summary>
    /// Raises an informational notification.
    /// </summary>
    /// <param name="text">The notification text.</param>
    /// <param name="duration">The optional display duration.</param>
    /// <param name="iconTextureName">The optional texture name for a leading icon.</param>
    void ShowInfo(string text, float? duration = null, string? iconTextureName = null);

    /// <summary>
    /// Raises a success notification.
    /// </summary>
    /// <param name="text">The notification text.</param>
    /// <param name="duration">The optional display duration.</param>
    /// <summary>
    /// Raises a success notification.
    /// </summary>
    /// <param name="text">The notification text.</param>
    /// <param name="duration">The optional display duration.</param>
    /// <param name="iconTextureName">The optional texture name for a leading icon.</param>
    void ShowSuccess(string text, float? duration = null, string? iconTextureName = null);

    /// <summary>
    /// Raises a warning notification.
    /// </summary>
    /// <param name="text">The notification text.</param>
    /// <param name="duration">The optional display duration.</param>
    /// <summary>
    /// Raises a warning notification.
    /// </summary>
    /// <param name="text">The notification text.</param>
    /// <param name="duration">The optional display duration.</param>
    /// <param name="iconTextureName">The optional texture name for a leading icon.</param>
    void ShowWarning(string text, float? duration = null, string? iconTextureName = null);

    /// <summary>
    /// Raises an error notification.
    /// </summary>
    /// <param name="text">The notification text.</param>
    /// <param name="duration">The optional display duration.</param>
    /// <summary>
    /// Raises an error notification.
    /// </summary>
    /// <param name="text">The notification text.</param>
    /// <param name="duration">The optional display duration.</param>
    /// <param name="iconTextureName">The optional texture name for a leading icon.</param>
    void ShowError(string text, float? duration = null, string? iconTextureName = null);

    /// <summary>
    /// Clears all active notifications.
    /// </summary>
    void Clear();
}
