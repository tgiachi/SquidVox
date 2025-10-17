using System.Drawing;
using System.Numerics;
using FontStashSharp;
using FontStashSharp.Interfaces;
using SquidVox.World.Extensions;
using TrippyGL;

namespace SquidVox.World.Fonts;

/// <summary>
/// Implements font rendering using FontStashSharp and TrippyGL.
/// </summary>
public class FontStashRenderer : IFontStashRenderer, IDisposable
{
    private readonly SimpleShaderProgram _shaderProgram;
    private readonly TextureBatcher _batch;
    private readonly Texture2DManager _textureManager;

    /// <summary>
    /// Gets the texture manager.
    /// </summary>
    public ITexture2DManager TextureManager => _textureManager;

    /// <summary>
    /// Gets the graphics device.
    /// </summary>
    public GraphicsDevice GraphicsDevice => _textureManager.GraphicsDevice;

    /// <summary>
    /// Initializes a new instance of the FontStashRenderer class.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device to use for rendering.</param>
    public FontStashRenderer(GraphicsDevice graphicsDevice)
    {
        _textureManager = new Texture2DManager(graphicsDevice);

        _shaderProgram = SimpleShaderProgram.Create<VertexColorTexture>(graphicsDevice, 0, 0, true);
        _batch = new TextureBatcher(graphicsDevice);
        _batch.SetShaderProgram(_shaderProgram);
        OnViewportChanged();
    }

    /// <summary>
    /// Updates the projection matrix when the viewport changes.
    /// </summary>
    public void OnViewportChanged()
    {
        _shaderProgram.Projection = Matrix4x4.CreateOrthographicOffCenter(
            0,
            GraphicsDevice.Viewport.Width,
            GraphicsDevice.Viewport.Height,
            0,
            0,
            1
        );
    }


    /// <summary>
    /// Begins the rendering batch.
    /// </summary>
    public void Begin() => _batch.Begin();

    /// <summary>
    /// Ends the rendering batch.
    /// </summary>
    public void End() => _batch.End();

    /// <summary>
    /// Draws a texture with the specified parameters.
    /// </summary>
    /// <param name="texture">The texture to draw.</param>
    /// <param name="pos">The position to draw at.</param>
    /// <param name="src">The source rectangle.</param>
    /// <param name="color">The color to tint with.</param>
    /// <param name="rotation">The rotation angle.</param>
    /// <param name="scale">The scale factor.</param>
    /// <param name="depth">The depth value.</param>
    public void Draw(object texture, Vector2 pos, Rectangle? src, FSColor color, float rotation, Vector2 scale, float depth)
    {
        var tex = (Texture2D)texture;

        _batch.Draw(
            tex,
            pos,
            src,
            color.ToTrippy(),
            scale,
            rotation,
            Vector2.Zero,
            depth
        );
    }

    public void Dispose()
    {
        _shaderProgram.Dispose();
        _batch.Dispose();

        GC.SuppressFinalize(this);
    }
}
