using DryIoc;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using SquidVox.Core.Context;
using SquidVox.Core.GameObjects;
using SquidVox.Core.Interfaces.Services;

namespace SquidVox.Voxel.GameObjects;

/// <summary>
/// Represents a single cloud instance with position and size.
/// </summary>
public struct Cloud
{
    /// <summary>
    /// Gets or sets the position of the cloud.
    /// </summary>
    public Vector3 Position { get; set; }

    /// <summary>
    /// Gets or sets the size of the cloud.
    /// </summary>
    public Vector3 Size { get; set; }

    /// <summary>
    /// Initializes a new instance of the Cloud struct.
    /// </summary>
    public Cloud(Vector3 position, Vector3 size)
    {
        Position = position;
        Size = size;
    }
}

/// <summary>
/// Renders volumetric clouds with face-based shading.
/// </summary>
public class CloudsGameObject : Base3dGameObject, IDisposable
{
    private readonly ILogger _logger = Log.ForContext<CloudsGameObject>();
    private readonly GraphicsDevice _graphicsDevice;
    private readonly Effect _cloudEffect;
    private readonly CameraGameObject _camera;
    private VertexBuffer? _cubeVertexBuffer;
    private IndexBuffer? _cubeIndexBuffer;
    private int _cubeIndexCount;
    private readonly List<Cloud> _clouds = new();

    /// <summary>
    /// Initializes a new instance of the CloudsGameObject class.
    /// </summary>
    /// <param name="camera">The camera to use for rendering.</param>
    public CloudsGameObject(CameraGameObject camera)
    {
        _camera = camera ?? throw new ArgumentNullException(nameof(camera));
        _graphicsDevice = SquidVoxGraphicContext.GraphicsDevice;

        var assetManager = SquidVoxGraphicContext.Container.Resolve<IAssetManagerService>();
        _cloudEffect = assetManager.GetEffect("Effects/Clouds");

        if (_cloudEffect == null)
        {
            _logger.Error("Failed to load Clouds effect");
            throw new InvalidOperationException("Clouds effect not loaded");
        }

        _logger.Information("CloudsGameObject initialized");

        CreateCubeGeometry();
    }

    /// <summary>
    /// Adds a cloud to the scene.
    /// </summary>
    /// <param name="cloud">The cloud to add.</param>
    public void AddCloud(Cloud cloud)
    {
        _clouds.Add(cloud);
    }

    /// <summary>
    /// Removes a cloud from the scene.
    /// </summary>
    /// <param name="cloud">The cloud to remove.</param>
    public void RemoveCloud(Cloud cloud)
    {
        _clouds.Remove(cloud);
    }

    /// <summary>
    /// Clears all clouds from the scene.
    /// </summary>
    public void ClearClouds()
    {
        _clouds.Clear();
    }

    /// <summary>
    /// Gets the number of clouds in the scene.
    /// </summary>
    public int CloudCount => _clouds.Count;

    /// <summary>
    /// Generates random clouds in a given area.
    /// </summary>
    /// <param name="count">Number of clouds to generate.</param>
    /// <param name="minPosition">Minimum position bounds.</param>
    /// <param name="maxPosition">Maximum position bounds.</param>
    /// <param name="minSize">Minimum cloud size.</param>
    /// <param name="maxSize">Maximum cloud size.</param>
    public void GenerateRandomClouds(int count, Vector3 minPosition, Vector3 maxPosition, Vector3 minSize, Vector3 maxSize)
    {
        var random = new Random();

        for (int i = 0; i < count; i++)
        {
            var position = new Vector3(
                Lerp(minPosition.X, maxPosition.X, (float)random.NextDouble()),
                Lerp(minPosition.Y, maxPosition.Y, (float)random.NextDouble()),
                Lerp(minPosition.Z, maxPosition.Z, (float)random.NextDouble())
            );

            var size = new Vector3(
                Lerp(minSize.X, maxSize.X, (float)random.NextDouble()),
                Lerp(minSize.Y, maxSize.Y, (float)random.NextDouble()),
                Lerp(minSize.Z, maxSize.Z, (float)random.NextDouble())
            );

            AddCloud(new Cloud(position, size));
        }

        _logger.Information("Generated {Count} random clouds", count);
    }

