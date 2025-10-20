using Microsoft.Xna.Framework;
using SquidVox.Core.GameObjects;
using SquidVox.GameObjects.UI.Types.Layout;

namespace SquidVox.GameObjects.UI.Components.UI.Layout;

public class StackPanelComponent
{
    public StackOrientation Orientation { get; set; } = StackOrientation.Vertical;
    public float Spacing { get; set; } = 5f;
    public bool IsAutoSize { get; set; } = true;

    public void ArrangeChildren(Base2dGameObject parent)
    {
        if (parent.Children == null || !parent.Children.Any()) return;

        float currentX = 0;
        float currentY = 0;
        float maxWidth = 0;
        float maxHeight = 0;

        foreach (var child in parent.Children)
        {
            if (child is Base2dGameObject go)
            {
                if (Orientation == StackOrientation.Horizontal)
                {
                    go.Position = new Vector2(currentX, currentY);
                    currentX += go.Size.X + Spacing;
                    maxHeight = Math.Max(maxHeight, go.Size.Y);
                }
                else
                {
                    go.Position = new Vector2(currentX, currentY);
                    currentY += go.Size.Y + Spacing;
                    maxWidth = Math.Max(maxWidth, go.Size.X);
                }
            }
        }

        if (IsAutoSize)
        {
            if (Orientation == StackOrientation.Horizontal)
            {
                parent.Size = new Vector2(currentX - Spacing, maxHeight);
            }
            else
            {
                parent.Size = new Vector2(maxWidth, currentY - Spacing);
            }
        }
    }
}