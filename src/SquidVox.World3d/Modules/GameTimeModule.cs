using SquidVox.Core.Attributes.Scripts;
using SquidVox.Core.Context;

namespace SquidVox.World3d.Modules;


[ScriptModule("game_time", "Module for managing game time.")]
public class GameTimeModule
{


    [ScriptFunction("getTotalSeconds", "Gets the total elapsed game time in seconds.")]
    public float GetTotalSeconds()
    {
        return (float)SquidVoxEngineContext.GameTime.TotalGameTime.TotalSeconds;
    }

    [ScriptFunction("getTotalMilliseconds", "Gets the total elapsed game time in milliseconds.")]
    public float GetTotalMilliseconds()
    {
        return (float)SquidVoxEngineContext.GameTime.TotalGameTime.TotalMilliseconds;
    }

    [ScriptFunction("delta")]
    public float GetDeltaMilliseconds()
    {
        return (float)SquidVoxEngineContext.GameTime.ElapsedGameTime.TotalMilliseconds;
    }


}
