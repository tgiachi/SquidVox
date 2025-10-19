namespace SquidVox.Core.Interfaces.GameObjects;

/// <summary>
/// Interface for game objects that provide debugging functionality.
/// </summary>
public interface ISVoxDebuggerGameObject
{
    string WindowTitle { get; }

    void Draw();
}
