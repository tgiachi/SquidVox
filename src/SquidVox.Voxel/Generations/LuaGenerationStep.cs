using SquidVox.Voxel.Interfaces.Generation.Pipeline;

namespace SquidVox.Voxel.Generations;

public class LuaGenerationStep : IGeneratorStep
{
    public string Name { get; }

    private readonly Action<IGeneratorContext> _contextAction;

    public LuaGenerationStep(string name, Action<IGeneratorContext> context)
    {
        Name = name;
        _contextAction = context;
    }

    public async Task ExecuteAsync(IGeneratorContext context)
    {
        await Task.Run(() => _contextAction(context));
    }
}
