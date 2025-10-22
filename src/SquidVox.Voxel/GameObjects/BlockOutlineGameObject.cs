using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SquidVox.Core.GameObjects;

namespace SquidVox.Voxel.GameObjects;

/// <summary>
/// Renders an outline around a block position for highlighting.
/// </summary>
public sealed class BlockOutlineGameObject : Base3dGameObject, IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly BasicEffect _effect;
    private VertexBuffer? _vertexBuffer;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockOutlineGameObject"/> class.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device.</param>
    public BlockOutlineGameObject(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));

        _effect = new BasicEffect(_graphicsDevice)
        {
            VertexColorEnabled = true,
            LightingEnabled = false,
            TextureEnabled = false
        };

        Name = "BlockOutline";
        CreateOutlineGeometry();
    }

    /// <summary>
    /// Gets or sets the outline color.
    /// </summary>
    public Color OutlineColor { get; set; } = Color.White;

    /// <summary>
    /// Gets or sets the line width.
    /// </summary>
    public float LineWidth { get; set; } = 2f;

    /// <summary>
    /// Gets or sets the view matrix for rendering.
    /// </summary>
    public Matrix View { get; set; }

    /// <summary>
    /// Gets or sets the projection matrix for rendering.
    /// </summary>
    public Matrix Projection { get; set; }

    /// <summary>
    /// Draws the block outline at a specific position.
    /// </summary>
    /// <param name="blockPosition">The position of the block to outline.</param>
    /// <param name="view">The view matrix.</param>
    /// <param name="projection">The projection matrix.</param>
    public void Draw(Vector3 blockPosition, Matrix view, Matrix projection)
    {
        var savedPosition = Position;
        Position = blockPosition;
        View = view;
        Projection = projection;

        Draw3d(null!);

        Position = savedPosition;
    }

    /// <summary>
    /// Draws the block outline using the current Position property.
    /// </summary>
    /// <param name="gameTime">Game timing information.</param>
    protected override void OnDraw3d(GameTime gameTime)
    {
        if (_vertexBuffer == null)
        {
            return;
        }

        var world = Matrix.CreateTranslation(Position);

        _effect.World = world;
        _effect.View = View;
        _effect.Projection = Projection;

        var previousDepthStencilState = _graphicsDevice.DepthStencilState;
        var previousRasterizerState = _graphicsDevice.RasterizerState;

        _graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
        _graphicsDevice.RasterizerState = RasterizerState.CullNone;

        _graphicsDevice.SetVertexBuffer(_vertexBuffer);

        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawPrimitives(PrimitiveType.LineList, 0, 12);
        }

        _graphicsDevice.SetVertexBuffer(null);
        _graphicsDevice.DepthStencilState = previousDepthStencilState;
        _graphicsDevice.RasterizerState = previousRasterizerState;
    }

    private void CreateOutlineGeometry()
    {
        const float offset = 0.005f;
        var min = new Vector3(-offset, -offset, -offset);
        var max = new Vector3(1f + offset, 1f + offset, 1f + offset);

        var vertices = new VertexPositionColor[]
        {
            new(new Vector3(min.X, min.Y, min.Z), OutlineColor),
            new(new Vector3(max.X, min.Y, min.Z), OutlineColor),

            new(new Vector3(max.X, min.Y, min.Z), OutlineColor),
            new(new Vector3(max.X, min.Y, max.Z), OutlineColor),

            new(new Vector3(max.X, min.Y, max.Z), OutlineColor),
            new(new Vector3(min.X, min.Y, max.Z), OutlineColor),

            new(new Vector3(min.X, min.Y, max.Z), OutlineColor),
            new(new Vector3(min.X, min.Y, min.Z), OutlineColor),

            new(new Vector3(min.X, max.Y, min.Z), OutlineColor),
            new(new Vector3(max.X, max.Y, min.Z), OutlineColor),

            new(new Vector3(max.X, max.Y, min.Z), OutlineColor),
            new(new Vector3(max.X, max.Y, max.Z), OutlineColor),

            new(new Vector3(max.X, max.Y, max.Z), OutlineColor),
            new(new Vector3(min.X, max.Y, max.Z), OutlineColor),

            new(new Vector3(min.X, max.Y, max.Z), OutlineColor),
            new(new Vector3(min.X, max.Y, min.Z), OutlineColor),

            new(new Vector3(min.X, min.Y, min.Z), OutlineColor),
            new(new Vector3(min.X, max.Y, min.Z), OutlineColor),

            new(new Vector3(max.X, min.Y, min.Z), OutlineColor),
            new(new Vector3(max.X, max.Y, min.Z), OutlineColor),

            new(new Vector3(max.X, min.Y, max.Z), OutlineColor),
            new(new Vector3(max.X, max.Y, max.Z), OutlineColor),

            new(new Vector3(min.X, min.Y, max.Z), OutlineColor),
            new(new Vector3(min.X, max.Y, max.Z), OutlineColor),
        };

        _vertexBuffer?.Dispose();
        _vertexBuffer = new VertexBuffer(_graphicsDevice, typeof(VertexPositionColor), vertices.Length, BufferUsage.WriteOnly);
        _vertexBuffer.SetData(vertices);
    }

    /// <summary>
    /// Disposes the block outline resources.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _vertexBuffer?.Dispose();
        _effect.Dispose();
    }
}
