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