using System.Drawing;
using FontStashSharp.Interfaces;
using Silk.NET.OpenGL;
using TrippyGL;

namespace SquidVox.World.Fonts;

/// <summary>
/// Manages Texture2D objects for FontStashSharp.
/// </summary>
internal class Texture2DManager : ITexture2DManager
{
    /// <summary>
    /// Gets the graphics device.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; }

    /// <summary>
    /// Initializes a new instance of the Texture2DManager class.
    /// </summary>
    /// <param name="device">The graphics device.</param>
    public Texture2DManager(GraphicsDevice device)
    {
        GraphicsDevice = device ?? throw new ArgumentNullException(nameof(device));
    }

    /// <summary>
    /// Creates a new texture with the specified dimensions.
    /// </summary>
    /// <param name="width">The width of the texture.</param>
    /// <param name="height">The height of the texture.</param>
    /// <returns>A new texture object.</returns>
    public object CreateTexture(int width, int height) => new Texture2D(GraphicsDevice, (uint)width, (uint)height);

    /// <summary>
    /// Gets the size of the texture.
    /// </summary>
    /// <param name="texture">The texture object.</param>
    /// <returns>The size of the texture.</returns>
    public Point GetTextureSize(object texture)
    {
        var xnaTexture = (Texture2D)texture;

        return new Point((int)xnaTexture.Width, (int)xnaTexture.Height);
    }

    /// <summary>
    /// Sets the data for the texture.
    /// </summary>
    /// <param name="texture">The texture object.</param>
    /// <param name="bounds">The bounds to set data for.</param>
    /// <param name="data">The data to set.</param>
    public void SetTextureData(object texture, Rectangle bounds, byte[] data)
    {
        var xnaTexture = (Texture2D)texture;

        xnaTexture.SetData<byte>(data, bounds.X, bounds.Y, (uint)bounds.Width, (uint)bounds.Height, PixelFormat.Rgba);
    }
}
