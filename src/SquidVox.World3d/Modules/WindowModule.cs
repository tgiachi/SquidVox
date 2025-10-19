using Microsoft.Xna.Framework;
using SquidVox.Core.Attributes.Scripts;

namespace SquidVox.World3d.Modules;

/// <summary>
/// Module for managing the game window.
/// </summary>
[ScriptModule("window", "Provides functions to create and manage in-game windows.")]
public class WindowModule
{
    [ScriptFunction("set_title", "Sets the title of the game window.")]
    public void SetTitle(string title)
    {
    }

    [ScriptFunction("set_size", "Sets the size of the game window.")]
    public void SetSize(int width, int height)
    {
    }

    [ScriptFunction("get_size", "Gets the current size of the game window.")]
    public Vector2 GetSize()
    {
        return Vector2.One;
    }

    [ScriptFunction("get_title", "Gets the current title of the game window.")]
    public string GetTitle()
    {
        return "";
    }
}
