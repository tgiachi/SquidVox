using DryIoc;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using SquidVox.Core.Context;
using SquidVox.Core.GameObjects;
using SquidVox.Core.Interfaces.Services;

namespace SquidVox.Voxel.GameObjects;

/// <summary>
/// Renders an animated panoramic skybox using a scrolling 2D texture.
/// </summary>
public class SkyPanoramaGameObject : Base3dGameObject, IDisposable
{
    private readonly ILogger _logger = Log.ForContext<SkyPanoramaGameObject>();
    private readonly GraphicsDevice _graphicsDevice;
    private readonly Effect _skyEffect;
    private readonly Texture2D _skyTexture;
    private readonly CameraGameObject _camera;
    private VertexBuffer? _vertexBuffer;
    private IndexBuffer? _indexBuffer;
    private int _indexCount;
    private float _animationTime;
    private BasicEffect? _debugEffect;

    /// <summary>
    /// Initializes a new instance of the SkyPanoramaGameObject class.
    /// </summary>
    /// <param name="camera">The camera to use for rendering.</param>
    /// <param name="textureName">The name of the sky texture to load.</param>
    public SkyPanoramaGameObject(CameraGameObject camera, string textureName)
    {
        _camera = camera ?? throw new ArgumentNullException(nameof(camera));
        _graphicsDevice = SquidVoxGraphicContext.GraphicsDevice;

        var assetManager = SquidVoxGraphicContext.Container.Resolve<IAssetManagerService>();
        _skyEffect = assetManager.GetEffect("Effects/SkyPanorama");
        _skyTexture = assetManager.GetTexture(textureName);

        if (_skyEffect == null)
        {
            _logger.Error("Failed to load SkyPanorama effect");
            throw new InvalidOperationException("SkyPanorama effect not loaded");
        }

        if (_skyTexture == null)
        {
            _logger.Error("Failed to load sky texture: {TextureName}", textureName);
            throw new InvalidOperationException($"Sky texture '{textureName}' not loaded");
        }

        _logger.Information("SkyPanorama initialized with texture {TextureName}", textureName);

        _debugEffect = new BasicEffect(_graphicsDevice)
        {
            VertexColorEnabled = true,
            LightingEnabled = false
        };

        CreateSkyGeometry();
    }

    /// <summary>
    /// Gets or sets the scale of the sky box.
    /// </summary>
    public float Radius { get; set; } = 1f;

    /// <summary>
    /// Gets or sets the scroll speed of the sky animation.
    /// </summary>
    public float ScrollSpeed { get; set; } = 0.01f;

    /// <summary>
    /// Gets or sets the number of segments for the sky sphere geometry.
    /// </summary>
    public int Segments { get; set; } = 32;

    /// <summary>
    /// Gets or sets whether to use debug rendering (solid color instead of texture).
    /// </summary>
    public bool DebugMode { get; set; } = true;

    /// <summary>
    /// Updates the sky animation.
    /// </summary>
    /// <param name="gameTime">Game timing information.</param>
    protected override void OnUpdate(GameTime gameTime)
    {
        _animationTime += (float)gameTime.ElapsedGameTime.TotalSeconds * ScrollSpeed;
        _animationTime %= 1.0f;
    }

