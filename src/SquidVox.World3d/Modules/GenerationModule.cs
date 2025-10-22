using MoonSharp.Interpreter;
using SquidVox.Core.Attributes.Scripts;
using SquidVox.Voxel.Generations;
using SquidVox.Voxel.Interfaces.Services;

namespace SquidVox.World3d.Modules;

[ScriptModule("generation", "Provides chunk generation functionalities.")]
public class GenerationModule
{
    private readonly IChunkGeneratorService _chunkGeneratorService;

    public GenerationModule(IChunkGeneratorService chunkGeneratorService)
    {
        _chunkGeneratorService = chunkGeneratorService;
    }


    [ScriptFunction("add_step", "Adds a new generation step to the chunk generation pipeline.")]
    public void AddGenerationStep(string name, Closure step)
    {
        _chunkGeneratorService.AddGeneratorStep(
            new LuaGenerationStep(
                name,
                context => { step.Call(DynValue.FromObject(null, context)); }
            )
        );
    }
}
