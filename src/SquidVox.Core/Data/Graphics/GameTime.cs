namespace SquidVox.Core.Data.Graphics;

/// <summary>
/// Represents the game time, including delta and total time.
/// </summary>
public class GameTime
{
    /// <summary>
    /// Gets or sets the delta time.
    /// </summary>
    public double DeltaTime { get; set; }
    /// <summary>
    /// Gets or sets the total time.
    /// </summary>
    public double TotalTime { get; set; }

    /// <summary>
    /// Gets the total time as a TimeSpan.
    /// </summary>
    public TimeSpan TotalTimeSpan => TimeSpan.FromSeconds(TotalTime);

    /// <summary>
    /// Gets the delta time as a TimeSpan.
    /// </summary>
    public TimeSpan DeltaTimeSpan => TimeSpan.FromSeconds(DeltaTime);

    /// <summary>
    /// Updates the game time with the new delta.
    /// </summary>
    /// <param name="deltaTime">The delta time.</param>
    public void Update(double deltaTime)
    {
        DeltaTime = deltaTime;
        TotalTime += deltaTime;
    }


    /// <summary>
    /// Returns a string representation of the game time.
    /// </summary>
    /// <returns>A string describing the total and delta time.</returns>
    public override string ToString()
    {
        return $"Total Time: {TotalTimeSpan}, Delta Time: {DeltaTimeSpan}";
    }
}
