namespace SquidVox.World;

public static class Program
{
    public static void Main()
    {
        using var world = new SquidVoxWorld();
        world.Run();
    }
}