    private static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }

    /// <summary>
    /// Renders all clouds.
    /// </summary>
    /// <param name="gameTime">Game timing information.</param>
    protected override void OnDraw3d(GameTime gameTime)
    {
        if (_cubeVertexBuffer == null || _cubeIndexBuffer == null || _clouds.Count == 0)
        {
            return;
        }

        if (_cloudEffect.Parameters["Model"] == null ||
            _cloudEffect.Parameters["View"] == null ||
            _cloudEffect.Parameters["Projection"] == null)
        {
            _logger.Error("Required parameters not found in cloud effect");
            return;
        }

        var oldBlend = _graphicsDevice.BlendState;
        var oldDepth = _graphicsDevice.DepthStencilState;
        var oldRaster = _graphicsDevice.RasterizerState;

        _graphicsDevice.BlendState = BlendState.AlphaBlend;
        _graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
        _graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;

        _graphicsDevice.SetVertexBuffer(_cubeVertexBuffer);
        _graphicsDevice.Indices = _cubeIndexBuffer;

        _cloudEffect.Parameters["View"].SetValue(_camera.View);
        _cloudEffect.Parameters["Projection"].SetValue(_camera.Projection);

        foreach (var cloud in _clouds)
        {
            var model = Matrix.CreateScale(cloud.Size) * Matrix.CreateTranslation(cloud.Position);
            _cloudEffect.Parameters["Model"].SetValue(model);

            foreach (var pass in _cloudEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _cubeIndexCount / 3);
            }
        }

        _graphicsDevice.BlendState = oldBlend;
        _graphicsDevice.DepthStencilState = oldDepth;
        _graphicsDevice.RasterizerState = oldRaster;
    }

    private void CreateCubeGeometry()
    {
        var vertices = new VertexPositionNormalTexture[]
        {
            // Front face (Z+)
            new(new Vector3(-0.5f, -0.5f,  0.5f), new Vector3(0, 0, 1), Vector2.Zero),
            new(new Vector3( 0.5f, -0.5f,  0.5f), new Vector3(0, 0, 1), Vector2.Zero),
            new(new Vector3(-0.5f,  0.5f,  0.5f), new Vector3(0, 0, 1), Vector2.Zero),
            new(new Vector3( 0.5f,  0.5f,  0.5f), new Vector3(0, 0, 1), Vector2.Zero),
            
            // Back face (Z-)
            new(new Vector3( 0.5f, -0.5f, -0.5f), new Vector3(0, 0, -1), Vector2.Zero),
            new(new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0, 0, -1), Vector2.Zero),
            new(new Vector3( 0.5f,  0.5f, -0.5f), new Vector3(0, 0, -1), Vector2.Zero),
            new(new Vector3(-0.5f,  0.5f, -0.5f), new Vector3(0, 0, -1), Vector2.Zero),
            
            // Top face (Y+)
            new(new Vector3(-0.5f,  0.5f,  0.5f), new Vector3(0, 1, 0), Vector2.Zero),
            new(new Vector3( 0.5f,  0.5f,  0.5f), new Vector3(0, 1, 0), Vector2.Zero),
            new(new Vector3(-0.5f,  0.5f, -0.5f), new Vector3(0, 1, 0), Vector2.Zero),
            new(new Vector3( 0.5f,  0.5f, -0.5f), new Vector3(0, 1, 0), Vector2.Zero),
            
            // Bottom face (Y-)
            new(new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0, -1, 0), Vector2.Zero),
            new(new Vector3( 0.5f, -0.5f, -0.5f), new Vector3(0, -1, 0), Vector2.Zero),
            new(new Vector3(-0.5f, -0.5f,  0.5f), new Vector3(0, -1, 0), Vector2.Zero),
            new(new Vector3( 0.5f, -0.5f,  0.5f), new Vector3(0, -1, 0), Vector2.Zero),
            
            // Right face (X+)
            new(new Vector3( 0.5f, -0.5f,  0.5f), new Vector3(1, 0, 0), Vector2.Zero),
            new(new Vector3( 0.5f, -0.5f, -0.5f), new Vector3(1, 0, 0), Vector2.Zero),
            new(new Vector3( 0.5f,  0.5f,  0.5f), new Vector3(1, 0, 0), Vector2.Zero),
            new(new Vector3( 0.5f,  0.5f, -0.5f), new Vector3(1, 0, 0), Vector2.Zero),
            
            // Left face (X-)
            new(new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-1, 0, 0), Vector2.Zero),
            new(new Vector3(-0.5f, -0.5f,  0.5f), new Vector3(-1, 0, 0), Vector2.Zero),
            new(new Vector3(-0.5f,  0.5f, -0.5f), new Vector3(-1, 0, 0), Vector2.Zero),
            new(new Vector3(-0.5f,  0.5f,  0.5f), new Vector3(-1, 0, 0), Vector2.Zero)
        };

        var indices = new short[]
        {
            // Front
            0, 2, 1, 1, 2, 3,
            // Back
            4, 6, 5, 5, 6, 7,
            // Top
            8, 10, 9, 9, 10, 11,
            // Bottom
            12, 14, 13, 13, 14, 15,
            // Right
            16, 18, 17, 17, 18, 19,
            // Left
            20, 22, 21, 21, 22, 23
        };

        _cubeVertexBuffer = new VertexBuffer(_graphicsDevice, typeof(VertexPositionNormalTexture),
                                            vertices.Length, BufferUsage.WriteOnly);
        _cubeVertexBuffer.SetData(vertices);

        _cubeIndexBuffer = new IndexBuffer(_graphicsDevice, IndexElementSize.SixteenBits,
                                          indices.Length, BufferUsage.WriteOnly);
        _cubeIndexBuffer.SetData(indices);
        _cubeIndexCount = indices.Length;

        _logger.Information("Cloud cube geometry created: {VertexCount} vertices, {IndexCount} indices",
            vertices.Length, _cubeIndexCount);
    }

    /// <summary>
    /// Disposes resources used by the clouds.
    /// </summary>
    public void Dispose()
    {
        _cubeVertexBuffer?.Dispose();
        _cubeIndexBuffer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
