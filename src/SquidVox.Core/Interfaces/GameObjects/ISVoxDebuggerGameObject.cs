namespace SquidVox.Core.Interfaces.GameObjects;

/// <summary>
/// Interface for game objects that provide debugging functionality.
/// </summary>
public interface ISVoxDebuggerGameObject
{

    bool IsVisible { get; set; }

    string WindowTitle { get; }

    void Draw();
}
