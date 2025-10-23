using Microsoft.Xna.Framework;

namespace SquidVox.GameObjects.UI.Controls;

/// <summary>
/// Represents a console entry with text and color information.
/// </summary>
internal readonly struct ConsoleEntry
{
    /// <summary>
    /// Initializes a new instance of the ConsoleEntry struct.
    /// </summary>
    /// <param name="text">The text content.</param>
    /// <param name="foreground">The foreground color.</param>
    /// <param name="background">The optional background color.</param>
    internal ConsoleEntry(string text, Color foreground, Color? background)
    {
        Text = text;
        Foreground = foreground;
        Background = background;
    }

    /// <summary>
    /// Gets the text content.
    /// </summary>
    internal string Text { get; }

    /// <summary>
    /// Gets the foreground color.
    /// </summary>
    internal Color Foreground { get; }

    /// <summary>
    /// Gets the optional background color.
    /// </summary>
    internal Color? Background { get; }
}