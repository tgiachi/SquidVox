using SquidVox.Core.Interfaces.GameObjects;

namespace SquidVox.World3d.Scripts;

public class LuaImGuiDebuggerObject : ISVoxDebuggerGameObject
{
    public string WindowTitle { get; }

    private readonly Action _callBack;

    public LuaImGuiDebuggerObject(string windowTitle, Action callBack)
    {
        WindowTitle = windowTitle;
        _callBack = callBack;
    }

    public void Draw()
    {
        _callBack.Invoke();
    }
}
