using ImGuiNET;
using SquidVox.Core.Collections;
using SquidVox.Core.Interfaces.GameObjects;
using SquidVox.Core.Interfaces.Rendering;

namespace SquidVox.World3d.ImGUI;

/// <summary>
/// Debugger that displays a tree view of all game objects organized by render layers.
/// </summary>
public class GameObjectTreeDebugger : ISVoxDebuggerGameObject
{
    private readonly RenderLayerCollection _renderLayers;

    /// <summary>
    /// Initializes a new instance of the GameObjectTreeDebugger class.
    /// </summary>
    /// <param name="renderLayers">The render layer collection to debug.</param>
    public GameObjectTreeDebugger(RenderLayerCollection renderLayers)
    {
        _renderLayers = renderLayers;
        WindowTitle = "Game Object Tree";
        IsVisible = false;
    }

    /// <summary>
    /// Gets or sets whether the debugger window is visible.
    /// </summary>
    public bool IsVisible { get; set; }

    /// <summary>
    /// Gets the title of the debugger window.
    /// </summary>
    public string WindowTitle { get; }

    /// <summary>
    /// Draws the debugger UI.
    /// </summary>
    public void Draw()
    {
        foreach (var layer in _renderLayers)
        {
            var layerName = $"{layer.GetType().Name} ({layer.Layer})";
            var enabled = layer.Enabled ? "[Enabled]" : "[Disabled]";
            var nodeLabel = $"{layerName} {enabled}";

            if (ImGui.TreeNode(nodeLabel))
            {
                // Show layer properties
                ImGui.Text($"Type: {layer.GetType().FullName}");
                ImGui.Text($"Priority: {layer.Layer}");
                ImGui.Text($"Enabled: {layer.Enabled}");
                ImGui.Text($"Has Focus: {layer.HasFocus}");

                // Get all components in this layer
                var components = GetAllComponents(layer);
                if (components.Any())
                {
                    if (ImGui.TreeNode($"Components ({components.Count()})"))
                    {
                        foreach (var component in components)
                        {
                            var compName = component?.GetType().Name ?? "null";
                            var compFullName = component?.GetType().FullName ?? "null";

                            if (ImGui.TreeNode(compName))
                            {
                                ImGui.Text($"Type: {compFullName}");

                                // Try to show Name if it's a game object
                                if (component is ISVoxObject gameObject)
                                {
                                    ImGui.Text($"Name: {gameObject.Name}");
                                    ImGui.Text($"Id: {gameObject.Id}");
                                    ImGui.Text($"Is Enabled: {gameObject.IsEnabled}");
                                    ImGui.Text($"Is Visible: {gameObject.IsVisible}");
                                    ImGui.Text($"Z-Index: {gameObject.ZIndex}");
                                }

                                if (component is ISVoxInputReceiver inputReceiver)
                                {
                                    ImGui.Text($"Has Focus: {inputReceiver.HasFocus}");
                                }

                                ImGui.TreePop();
                            }
                        }
                        ImGui.TreePop();
                    }
                }
                else
                {
                    ImGui.Text("No components");
                }

                ImGui.TreePop();
            }
        }
    }

    private static IEnumerable<object?> GetAllComponents(IRenderableLayer layer)
    {
        // Use reflection or specific methods to get all components
        if (layer is Rendering.GameObject2dRenderLayer gameObject2dLayer)
        {
            return gameObject2dLayer.GetAllComponents().Cast<object>();
        }
        else if (layer is Rendering.GameObject3dRenderLayer gameObject3dLayer)
        {
            return gameObject3dLayer.GetAllComponents().Cast<object>();
        }
        else if (layer is Rendering.SceneRenderLayer sceneLayer)
        {
            return sceneLayer.GetAllComponents();
        }
        else if (layer is Rendering.ImGuiRenderLayer imGuiLayer)
        {
            return imGuiLayer.GetAllComponents().Cast<object>();
        }
        else
        {
            // Fallback: try GetComponent for common types or return empty
            return [];
        }
    }
}