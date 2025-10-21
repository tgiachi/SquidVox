using FontStashSharp.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SquidVox.Core.Enums;
using SquidVox.Core.Interfaces.GameObjects;

namespace SquidVox.Core.Interfaces.Rendering;

/// <summary>
/// Defines a renderable layer in the rendering pipeline.
/// Layers are rendered in order based on their RenderLayer priority.
/// </summary>
public interface IRenderableLayer : ISVoxInputReceiver
{
    /// <summary>
    /// Gets the rendering layer priority.
    /// </summary>
    RenderLayer Layer { get; }

    /// <summary>
    /// Gets or sets whether this layer is enabled for rendering.
    /// </summary>
    bool Enabled { get; set; }

    /// <summary>
    /// Renders the layer content.
    /// </summary>
    /// <param name="spriteBatch">SpriteBatch for rendering textures.</param>
    void Render(SpriteBatch spriteBatch);


    void Update(GameTime gameTime);
}
