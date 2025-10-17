using FontStashSharp.Interfaces;
using TrippyGL;

namespace SquidVox.Core.Interfaces.GameObjects;

public interface ISVox2dRenderable
{
    void Render(TextureBatcher textureBatcher, IFontStashRenderer fontRenderer);
}
