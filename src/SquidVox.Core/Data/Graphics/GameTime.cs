namespace SquidVox.Core.Data.Graphics;

public class GameTime
{
    public double DeltaTime { get; set; }
    public double TotalTime { get; set; }

    public TimeSpan TotalTimeSpan => TimeSpan.FromSeconds(TotalTime);

    public TimeSpan DeltaTimeSpan => TimeSpan.FromSeconds(DeltaTime);

    public void Update(double deltaTime)
    {
        DeltaTime = deltaTime;
        TotalTime += deltaTime;
    }


    public override string ToString()
    {
        return $"Total Time: {TotalTimeSpan}, Delta Time: {DeltaTimeSpan}";
    }
}
