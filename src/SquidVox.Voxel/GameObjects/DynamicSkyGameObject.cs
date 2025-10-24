using System;
using System.Collections.Generic;
using DryIoc;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using SquidVox.Core.Context;
using SquidVox.Core.GameObjects;
using SquidVox.Core.Interfaces.Services;

namespace SquidVox.Voxel.GameObjects;

/// <summary>
/// Renders a procedural day/night cycle skydome (spherical sky) with sun and lighting.
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
    private Vector3 _sunDirection;
    private Color _ambientColor;
    private Color _directionalColor;
    private float _sunIntensity;

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

        // Initialize lighting values
        UpdateLighting();
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
    /// Gets the current sun direction vector (normalized).
    /// </summary>
    public Vector3 SunDirection => _sunDirection;

    /// <summary>
    /// Gets the current ambient light color based on time of day.
    /// </summary>
    public Color AmbientLightColor => _ambientColor;

    /// <summary>
    /// Gets the current directional (sun) light color based on time of day.
    /// </summary>
    public Color DirectionalLightColor => _directionalColor;

    /// <summary>
    /// Gets the current sun intensity (0.0 to 1.0).
    /// </summary>
    public float SunIntensity => _sunIntensity;

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

        UpdateLighting();
    }

    /// <summary>
    /// Updates sun direction and lighting colors based on time of day.
    /// </summary>
    private void UpdateLighting()
    {
        // Calculate sun angle (0 = midnight, 0.5 = noon)
        float sunAngle = _timeOfDay * MathHelper.TwoPi;
        float sunHeight = MathF.Sin(sunAngle);

        // Sun direction: rotates from east to west
        // At noon (0.5), sun is at zenith (0, 1, 0)
        // At sunrise/sunset, sun is on horizon
        _sunDirection = new Vector3(
            MathF.Cos(sunAngle),      // X: east-west
            sunHeight,                 // Y: height
            MathF.Sin(sunAngle) * 0.3f // Z: slight north-south variation
        );
        _sunDirection = Vector3.Normalize(_sunDirection);

        // Calculate sun intensity (0.0 at night, 1.0 at day)
        if (sunHeight > 0.0f)
        {
            _sunIntensity = MathHelper.Clamp(sunHeight * 1.5f, 0.0f, 1.0f);
        }
        else
        {
            _sunIntensity = 0.0f;
        }

        // Calculate lighting colors based on sun height
        if (sunHeight > 0.7f)
        {
            // Full day
            _ambientColor = new Color(150, 170, 200);
            _directionalColor = new Color(255, 250, 235);
        }
        else if (sunHeight > 0.0f)
        {
            // Day to sunset transition
            float blend = sunHeight / 0.7f;
            Color dayAmbient = new Color(150, 170, 200);
            Color sunsetAmbient = new Color(180, 120, 100);
            _ambientColor = Color.Lerp(sunsetAmbient, dayAmbient, blend);

            Color dayDirectional = new Color(255, 250, 235);
            Color sunsetDirectional = new Color(255, 180, 120);
            _directionalColor = Color.Lerp(sunsetDirectional, dayDirectional, blend);
        }
        else if (sunHeight > -0.3f)
        {
            // Sunset to night transition
            float blend = (sunHeight + 0.3f) / 0.3f;
            Color nightAmbient = new Color(20, 20, 40);
            Color sunsetAmbient = new Color(180, 120, 100);
            _ambientColor = Color.Lerp(nightAmbient, sunsetAmbient, blend);

            Color nightDirectional = new Color(40, 50, 80);
            Color sunsetDirectional = new Color(255, 180, 120);
            _directionalColor = Color.Lerp(nightDirectional, sunsetDirectional, blend);
        }
        else
        {
            // Full night
            _ambientColor = new Color(20, 20, 40);
            _directionalColor = new Color(40, 50, 80);
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

        // CRITICAL: Create World matrix that centers the skybox on the camera position
        // This makes the skybox follow the camera, giving the illusion of infinite distance
        const float skyboxSize = 500f;
        Matrix world = Matrix.CreateScale(skyboxSize) * Matrix.CreateTranslation(_camera.Position);

        _skyEffect.Parameters["World"].SetValue(world);
        _skyEffect.Parameters["Projection"].SetValue(_camera.Projection);
        _skyEffect.Parameters["View"].SetValue(_camera.View);
        _skyEffect.Parameters["Time"].SetValue(_timeOfDay);
        _skyEffect.Parameters["SunDirection"]?.SetValue(_sunDirection);
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
        // Create a unit cube centered at origin
        // The cube will be scaled and translated using the World matrix
        var vertices = new VertexPosition[]
        {
            // Front face (+Z)
            new(new Vector3(-1, -1,  1)),
            new(new Vector3( 1, -1,  1)),
            new(new Vector3(-1,  1,  1)),
            new(new Vector3( 1,  1,  1)),

            // Back face (-Z)
            new(new Vector3( 1, -1, -1)),
            new(new Vector3(-1, -1, -1)),
            new(new Vector3( 1,  1, -1)),
            new(new Vector3(-1,  1, -1)),

            // Top face (+Y)
            new(new Vector3(-1,  1,  1)),
            new(new Vector3( 1,  1,  1)),
            new(new Vector3(-1,  1, -1)),
            new(new Vector3( 1,  1, -1)),

            // Bottom face (-Y)
            new(new Vector3(-1, -1, -1)),
            new(new Vector3( 1, -1, -1)),
            new(new Vector3(-1, -1,  1)),
            new(new Vector3( 1, -1,  1)),

            // Right face (+X)
            new(new Vector3( 1, -1,  1)),
            new(new Vector3( 1, -1, -1)),
            new(new Vector3( 1,  1,  1)),
            new(new Vector3( 1,  1, -1)),

            // Left face (-X)
            new(new Vector3(-1, -1, -1)),
            new(new Vector3(-1, -1,  1)),
            new(new Vector3(-1,  1, -1)),
            new(new Vector3(-1,  1,  1))
        };

        var indices = new short[]
        {
            // Front face
            0, 1, 2, 2, 1, 3,
            // Back face
            4, 5, 6, 6, 5, 7,
            // Top face
            8, 9, 10, 10, 9, 11,
            // Bottom face
            12, 13, 14, 14, 13, 15,
            // Right face
            16, 17, 18, 18, 17, 19,
            // Left face
            20, 21, 22, 22, 21, 23
        };

        _vertexBuffer = new VertexBuffer(_graphicsDevice, typeof(VertexPosition),
                                        vertices.Length, BufferUsage.WriteOnly);
        _vertexBuffer.SetData(vertices);

        _indexBuffer = new IndexBuffer(_graphicsDevice, IndexElementSize.SixteenBits,
                                       indices.Length, BufferUsage.WriteOnly);
        _indexBuffer.SetData(indices);
        _indexCount = indices.Length;

        _logger.Information("Skybox cube created: {VertexCount} vertices, {IndexCount} indices",
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
