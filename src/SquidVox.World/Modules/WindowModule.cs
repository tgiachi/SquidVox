using Silk.NET.Maths;
using SquidVox.Core.Attributes.Scripts;
using SquidVox.World.Context;

namespace SquidVox.World.Modules;

[ScriptModule("window", "Provides functions to create and manage in-game windows.")]
public class WindowModule
{
    [ScriptFunction("set_title", "Sets the title of the game window.")]
    public void SetTitle(string title)
    {
        SquidVoxGraphicContext.Window.Title = title;
    }

    [ScriptFunction("set_size", "Sets the size of the game window.")]
    public void SetSize(int width, int height)
    {
        SquidVoxGraphicContext.Window.Size = new Vector2D<int>(width, height);
    }

    [ScriptFunction("get_size", "Gets the current size of the game window.")]
    public Vector2D<int> GetSize()
    {
        return SquidVoxGraphicContext.Window.Size;
    }

    [ScriptFunction("get_title", "Gets the current title of the game window.")]
    public string GetTitle()
    {
        return SquidVoxGraphicContext.Window.Title;
    }
}
