using Microsoft.Xna.Framework;

namespace SquidVox.Core.Interfaces.GameObjects;

/// <summary>
/// Defines the contract for updateable objects in the SquidVox engine.
/// </summary>
public interface ISVoxUpdateable
{
    /// <summary>
    /// Updates the object with the given game time.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    void Update(GameTime gameTime);
}
