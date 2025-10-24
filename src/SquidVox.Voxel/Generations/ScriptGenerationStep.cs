using Jint;
using Jint.Runtime;
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
        try
        {
            await Task.Run(() => _contextAction(context));
        }
        catch (JavaScriptException jsEx)
        {
            var location = jsEx.Location;
            int? lineNumber = location.Start.Line > 0 ? location.Start.Line : null;
            int? columnNumber = location.Start.Column > 0 ? location.Start.Column : null;

            var stackTrace = ExtractScriptStackTrace(jsEx);

            var locationDescription = lineNumber.HasValue
                ? $" at line {lineNumber}, column {columnNumber}"
                : string.Empty;

            var message = $"JavaScript error in generation step '{Name}'{locationDescription}: {jsEx.Message}";

            throw new ScriptGenerationException(Name, message, lineNumber, columnNumber, stackTrace, jsEx);
        }
        catch (Exception ex)
        {
            var message = $"Error in generation step '{Name}': {ex.Message}";
            throw new ScriptGenerationException(Name, message, null, null, ex.StackTrace, ex);
        }
    }

    private static string? ExtractScriptStackTrace(JavaScriptException jsException)
    {
        try
        {
            var errorValue = jsException.Error;
            if (errorValue.IsObject())
            {
                var stackProperty = errorValue.AsObject().Get("stack");
                if (!stackProperty.IsUndefined())
                {
                    return stackProperty.ToString();
                }
            }
        }
        catch
        {
            // Ignore extraction errors and fall back to .NET stack trace.
        }

        return jsException.StackTrace;
    }
}
