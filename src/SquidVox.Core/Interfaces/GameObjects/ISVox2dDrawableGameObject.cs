namespace SquidVox.Core.Interfaces.GameObjects;

/// <summary>
/// Defines the contract for 2D drawable game objects in the SquidVox engine.
/// Combines updateable, renderable, and base object properties.
/// </summary>
public interface ISVox2dDrawableGameObject : ISVoxObject, ISVoxUpdateable, ISVox2dRenderable
{
}
