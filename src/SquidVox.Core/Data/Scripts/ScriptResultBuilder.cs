namespace SquidVox.Core.Data.Scripts;

/// <summary>
/// Builder class for creating ScriptResult instances.
/// </summary>
public class ScriptResultBuilder
{
    private object? _data;
    private string _message = string.Empty;
    private bool _success;

    /// <summary>
    ///
    /// </summary>
    public static ScriptResultBuilder CreateSuccess()
    {
        return new ScriptResultBuilder().WithSuccess(true);
    }

    /// <summary>
    ///
    /// </summary>
    public static ScriptResultBuilder CreateError()
    {
        return new ScriptResultBuilder().WithSuccess(false);
    }

    /// <summary>
    ///
    /// </summary>
    public ScriptResultBuilder WithSuccess(bool success)
    {
        _success = success;
        return this;
    }

    /// <summary>
    ///
    /// </summary>
    public ScriptResultBuilder WithMessage(string message)
    {
        _message = message;
        return this;
    }

    /// <summary>
    ///
    /// </summary>
    public ScriptResultBuilder WithData(object? data)
    {
        _data = data;
        return this;
    }

    /// <summary>
    ///
    /// </summary>
    public ScriptResultBuilder Success()
    {
        _success = true;
        return this;
    }

    /// <summary>
    ///
    /// </summary>
    public ScriptResultBuilder Failure()
    {
        _success = false;
        return this;
    }

    /// <summary>
    ///
    /// </summary>
    public ScriptResult Build()
    {
        return new ScriptResult
        {
            Success = _success,
            Message = _message,
            Data = _data
        };
    }
}