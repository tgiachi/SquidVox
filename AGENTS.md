# SquidVox Project Analysis and Service Architecture Proposal

## Project Overview
SquidVox is a voxel engine project written in C# using Silk.NET for OpenGL graphics, TrippyGL as a graphics wrapper, and ImGui for UI. The goal is to create a voxel-based world, similar to Minecraft but customized. The current structure includes:
- **SquidVox.Core**: Shared data and utilities (e.g., GameTime, ColorExtensions).
- **SquidVox.World**: Main game loop, graphics context, and services like FontStashRenderer and Texture2DManager.
- **Tests**: Basic unit tests.

The architecture uses a static graphic context (SquidVoxGraphicContext) for global resources and an event-driven update/render loop in SquidVoxWorld.

## Proposed Service Architecture
For structuring services like AssetManagerService, I recommend using **Dependency Injection (IoC)** with DryIoc. This provides flexibility, testability, and decoupling without the rigidity of singletons or the complexity of GameObject patterns (which are better for in-world entities).

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
using SquidVox.World.Services;

namespace SquidVox.World;

public static class Program
{
    public static void Main()
    {
        var container = new Container();
        container.Register<IAssetManagerService, AssetManagerService>(Reuse.Singleton);
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

namespace SquidVox.World;

public class SquidVoxWorld : IDisposable
{
    private readonly IAssetManagerService _assetManager;

    public SquidVoxWorld(IContainer container)
    {
        _assetManager = container.Resolve<IAssetManagerService>();
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