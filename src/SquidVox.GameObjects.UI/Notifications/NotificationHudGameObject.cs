using System;
using System.Collections.Generic;
using FontStashSharp;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SquidVox.Core.Context;
using SquidVox.Core.GameObjects;
using SquidVox.Core.Interfaces.Services;
using SquidVox.Core.Notifications;

namespace SquidVox.GameObjects.UI.Notifications;

/// <summary>
/// Displays queued toast notifications with fade and slide animations.
/// </summary>
public class NotificationHudGameObject : Base2dGameObject
{
    private const float DefaultSlideSpeed = 8.0f;
    private const float DefaultPadding = 10.0f;
    private const float DefaultSpacing = 35.0f;
    private const float DefaultOpacityThreshold = 0.01f;
    private const float InitialYOffset = -50f;
    private readonly ObjectPool<NotificationMessage> _messagePool = ObjectPool.Create<NotificationMessage>();
    private readonly List<NotificationMessage> _messages = new();
    private DynamicSpriteFont? _font;
    private IAssetManagerService? _assetManager;
    private bool _isInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationHudGameObject"/> class.
    /// </summary>
    public NotificationHudGameObject()
    {
        ZIndex = 5000;
    }

    /// <summary>
    /// Gets or sets the anchor position for notifications.
    /// </summary>
    public Vector2 StartPosition { get; set; } = new(20f, 20f);

    /// <summary>
    /// Gets or sets the vertical spacing between notifications.
    /// </summary>
    public float MessageSpacing { get; set; } = DefaultSpacing;

    /// <summary>
    /// Gets or sets the text padding.
    /// </summary>
    public float MessagePadding { get; set; } = DefaultPadding;

    /// <summary>
    /// Gets or sets the maximum number of active notifications.
    /// </summary>
    public int MaxVisibleMessages { get; set; } = 5;

    /// <summary>
    /// Gets or sets the slide animation speed.
    /// </summary>
    public float SlideAnimationSpeed { get; set; } = DefaultSlideSpeed;

    /// <summary>
    /// Gets or sets the icon size.
    /// </summary>
    public Vector2 IconSize { get; set; } = new(24f, 24f);

    /// <summary>
    /// Gets or sets the spacing between icon and text.
    /// </summary>
    public float IconSpacing { get; set; } = 8f;

    /// <summary>
    /// Initializes the component resources.
    /// </summary>
    /// <param name="assetManagerService">The asset manager service.</param>
    /// <param name="notificationService">The notification service.</param>
    public void Initialize(IAssetManagerService assetManagerService, INotificationService notificationService)
    {
        if (_isInitialized)
        {
            return;
        }

        try
        {
            _font = assetManagerService.GetFont("Monocraft", 18);
        }
        catch
        {
            _font = null;
        }
        _assetManager = assetManagerService;
        notificationService.NotificationRaised += HandleNotificationRaised;
        notificationService.NotificationsCleared += HandleNotificationsCleared;
        _isInitialized = true;
    }

    /// <inheritdoc />
    protected override void OnUpdate(GameTime gameTime)
    {
        if (_messages.Count == 0)
        {
            return;
        }

        var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        for (var i = _messages.Count - 1; i >= 0; i--)
        {
            var message = _messages[i];
            message.ElapsedTime += deltaTime;

            if (message.IsFadingIn)
            {
                var fadeProgress = message.ElapsedTime / message.FadeInDuration;
                message.Alpha = Math.Min(1.0f, fadeProgress);
            }
            else if (message.IsFadingOut)
            {
                var fadeOutStart = message.Duration - message.FadeOutDuration;
                var fadeProgress = (message.ElapsedTime - fadeOutStart) / message.FadeOutDuration;
                message.Alpha = Math.Max(0.0f, 1.0f - fadeProgress);
            }
            else
            {
                message.Alpha = 1.0f;
            }

            var targetY = message.TargetY;
            var currentY = StartPosition.Y + message.YOffset;
            var difference = targetY - currentY;

            if (Math.Abs(difference) > 0.5f)
            {
                message.YOffset += difference * SlideAnimationSpeed * deltaTime;
            }
            else
            {
                message.YOffset = targetY - StartPosition.Y;
            }

            if (message.ShouldRemove)
            {
                _messages.RemoveAt(i);
                ResetMessage(message);
                _messagePool.Return(message);
                UpdateMessagePositions();
            }
        }

        while (_messages.Count > MaxVisibleMessages)
        {
            var oldest = _messages[0];
            _messages.RemoveAt(0);
            ResetMessage(oldest);
            _messagePool.Return(oldest);
            UpdateMessagePositions();
        }
    }

