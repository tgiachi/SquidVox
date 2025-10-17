namespace SquidVox.Core.Interfaces.GameObjects;

public interface ISVoxGameObject
{
    string Name { get; set; }

    int ZIndex { get; set; }

    bool IsEnabled { get; set; }

    bool IsVisible { get; set; }


}
