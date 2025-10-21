# SquidVox Project Analysis and Service Architecture Proposal

## Project Overview
SquidVox is a voxel engine project written in C# using MonoGame for graphics and game framework, TrippyGL as a graphics wrapper, and ImGui for UI. Additional technologies include ConsoleAppFramework for command-line argument handling, Serilog for logging, MoonSharp for Lua scripting integration, and FontStashSharp for font rendering. The goal is to create a voxel-based world, similar to Minecraft but customized. The current structure includes:
- **SquidVox.Core**: Shared interfaces, data structures, utilities, and services.
- **SquidVox.GameObjects.Base**: Base game object implementations.
- **SquidVox.GameObjects.UI**: UI components and controls.
- **SquidVox.Lua.Scripting**: Lua scripting engine and related utilities.
- **SquidVox.Voxel**: Voxel-specific data and types.
- **SquidVox.World3d**: Main game loop, graphics context, rendering layers, and services.
- **Tests**: Unit tests and benchmarks.

The architecture uses a static graphic context (SquidVoxGraphicContext) for global resources and an event-driven update/render loop in SquidVoxWorld. Rendering is organized into layers (ImGuiRenderLayer, GameObjectRenderLayer, SceneRenderLayer) for modular drawing. Lua scripting is integrated with custom modules (ConsoleModule, WindowModule) for extensibility.

## Service Architecture
The project uses **Dependency Injection (IoC)** with DryIoc for structuring services like AssetManagerService, SceneManagerService, and LuaScriptEngineService. This provides flexibility, testability, and decoupling without the rigidity of singletons or the complexity of GameObject patterns (which are better for in-world entities).

### Why IoC?
- **Flexibility**: Easy to swap implementations (e.g., mock services for testing).
- **Decoupling**: Services don't depend on global state.
- **Scalability**: Suitable for a voxel engine with multiple systems (rendering, world generation, assets).
- Alternatives considered:
  - **Singleton**: Simple but leads to tight coupling and hard-to-test code.
  - **GameObject**: Not ideal for global services; better for voxel entities or components.

### Implementation Steps
1. Add DryIoc to project dependencies.
2. Create interfaces for services (e.g., IAssetManagerService).
3. Register services in a Container during initialization.
4. Inject services into classes that need them (e.g., via constructor injection in SquidVoxWorld or subsystems).
5. For voxel-specific services, consider adding IVoxelRenderer, IWorldGenerator, etc., following the same pattern.

This structure will support building a robust voxel world while keeping the codebase maintainable.

### Implementation Example
Here's a simple example of how to register and use an AssetManagerService.

First, add to SquidVox.World.csproj:
```xml
<PackageReference Include="DryIoc" Version="5.4.3" />
```

Create an interface in SquidVox.Core/Services/IAssetManagerService.cs:
```csharp
namespace SquidVox.Core.Services;

public interface IAssetManagerService
{
    void LoadAsset(string assetName);
    // Other methods...
}
```

Implement the service in SquidVox.World/Services/AssetManagerService.cs:
```csharp
using SquidVox.Core.Services;

namespace SquidVox.World.Services;

public class AssetManagerService : IAssetManagerService
{
    public void LoadAsset(string assetName)
    {
        // Asset loading logic
        Console.WriteLine($"Loading asset: {assetName}");
    }
}
```

Modify Program.cs to register services:
```csharp
using DryIoc;
using SquidVox.Core.Services;
using SquidVox.World3d.Services;

namespace SquidVox.World3d;

public static class Program
{
    public static void Main()
    {
        var container = new Container();
        container.Register<IAssetManagerService, AssetManagerService>(Reuse.Singleton);
        container.Register<ISceneManager, SceneManagerService>(Reuse.Singleton);
        container.Register<IScriptEngineService, LuaScriptEngineService>(Reuse.Singleton);
        // Register other services here...

        using var world = new SquidVoxWorld(container);
        world.Run();
    }
}
```

Modify SquidVoxWorld.cs to accept IContainer and inject services:
```csharp
using DryIoc;
using SquidVox.Core.Services;

namespace SquidVox.World3d;

public class SquidVoxWorld : IDisposable
{
    private readonly IAssetManagerService _assetManager;
    private readonly ISceneManager _sceneManager;
    private readonly IScriptEngineService _scriptEngine;

    public SquidVoxWorld(IContainer container)
    {
        _assetManager = container.Resolve<IAssetManagerService>();
        _sceneManager = container.Resolve<ISceneManager>();
        _scriptEngine = container.Resolve<IScriptEngineService>();
        // Initialize other services...
    }

    // Rest of the code...
}
```

