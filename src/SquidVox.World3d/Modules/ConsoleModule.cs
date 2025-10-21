using System.Globalization;
using Serilog;
using SquidVox.Core.Attributes.Scripts;

namespace SquidVox.World3d.Modules;

/// <summary>
///     Console API implementation (console.log, console.error, etc.)
/// </summary>
[ScriptModule("console", "Console API for logging and debugging")]
/// <summary>
/// 
/// </summary>
public class ConsoleModule
{
    private readonly ILogger _logger = Serilog.Log.ForContext<ConsoleModule>();

    /// <summary>
    /// 
    /// </summary>
    public ConsoleModule()
    {
    }

    [ScriptFunction(functionName: "log")]
    /// <summary>
    /// 
    /// </summary>
    public void Log(params object[] args)
    {
        var message = string.Join(" ", args.Select(FormatArg));
        _logger.Information("[Console] {Message}", message);
    }

[ScriptFunction(functionName: "info")]
/// <summary>
/// Logs an info message to the console.
/// </summary>
/// <param name="args">The arguments to log.</param>
public void Info(params object[] args)
    {
        var message = string.Join(" ", args.Select(FormatArg));
        _logger.Information("[Console] {Message}", message);
    }

[ScriptFunction(functionName: "warn")]
/// <summary>
/// Logs a warning message to the console.
/// </summary>
/// <param name="args">The arguments to log.</param>
public void Warn(params object[] args)
    {
        var message = string.Join(" ", args.Select(FormatArg));
        _logger.Warning("[Console] {Message}", message);
    }

[ScriptFunction(functionName: "error")]
/// <summary>
/// Logs an error message to the console.
/// </summary>
/// <param name="args">The arguments to log.</param>
public void Error(params object[] args)
    {
        var message = string.Join(" ", args.Select(FormatArg));
        _logger.Error("[Console] {Message}", message);
    }

[ScriptFunction(functionName: "debug")]
/// <summary>
/// Logs a debug message to the console.
/// </summary>
/// <param name="args">The arguments to log.</param>
public void Debug(params object[] args)
    {
        var message = string.Join(" ", args.Select(FormatArg));
        _logger.Debug("[Console] {Message}", message);
    }

[ScriptFunction(functionName: "trace")]
/// <summary>
/// Logs a trace message to the console with stack trace.
/// </summary>
/// <param name="args">The arguments to log.</param>
public void Trace(params object[] args)
    {
        var message = string.Join(" ", args.Select(FormatArg));
        var stackTrace = Environment.StackTrace;
        _logger.Debug("[Console] {Message}\n{StackTrace}", message, stackTrace);
    }

[ScriptFunction(functionName: "clear")]
/// <summary>
/// Clears the console.
/// </summary>
public void Clear()
    {
        _logger.Information("[Console] Console cleared");
        // In a real implementation, this could clear the console component
    }

[ScriptFunction(functionName: "assert")]
/// <summary>
/// Asserts a condition and logs an error if false.
/// </summary>
/// <param name="condition">The condition to assert.</param>
/// <param name="args">The arguments to log if assertion fails.</param>
public void Assert(bool condition, params object[] args)
    {
        if (!condition)
        {
            var message = args.Length > 0 ? string.Join(" ", args.Select(FormatArg)) : "Assertion failed";
            _logger.Error("[Console] Assertion failed: {Message}", message);
        }
    }


    private static string FormatArg(object? arg)
    {
        if (arg == null)
        {
            return "null";
        }

        if (arg is string str)
        {
            return str;
        }

        if (arg is bool b)
        {
            return b.ToString().ToLower(CultureInfo.InvariantCulture);
        }

        return arg.ToString() ?? "undefined";
    }
}
