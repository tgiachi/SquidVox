using System;
using DryIoc;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using SquidVox.Core.Context;
using SquidVox.Core.GameObjects;
using SquidVox.Core.Interfaces.Services;

namespace SquidVox.Voxel.GameObjects;

/// <summary>
/// Renders a procedural day/night cycle skybox.
/// </summary>
public class DynamicSkyGameObject : Base3dGameObject, IDisposable
{
    private readonly ILogger _logger = Log.ForContext<DynamicSkyGameObject>();
    private readonly GraphicsDevice _graphicsDevice;
    private readonly Effect _skyEffect;
    private readonly CameraGameObject _camera;
    private Texture2D? _skyTexture;
    private bool _useSkyTexture;
    private float _textureBlend = 1f;
    private VertexBuffer? _vertexBuffer;
    private IndexBuffer? _indexBuffer;
    private int _indexCount;
    private float _timeOfDay;

    /// <summary>
    /// Initializes a new instance of the DynamicSkyGameObject class.
    /// </summary>
    /// <param name="camera">The camera to use for rendering.</param>
    public DynamicSkyGameObject(CameraGameObject camera)
    {
        _camera = camera ?? throw new ArgumentNullException(nameof(camera));
        _graphicsDevice = SquidVoxEngineContext.GraphicsDevice;

        var assetManager = SquidVoxEngineContext.Container.Resolve<IAssetManagerService>();
        _skyEffect = assetManager.GetEffect("Effects/DynamicSky");

        if (_skyEffect == null)
        {
            _logger.Error("Failed to load DynamicSky effect");
            throw new InvalidOperationException("DynamicSky effect not loaded");
        }

        _logger.Information("DynamicSky initialized");

        CreateSkyGeometry();
    }

    /// <summary>
    /// Gets or sets the time of day (0.0 = midnight, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset, 1.0 = midnight).
    /// </summary>
    public float TimeOfDay
    {
        get => _timeOfDay;
        set => _timeOfDay = value % 1.0f;
    }

    /// <summary>
    /// Gets or sets the day/night cycle speed (time units per second).
    /// </summary>
    public float CycleSpeed { get; set; } = 0.01f;