This allows injecting services where needed, keeping the code decoupled.

### Registered Custom Commands
- **/format**: Comments C# files lacking standard /// comments in English (adds missing XML comments to classes, methods, etc.).
- **/csfix**: Checks that each C# file contains exactly one class, struct, or record (reports violations).
- **/go**: Runs /format and /csfix in parallel on all project files.

## Code Style Guidelines

### File Structure
All C# files should follow this structure (in order):
1. **Using statements**
2. **Namespace declaration**
3. **Class/struct/record declaration with XML documentation**
4. **Private fields** (const, readonly, then regular fields)
5. **Events** (public events with XML documentation)
6. **Constructor(s)** (with XML documentation)
7. **Private methods** (grouped logically)
8. **Public properties** (with XML documentation, getters and setters)
9. **Public methods** (with XML documentation)

### Specific Rules
- **No `#region` directives**: Do not use `#region` or `#endregion` anywhere in the code
- **Properties at the top**: All public properties must be declared at the top of the class, after events and before methods
- **Events after fields**: All events must be declared immediately after private fields, before the constructor
- **No comments unless requested**: Do not add code comments unless explicitly asked by the user
- **XML documentation required**: All public members (classes, methods, properties, events) must have XML documentation comments (///)
- **XML documentation style**: All XML documentation should end with a period

### Example Structure
```csharp
using System;
using Microsoft.Xna.Framework;

namespace SquidVox.GameObjects.UI.Controls;

/// <summary>
/// Example game object demonstrating proper file structure.
/// </summary>
public class ExampleGameObject : Base2dGameObject
{
    private readonly string _fontName;
    private string _text;
    private bool _isInitialized;

    /// <summary>
    /// Event fired when something changes.
    /// </summary>
    public event EventHandler? SomethingChanged;

    /// <summary>
    /// Initializes a new instance of the ExampleGameObject class.
    /// </summary>
    public ExampleGameObject(string text = "Example")
    {
        _text = text;
    }

    private void HelperMethod()
    {
        // Implementation
    }

    /// <summary>
    /// Gets or sets the text.
    /// </summary>
    public string Text
    {
        get => _text;
        set => _text = value;
    }

    /// <summary>
    /// Performs some public action.
    /// </summary>
    public void DoSomething()
    {
        // Implementation
    }
}
```

---

## Sky Rendering with SkyPanorama Effect

SquidVox includes shader support for rendering dynamic panoramic skies with animation. The `SkyPanorama.fx` effect (converted from GLSL) enables scrolling 2D panoramic skyboxes ideal for creating day/night cycles or moving cloud effects.

### Overview

**SkyPanorama.fx** is a MonoGame HLSL shader that:
- Renders a 2D panoramic texture around the world
- Supports horizontal scrolling animation via a `Timer` parameter
- Uses texture wrapping for seamless infinite scrolling
- Optimized for Shader Model 3.0 (compatible with OpenGL/DirectX)

### Shader Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `Matrix` | `float4x4` | Combined World-View-Projection matrix |
| `Timer` | `float` | Animation time for scrolling (typically 0-1 range) |
| `SkyTexture` | `Texture2D` | Panoramic sky texture (seamless horizontal wrapping) |

### Basic Usage

#### 1. Load the Effect

```csharp
using Microsoft.Xna.Framework.Graphics;

public class SkyRenderer
{
    private Effect _skyEffect;
    private Texture2D _skyTexture;

    public void LoadContent(ContentManager content)
    {
        // Load the compiled effect
        _skyEffect = content.Load<Effect>("Effects/SkyPanorama");

        // Load your sky texture (should be seamless for wrapping)
        _skyTexture = content.Load<Texture2D>("Textures/SkyPanorama");
    }
}
```

#### 2. Setup Geometry

Create a large sphere or box around the camera to render the sky:

```csharp
private VertexBuffer _skyVertexBuffer;
private IndexBuffer _skyIndexBuffer;

private void CreateSkyGeometry(GraphicsDevice device)
{
    // Example: Create a large inverted sphere or box
    // Vertex format should include Position, Normal, UV
    var vertices = GenerateSkyGeometry(); // Your geometry generation

    _skyVertexBuffer = new VertexBuffer(
        device,
        typeof(VertexPositionNormalTexture),
        vertices.Length,
        BufferUsage.WriteOnly
    );
    _skyVertexBuffer.SetData(vertices);

    // Setup indices...
}
```

#### 3. Render the Sky

```csharp
public void Draw(GameTime gameTime, Matrix view, Matrix projection)
{
    var device = _skyEffect.GraphicsDevice;

    // Calculate world matrix (centered on camera)
    var cameraPosition = ExtractCameraPosition(view);
    var world = Matrix.CreateTranslation(cameraPosition);

    // Combine matrices
    var worldViewProjection = world * view * projection;

    // Set effect parameters
    _skyEffect.Parameters["Matrix"].SetValue(worldViewProjection);
    _skyEffect.Parameters["SkyTexture"].SetValue(_skyTexture);

    // Animate the sky: scroll slowly over time
    // Use modulo to keep value in 0-1 range for seamless wrapping
    var scrollSpeed = 0.01f; // Adjust for desired speed
    var timer = (float)(gameTime.TotalGameTime.TotalSeconds * scrollSpeed) % 1.0f;
    _skyEffect.Parameters["Timer"].SetValue(timer);

    // Render settings for sky
    var previousDepthStencil = device.DepthStencilState;
    var previousRasterizer = device.RasterizerState;

    // Disable depth writes (sky is always behind everything)
    device.DepthStencilState = DepthStencilState.DepthRead;
    device.RasterizerState = RasterizerState.CullNone; // Or CullClockwise for inverted geometry

    // Apply the effect and render
    _skyEffect.CurrentTechnique = _skyEffect.Techniques["SkyPanorama"];
    foreach (var pass in _skyEffect.CurrentTechnique.Passes)
    {
        pass.Apply();
        device.SetVertexBuffer(_skyVertexBuffer);
        device.Indices = _skyIndexBuffer;
        device.DrawIndexedPrimitives(
            PrimitiveType.TriangleList,
            0,
            0,
            _indexCount / 3
        );
    }

    // Restore render states
    device.DepthStencilState = previousDepthStencil;
    device.RasterizerState = previousRasterizer;
}

private Vector3 ExtractCameraPosition(Matrix viewMatrix)
{
    var inverted = Matrix.Invert(viewMatrix);
    return inverted.Translation;
}
```

### Advanced Usage

#### Custom Animation

Instead of linear scrolling, you can implement custom animations:

```csharp
// Day/night cycle: timer value represents time of day
var dayProgress = (float)(gameTime.TotalGameTime.TotalHours % 24.0) / 24.0f;
_skyEffect.Parameters["Timer"].SetValue(dayProgress);

// Pulsing/wave effect
var wave = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds) * 0.5f + 0.5f;
_skyEffect.Parameters["Timer"].SetValue(wave);
```

#### Multiple Sky Layers

Render multiple sky layers at different speeds for parallax effects:

```csharp
public void DrawLayeredSky(GameTime gameTime, Matrix view, Matrix projection)
{
    // Layer 1: Slow-moving clouds
    DrawSkyLayer(_cloudTexture, gameTime, view, projection, scrollSpeed: 0.005f, scale: 1.0f);

    // Layer 2: Faster stars
    DrawSkyLayer(_starTexture, gameTime, view, projection, scrollSpeed: 0.02f, scale: 1.1f);
}

private void DrawSkyLayer(Texture2D texture, GameTime gameTime, Matrix view, Matrix projection, float scrollSpeed, float scale)
{
    // Implementation similar to Draw() but with different parameters
}
```

### Texture Requirements

For best results, your sky texture should be:
- **Seamless horizontally**: Left edge connects perfectly to right edge
- **Wide aspect ratio**: Recommended 4:1 or wider (e.g., 2048x512)
- **Proper UV mapping**: U coordinate (horizontal) should wrap from 0 to 1
- **Appropriate resolution**: Balance quality vs performance (1024x256 to 4096x1024)

### Performance Tips

1. **Render First**: Draw sky before other geometry to leverage early-z rejection
2. **Use Mipmaps**: Enable mipmapping for distant pixels
3. **Optimize Geometry**: Use low-poly sphere/box (16-32 segments is sufficient)
4. **Batch Rendering**: If using multiple layers, minimize state changes

### Integration with AssetManagerService

```csharp
public class SkyRenderer
{
    private readonly IAssetManagerService _assetManager;
    private Effect _skyEffect;

    public SkyRenderer(IAssetManagerService assetManager)
    {
        _assetManager = assetManager;
    }

    public void Initialize()
    {
        // Load through asset manager
        _skyEffect = _assetManager.LoadEffect("Effects/SkyPanorama");
        var skyTexture = _assetManager.LoadTexture2D("Textures/SkyPanorama");
        _skyEffect.Parameters["SkyTexture"]?.SetValue(skyTexture);
    }
}
```

### Troubleshooting

**Sky appears stretched or distorted:**
- Check UV coordinates on your geometry
- Ensure texture aspect ratio matches geometry UV layout

**Seams visible during scrolling:**
- Verify texture is seamless on left/right edges
- Check AddressU is set to Wrap in sampler state

**Sky renders in front of geometry:**
- Ensure `DepthStencilState.DepthRead` is used
- Verify geometry scale is large enough to encompass the scene

**Performance issues:**
- Reduce sky geometry polygon count
- Lower texture resolution
- Consider rendering sky to a render target and reusing across frames

### Example Complete Implementation

```csharp
public class PanoramicSkyRenderer : IDisposable
{
    private readonly GraphicsDevice _device;
    private Effect _effect;
    private VertexBuffer _vertexBuffer;
    private IndexBuffer _indexBuffer;
    private int _indexCount;

    public PanoramicSkyRenderer(GraphicsDevice device, Effect effect, Texture2D texture)
    {
        _device = device;
        _effect = effect;
        _effect.Parameters["SkyTexture"]?.SetValue(texture);
        CreateSkyGeometry();
    }

    private void CreateSkyGeometry()
    {
        const int segments = 32;
        const float radius = 1000f;

        // Generate inverted sphere geometry
        var vertices = new List<VertexPositionNormalTexture>();
        var indices = new List<short>();

        for (int lat = 0; lat <= segments; lat++)
        {
            float theta = lat * MathHelper.Pi / segments;
            float sinTheta = (float)Math.Sin(theta);
            float cosTheta = (float)Math.Cos(theta);

            for (int lon = 0; lon <= segments; lon++)
            {
                float phi = lon * MathHelper.TwoPi / segments;
                float sinPhi = (float)Math.Sin(phi);
                float cosPhi = (float)Math.Cos(phi);

                var position = new Vector3(
                    radius * sinTheta * cosPhi,
                    radius * cosTheta,
                    radius * sinTheta * sinPhi
                );

                var normal = Vector3.Normalize(-position); // Inverted
                var uv = new Vector2((float)lon / segments, (float)lat / segments);

                vertices.Add(new VertexPositionNormalTexture(position, normal, uv));
            }
        }

        // Generate indices
        for (int lat = 0; lat < segments; lat++)
        {
            for (int lon = 0; lon < segments; lon++)
            {
                int first = lat * (segments + 1) + lon;
                int second = first + segments + 1;

                indices.Add((short)first);
                indices.Add((short)(second + 1));
                indices.Add((short)second);

                indices.Add((short)first);
                indices.Add((short)(first + 1));
                indices.Add((short)(second + 1));
            }
        }

        _vertexBuffer = new VertexBuffer(_device, typeof(VertexPositionNormalTexture),
                                        vertices.Count, BufferUsage.WriteOnly);
        _vertexBuffer.SetData(vertices.ToArray());

        _indexBuffer = new IndexBuffer(_device, IndexElementSize.SixteenBits,
                                       indices.Count, BufferUsage.WriteOnly);
        _indexBuffer.SetData(indices.ToArray());
        _indexCount = indices.Count;
    }

    public void Render(Matrix view, Matrix projection, float animationTime)
    {
        var cameraPos = Matrix.Invert(view).Translation;
        var world = Matrix.CreateTranslation(cameraPos);
        var wvp = world * view * projection;

        _effect.Parameters["Matrix"]?.SetValue(wvp);
        _effect.Parameters["Timer"]?.SetValue(animationTime % 1.0f);

        var oldDepth = _device.DepthStencilState;
        var oldRaster = _device.RasterizerState;

        _device.DepthStencilState = DepthStencilState.DepthRead;
        _device.RasterizerState = RasterizerState.CullClockwise;

        _device.SetVertexBuffer(_vertexBuffer);
        _device.Indices = _indexBuffer;

        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _indexCount / 3);
        }

        _device.DepthStencilState = oldDepth;
        _device.RasterizerState = oldRaster;
    }

    public void Dispose()
    {
        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
    }
}
```

This implementation provides a complete, production-ready panoramic sky renderer that can be easily integrated into the SquidVox rendering pipeline.