    /// <inheritdoc />
    protected override void OnRender(SpriteBatch spriteBatch)
    {
        if (_messages.Count == 0 || _font == null)
        {
            return;
        }

        for (var i = 0; i < _messages.Count; i++)
        {
            var message = _messages[i];
            if (message.Alpha <= DefaultOpacityThreshold)
            {
                continue;
            }

            if (message.IconTexture == null && !string.IsNullOrWhiteSpace(message.IconTextureName))
            {
                message.IconTexture = ResolveIcon(message.IconTextureName);
            }

            var basePosition = new Vector2(StartPosition.X, StartPosition.Y + message.YOffset);
            var textSize = _font.MeasureString(message.Text);
            var iconTexture = message.IconTexture;
            var hasIcon = iconTexture != null;
            var iconWidth = hasIcon ? IconSize.X : 0f;
            var iconSpacing = hasIcon ? IconSpacing : 0f;
            var contentHeight = Math.Max(textSize.Y, hasIcon ? IconSize.Y : 0f);
            var backgroundRect = new Rectangle(
                (int)(basePosition.X - MessagePadding),
                (int)(basePosition.Y - MessagePadding),
                (int)(textSize.X + iconWidth + iconSpacing + MessagePadding * 2f),
                (int)(contentHeight + MessagePadding * 2f)
            );
            var textPosition = new Vector2(
                basePosition.X + iconWidth + iconSpacing,
                basePosition.Y + (contentHeight - textSize.Y) / 2f
            );

            var finalBackground = ApplyAlpha(message.BackgroundColor, message.Alpha);
            var finalText = ApplyAlpha(message.TextColor, message.Alpha);

            if (finalBackground.A > 0)
            {
                spriteBatch.Draw(SquidVoxEngineContext.WhitePixel, backgroundRect, finalBackground);
            }

            if (hasIcon && iconTexture != null)
            {
                var iconRect = new Rectangle(
                    (int)basePosition.X,
                    (int)(basePosition.Y + (contentHeight - IconSize.Y) / 2f),
                    (int)IconSize.X,
                    (int)IconSize.Y
                );
                spriteBatch.Draw(iconTexture, iconRect, Color.White * message.Alpha);
            }

            spriteBatch.DrawString(_font, message.Text, textPosition, finalText);
        }
    }

    private static Color ApplyAlpha(Color color, float alpha)
    {
        var a = (byte)(color.A * alpha);
        return new Color(color.R, color.G, color.B, a);
    }

    private void HandleNotificationRaised(object? sender, NotificationMessage message)
    {
        var instance = _messagePool.Get();
        ResetMessage(instance);
        instance.Text = message.Text;
        instance.Duration = message.Duration;
        instance.TextColor = message.TextColor;
        instance.BackgroundColor = message.BackgroundColor;
        instance.FadeInDuration = message.FadeInDuration;
        instance.FadeOutDuration = message.FadeOutDuration;
        instance.ElapsedTime = 0f;
        instance.Alpha = 0f;
        instance.YOffset = InitialYOffset;
        instance.TargetY = 0f;
        instance.IconTextureName = message.IconTextureName;
        instance.IconTexture = ResolveIcon(message.IconTextureName);

        _messages.Add(instance);
        UpdateMessagePositions();
    }

    private void HandleNotificationsCleared(object? sender, EventArgs e)
    {
        for (var i = 0; i < _messages.Count; i++)
        {
            var message = _messages[i];
            ResetMessage(message);
            _messagePool.Return(message);
        }

        _messages.Clear();
    }

    private void UpdateMessagePositions()
    {
        for (var i = 0; i < _messages.Count; i++)
        {
            _messages[i].TargetY = StartPosition.Y + i * MessageSpacing;
        }
    }

    private Texture2D? ResolveIcon(string? iconTextureName)
    {
        if (string.IsNullOrWhiteSpace(iconTextureName) || _assetManager == null)
        {
            return null;
        }

        try
        {
            return _assetManager.GetTexture(iconTextureName);
        }
        catch
        {
            return null;
        }
    }

    private static void ResetMessage(NotificationMessage message)
    {
        message.Text = string.Empty;
        message.Duration = 0f;
        message.TextColor = Color.White;
        message.BackgroundColor = Color.Black;
        message.ElapsedTime = 0f;
        message.FadeInDuration = 0.3f;
        message.FadeOutDuration = 0.5f;
        message.Alpha = 0f;
        message.YOffset = InitialYOffset;
        message.TargetY = 0f;
        message.IconTextureName = null;
        message.IconTexture = null;
    }
}
