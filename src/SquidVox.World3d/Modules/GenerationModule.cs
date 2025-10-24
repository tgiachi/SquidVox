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

    [ScriptFunction(helpText: "Adds a new generation step to the chunk generation pipeline.")]
    public void AddStep(string name, Action<object> step)
    {
        _chunkGeneratorService.AddGeneratorStep(
            new ScriptGenerationStep(
                name,
                context => { step(context); }
            )
        );
    }
}
