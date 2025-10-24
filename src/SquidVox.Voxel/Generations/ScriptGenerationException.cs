using System;

namespace SquidVox.Voxel.Generations;

/// <summary>
///     Represents an error that occurred while executing a JavaScript-backed generation step.
/// </summary>
public class ScriptGenerationException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ScriptGenerationException"/> class.
    /// </summary>
    /// <param name="stepName">The name of the generation step.</param>
    /// <param name="message">The error message.</param>
    /// <param name="lineNumber">The line number where the error occurred.</param>
    /// <param name="columnNumber">The column number where the error occurred.</param>
    /// <param name="scriptStackTrace">The JavaScript stack trace, if available.</param>
    /// <param name="innerException">The underlying exception.</param>
    public ScriptGenerationException(
        string stepName,
        string message,
        int? lineNumber,
        int? columnNumber,
        string? scriptStackTrace,
        Exception innerException)
        : base(message, innerException)
    {
        StepName = stepName;
        LineNumber = lineNumber;
        ColumnNumber = columnNumber;
        ScriptStackTrace = scriptStackTrace;
    }

    /// <summary>
    ///     Gets the name of the generation step that caused the error.
    /// </summary>
    public string StepName { get; }

    /// <summary>
    ///     Gets the line number where the error occurred, if available.
    /// </summary>
    public int? LineNumber { get; }

    /// <summary>
    ///     Gets the column number where the error occurred, if available.
    /// </summary>
    public int? ColumnNumber { get; }

    /// <summary>
    ///     Gets the JavaScript stack trace associated with the error, if available.
    /// </summary>
    public string? ScriptStackTrace { get; }
}
