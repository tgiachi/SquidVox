using Microsoft.Xna.Framework;
using SquidVox.Core.Attributes.Scripts;
using SquidVox.World3d.Context;

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
        SquidVoxGraphicContext.Window.Title = title;
    }

    [ScriptFunction("set_size", "Sets the size of the game window.")]
    public void SetSize(int width, int height)
    {
        SquidVoxGraphicContext.GraphicsDeviceManager.PreferredBackBufferWidth = width;
        SquidVoxGraphicContext.GraphicsDeviceManager.PreferredBackBufferHeight = height;
        SquidVoxGraphicContext.GraphicsDeviceManager.ApplyChanges();
    }

    [ScriptFunction("get_size", "Gets the current size of the game window.")]
    public Vector2 GetSize()
    {
        return new Vector2(
            SquidVoxGraphicContext.GraphicsDeviceManager.PreferredBackBufferWidth,
            SquidVoxGraphicContext.GraphicsDeviceManager.PreferredBackBufferHeight
        );
    }

    [ScriptFunction("get_title", "Gets the current title of the game window.")]
    public string GetTitle()
    {
        return SquidVoxGraphicContext.Window.Title;
    }
}