    /// <summary>
    /// Gets or sets the blend factor used when combining the procedural sky with the texture.
    /// </summary>
    public float SkyTextureBlend
    {
        get => _textureBlend;
        set => _textureBlend = MathHelper.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the sky texture should be used.
    /// </summary>
    public bool UseSkyTexture
    {
        get => _useSkyTexture;
        set => _useSkyTexture = value;
    }

    /// <summary>
    /// Gets the current sky texture.
    /// </summary>
    public Texture2D? SkyTexture => _skyTexture;

    /// <summary>
    /// Gets or sets whether the day/night cycle is enabled.
    /// </summary>
    public bool EnableCycle { get; set; } = true;

    /// <summary>
    /// Sets the texture to blend with the procedural sky.
    /// </summary>
    /// <param name="texture">The texture to apply.</param>
    /// <param name="blend">The blend factor to apply.</param>
    public void SetSkyTexture(Texture2D texture, float blend = 1f)
    {
        _skyTexture = texture ?? throw new ArgumentNullException(nameof(texture));
        SkyTextureBlend = blend;
        _useSkyTexture = true;
    }

    /// <summary>
    /// Attempts to load and assign a sky texture by name using the asset manager.
    /// </summary>
    /// <param name="textureName">The name of the texture asset.</param>
    /// <param name="blend">The blend factor to apply.</param>
    /// <returns>True if the texture was set; otherwise, false.</returns>
    public bool TrySetSkyTexture(string textureName, float blend = 1f)
    {
        if (string.IsNullOrWhiteSpace(textureName))
        {
            throw new ArgumentException("Texture name cannot be null or whitespace.", nameof(textureName));
        }

        var assetManager = SquidVoxEngineContext.Container.Resolve<IAssetManagerService>();
        var texture = assetManager.GetTexture(textureName);

        if (texture == null)
        {
            _logger.Warning("Sky texture '{TextureName}' not found.", textureName);
            return false;
        }

        SetSkyTexture(texture, blend);
        return true;
    }

    /// <summary>
    /// Clears the current sky texture.
    /// </summary>
    public void ClearSkyTexture()
    {
        _skyTexture = null;
        _useSkyTexture = false;
    }

    /// <summary>
    /// Updates the sky animation.
    /// </summary>
    /// <param name="gameTime">Game timing information.</param>
    protected override void OnUpdate(GameTime gameTime)
    {
        if (EnableCycle)
        {
            _timeOfDay += (float)gameTime.ElapsedGameTime.TotalSeconds * CycleSpeed;
            _timeOfDay %= 1.0f;
        }
    }

    /// <summary>
    /// Renders the dynamic sky.
    /// </summary>
    /// <param name="gameTime">Game timing information.</param>
    protected override void OnDraw3d(GameTime gameTime)
    {
        if (_vertexBuffer == null || _indexBuffer == null)
        {
            _logger.Warning("Vertex or index buffer is null, skipping render");
            return;
        }

        if (_skyEffect.Parameters["Projection"] == null)
        {
            _logger.Error("Projection parameter not found in effect");
            return;
        }

        if (_skyEffect.Parameters["View"] == null)
        {
            _logger.Error("View parameter not found in effect");
            return;
        }

        if (_skyEffect.Parameters["Time"] == null)
        {
            _logger.Error("Time parameter not found in effect");
            return;
        }

        _skyEffect.Parameters["Projection"].SetValue(_camera.Projection);
        _skyEffect.Parameters["View"].SetValue(_camera.View);
        _skyEffect.Parameters["Time"].SetValue(_timeOfDay);
        _skyEffect.Parameters["UseTexture"]?.SetValue(_useSkyTexture && _skyTexture != null && _textureBlend > 0f ? 1f : 0f);
        _skyEffect.Parameters["TextureStrength"]?.SetValue(_textureBlend);

        if (_skyTexture != null && _useSkyTexture)
        {
            _skyEffect.Parameters["SkyTexture"]?.SetValue(_skyTexture);
        }

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

        foreach (var pass in _skyEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _indexCount / 3);
        }

        _graphicsDevice.DepthStencilState = oldDepth;
        _graphicsDevice.RasterizerState = oldRaster;
    }

    private void CreateSkyGeometry()
    {
        var vertices = new VertexPosition[]
        {
            new(new Vector3(-1, -1,  1)),
            new(new Vector3( 1, -1,  1)),
            new(new Vector3(-1,  1,  1)),
            new(new Vector3( 1,  1,  1)),

            new(new Vector3(-1, -1, -1)),
            new(new Vector3(-1,  1, -1)),
            new(new Vector3( 1, -1, -1)),
            new(new Vector3( 1,  1, -1)),

            new(new Vector3(-1,  1, -1)),
            new(new Vector3(-1,  1,  1)),
            new(new Vector3( 1,  1, -1)),
            new(new Vector3( 1,  1,  1)),

            new(new Vector3(-1, -1, -1)),
            new(new Vector3( 1, -1, -1)),
            new(new Vector3(-1, -1,  1)),
            new(new Vector3( 1, -1,  1)),

            new(new Vector3( 1, -1, -1)),
            new(new Vector3( 1,  1, -1)),
            new(new Vector3( 1, -1,  1)),
            new(new Vector3( 1,  1,  1)),

            new(new Vector3(-1, -1, -1)),
            new(new Vector3(-1, -1,  1)),
            new(new Vector3(-1,  1, -1)),
            new(new Vector3(-1,  1,  1))
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

        _vertexBuffer = new VertexBuffer(_graphicsDevice, typeof(VertexPosition),
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
    /// Disposes resources used by the dynamic sky.
    /// </summary>
    public void Dispose()
    {
        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