    /// <summary>
    /// Renders the sky panorama.
    /// </summary>
    /// <param name="gameTime">Game timing information.</param>
    protected override void OnDraw3d(GameTime gameTime)
    {
        if (_vertexBuffer == null || _indexBuffer == null)
        {
            _logger.Warning("Vertex or index buffer is null, skipping render");
            return;
        }

        _logger.Verbose("Drawing sky - Camera pos: {Pos}, Animation time: {Time}",
            _camera.Position, _animationTime);

        var viewNoTranslation = new Matrix(
            _camera.View.M11, _camera.View.M12, _camera.View.M13, 0,
            _camera.View.M21, _camera.View.M22, _camera.View.M23, 0,
            _camera.View.M31, _camera.View.M32, _camera.View.M33, 0,
            0, 0, 0, 1
        );

        var world = Matrix.CreateScale(Radius);
        var wvp = world * viewNoTranslation * _camera.Projection;

        if (_skyEffect.Parameters["Matrix"] == null)
        {
            _logger.Error("Matrix parameter not found in effect");
            return;
        }

        if (_skyEffect.Parameters["Timer"] == null)
        {
            _logger.Error("Timer parameter not found in effect");
            return;
        }

        if (_skyEffect.Parameters["SkyTexture"] == null)
        {
            _logger.Error("SkyTexture parameter not found in effect");
            return;
        }

        _skyEffect.Parameters["Matrix"].SetValue(wvp);
        _skyEffect.Parameters["Timer"].SetValue(_animationTime);
        _skyEffect.Parameters["SkyTexture"].SetValue(_skyTexture);

        var oldDepth = _graphicsDevice.DepthStencilState;
        var oldRaster = _graphicsDevice.RasterizerState;

        var depthStencilState = new DepthStencilState
        {
            DepthBufferEnable = true,
            DepthBufferWriteEnable = false,
            DepthBufferFunction = CompareFunction.LessEqual
        };
        _graphicsDevice.DepthStencilState = depthStencilState;
        _graphicsDevice.RasterizerState = RasterizerState.CullNone;

        _graphicsDevice.SetVertexBuffer(_vertexBuffer);
        _graphicsDevice.Indices = _indexBuffer;

        _logger.Verbose("Drawing {Triangles} triangles", _indexCount / 3);

        if (DebugMode && _debugEffect != null)
        {
            _debugEffect.World = Matrix.Identity;
            _debugEffect.View = _camera.View;
            _debugEffect.Projection = _camera.Projection;
            _debugEffect.DiffuseColor = new Vector3(0f, 0.5f, 1f);
            _debugEffect.Alpha = 1f;

            foreach (var pass in _debugEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _indexCount / 3);
            }
        }
        else
        {
            foreach (var pass in _skyEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _indexCount / 3);
            }
        }

        _graphicsDevice.DepthStencilState = oldDepth;
        _graphicsDevice.RasterizerState = oldRaster;

        _logger.Verbose("Sky draw complete");
    }

    private void CreateSkyGeometry()
    {
        var vertices = new VertexPositionNormalTexture[]
        {
            new(new Vector3(-1, -1,  1), Vector3.Forward, new Vector2(0, 1)),
            new(new Vector3( 1, -1,  1), Vector3.Forward, new Vector2(1, 1)),
            new(new Vector3(-1,  1,  1), Vector3.Forward, new Vector2(0, 0)),
            new(new Vector3( 1,  1,  1), Vector3.Forward, new Vector2(1, 0)),

            new(new Vector3(-1, -1, -1), Vector3.Backward, new Vector2(1, 1)),
            new(new Vector3(-1,  1, -1), Vector3.Backward, new Vector2(1, 0)),
            new(new Vector3( 1, -1, -1), Vector3.Backward, new Vector2(0, 1)),
            new(new Vector3( 1,  1, -1), Vector3.Backward, new Vector2(0, 0)),

            new(new Vector3(-1,  1, -1), Vector3.Up, new Vector2(0, 1)),
            new(new Vector3(-1,  1,  1), Vector3.Up, new Vector2(0, 0)),
            new(new Vector3( 1,  1, -1), Vector3.Up, new Vector2(1, 1)),
            new(new Vector3( 1,  1,  1), Vector3.Up, new Vector2(1, 0)),

            new(new Vector3(-1, -1, -1), Vector3.Down, new Vector2(1, 1)),
            new(new Vector3( 1, -1, -1), Vector3.Down, new Vector2(0, 1)),
            new(new Vector3(-1, -1,  1), Vector3.Down, new Vector2(1, 0)),
            new(new Vector3( 1, -1,  1), Vector3.Down, new Vector2(0, 0)),

            new(new Vector3( 1, -1, -1), Vector3.Right, new Vector2(1, 1)),
            new(new Vector3( 1,  1, -1), Vector3.Right, new Vector2(1, 0)),
            new(new Vector3( 1, -1,  1), Vector3.Right, new Vector2(0, 1)),
            new(new Vector3( 1,  1,  1), Vector3.Right, new Vector2(0, 0)),

            new(new Vector3(-1, -1, -1), Vector3.Left, new Vector2(0, 1)),
            new(new Vector3(-1, -1,  1), Vector3.Left, new Vector2(1, 1)),
            new(new Vector3(-1,  1, -1), Vector3.Left, new Vector2(0, 0)),
            new(new Vector3(-1,  1,  1), Vector3.Left, new Vector2(1, 0))
        };

        var indices = new short[]
        {
            0, 1, 2, 2, 1, 3,
            4, 5, 6, 6, 5, 7,
            8, 9, 10, 10, 9, 11,
            12, 13, 14, 14, 13, 15,
            16, 17, 18, 18, 17, 19,
            20, 21, 22, 22, 21, 23
        };

        _vertexBuffer = new VertexBuffer(_graphicsDevice, typeof(VertexPositionNormalTexture),
                                        vertices.Length, BufferUsage.WriteOnly);
        _vertexBuffer.SetData(vertices);

        _indexBuffer = new IndexBuffer(_graphicsDevice, IndexElementSize.SixteenBits,
                                       indices.Length, BufferUsage.WriteOnly);
        _indexBuffer.SetData(indices);
        _indexCount = indices.Length;

        _logger.Information("Sky cube created: {VertexCount} vertices, {IndexCount} indices",
            vertices.Length, _indexCount);
    }

    /// <summary>
    /// Disposes resources used by the sky panorama.
    /// </summary>
    public void Dispose()
    {
        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
        _debugEffect?.Dispose();
        GC.SuppressFinalize(this);
    }
}
