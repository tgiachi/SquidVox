namespace SquidVox.Core.Interfaces.GameObjects;

/// <summary>
/// Combined interface for 3D drawable game objects.
/// Represents an object that can be updated and rendered in 3D space.
/// </summary>
public interface ISVox3dDrawableGameObject : ISVoxObject, ISVoxUpdateable, ISVox3dRenderable
{
}
