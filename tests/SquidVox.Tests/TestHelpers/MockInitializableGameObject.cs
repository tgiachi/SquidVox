using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SquidVox.Core.Interfaces.GameObjects;

namespace SquidVox.Tests.TestHelpers;

/// <summary>
/// Mock game object with initialization support.
/// </summary>
public class MockInitializableGameObject : MockGameObject, ISVoxInitializable
{
    /// <summary>
    ///
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    ///
    /// </summary>
    public void Initialize()
    {
        IsInitialized = true;
    }
}