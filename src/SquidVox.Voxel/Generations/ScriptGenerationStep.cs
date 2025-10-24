using SquidVox.Voxel.Interfaces.Generation.Pipeline;

namespace SquidVox.Voxel.Generations;

/// <summary>
/// A generation step that executes a script function.
/// </summary>
public class ScriptGenerationStep : IGeneratorStep
{
    public string Name { get; }

    private readonly Action<IGeneratorContext> _contextAction;

    public ScriptGenerationStep(string name, Action<IGeneratorContext> context)
    {
        Name = name;
        _contextAction = context;
    }

    public async Task ExecuteAsync(IGeneratorContext context)
    {
        await Task.Run(() => _contextAction(context));
    }
}
