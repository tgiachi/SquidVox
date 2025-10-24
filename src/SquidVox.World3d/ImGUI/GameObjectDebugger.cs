using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ImGuiNET;
using Microsoft.Xna.Framework;
using SquidVox.Core.Attributes.Debugger;
using SquidVox.Core.Collections;
using SquidVox.Core.Interfaces.GameObjects;
using SquidVox.Core.Interfaces.Rendering;

namespace SquidVox.World3d.ImGUI;

/// <summary>
/// Generic debugger that uses reflection to display and edit properties/fields marked with debugger attributes.
/// Can also browse and select GameObjects from RenderLayerCollection.
/// </summary>
public class GameObjectDebugger : ISVoxDebuggerGameObject
{
    private object? _selectedGameObject;
    private readonly RenderLayerCollection? _renderLayers;

    /// <summary>
    /// Initializes a new instance of the GameObjectDebugger class.
    /// </summary>
    public GameObjectDebugger()
    {
        WindowTitle = "Object Debugger";
        IsVisible = false;
    }

    /// <summary>
    /// Initializes a new instance of the GameObjectDebugger class with RenderLayerCollection support.
    /// </summary>
    /// <param name="renderLayers">The render layer collection to browse.</param>
    public GameObjectDebugger(RenderLayerCollection renderLayers)
    {
        _renderLayers = renderLayers;
        WindowTitle = "Object Debugger";
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
    /// Gets or sets the object to debug.
    /// </summary>
    public object? SelectedGameObject
    {
        get => _selectedGameObject;
        set => _selectedGameObject = value;
    }

    /// <summary>
    /// Draws the debugger UI.
    /// </summary>
    public void Draw()
    {
        // If RenderLayers is provided, show split view (tree + properties)
        if (_renderLayers != null)
        {
            DrawSplitView();
        }
        else
        {
            // Single panel mode - just show properties
            DrawPropertiesPanel();
        }
    }

    /// <summary>
    /// Draws the split view with GameObject tree on the left and properties on the right.
    /// </summary>
    private void DrawSplitView()
    {
        // Left panel - GameObject tree
        if (ImGui.BeginChild("GameObjectTree", new System.Numerics.Vector2(300, 0), ImGuiChildFlags.Borders))
        {
            ImGui.Text("GameObjects");
            ImGui.Separator();

            if (_renderLayers != null)
            {
                foreach (var layer in _renderLayers)
                {
                    DrawLayerTree(layer);
                }
            }
        }
        ImGui.EndChild();

        ImGui.SameLine();

        // Right panel - Properties editor
        if (ImGui.BeginChild("Properties", new System.Numerics.Vector2(0, 0), ImGuiChildFlags.Borders))
        {
            DrawPropertiesPanel();
        }
        ImGui.EndChild();
    }

    /// <summary>
    /// Draws the properties panel for the selected object.
    /// </summary>
    private void DrawPropertiesPanel()
    {
        if (_selectedGameObject == null)
        {
            ImGui.Text("No object selected");
            ImGui.Text("Select a GameObject from the tree or set SelectedGameObject");

            return;
        }

        var objectType = _selectedGameObject.GetType();
        ImGui.Text($"Type: {objectType.Name}");
        ImGui.Text($"Full Name: {objectType.FullName}");
        ImGui.Separator();

        DrawCustomProperties();
    }

    /// <summary>
    /// Draws a render layer and its components as a tree.
    /// </summary>
    private void DrawLayerTree(IRenderableLayer layer)
    {
        var layerName = $"{layer.GetType().Name} ({layer.Layer})";
        var enabled = layer.Enabled ? "[Enabled]" : "[Disabled]";
        var nodeLabel = $"{layerName} {enabled}";

        if (ImGui.TreeNode(nodeLabel))
        {
            // Get all components in this layer
            var components = GetAllComponents(layer);

            if (components.Any())
            {
                foreach (var component in components)
                {
                    if (component != null)
                    {
                        DrawGameObjectNode(component);
                    }
                }
            }
            else
            {
                ImGui.Text("No components");
            }

            ImGui.TreePop();
        }
    }

    /// <summary>
    /// Draws a GameObject node in the tree.
    /// </summary>
    private void DrawGameObjectNode(object gameObject)
    {
        var objectName = gameObject.GetType().Name;

        // If it's an ISVoxObject, show its name
        if (gameObject is ISVoxObject svoxObject)
        {
            objectName = $"{svoxObject.Name} ({gameObject.GetType().Name})";
        }

        var flags = ImGuiTreeNodeFlags.Leaf;

        // Add selection flags if this is the selected object
        if (_selectedGameObject == gameObject)
        {
            flags |= ImGuiTreeNodeFlags.Selected;
        }

        var isOpen = ImGui.TreeNodeEx($"{objectName}##{gameObject.GetHashCode()}", flags);

        // Handle selection on click
        if (ImGui.IsItemClicked())
        {
            _selectedGameObject = gameObject;
        }

        // Show tooltip on hover
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text($"Type: {gameObject.GetType().Name}");

            if (gameObject is ISVoxObject obj)
            {
                ImGui.Text($"Name: {obj.Name}");
                ImGui.Text($"Enabled: {obj.IsEnabled}");
                ImGui.Text($"Visible: {obj.IsVisible}");
            }

            ImGui.EndTooltip();
        }

        if (isOpen)
        {
            ImGui.TreePop();
        }
    }

    /// <summary>
    /// Gets all components from a render layer.
    /// </summary>
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
            // Fallback: return empty
            return [];
        }
    }

    /// <summary>
    /// Draws custom properties using reflection and debugger attributes.
    /// </summary>
    private void DrawCustomProperties()
    {
        if (_selectedGameObject == null)
        {
            return;
        }

        var objectType = _selectedGameObject.GetType();

        // Get writable properties (can be modified)
        var writableProperties = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                           .Where(p => p.CanRead && p.CanWrite)
                                           .ToArray();

        // Get readonly properties (for display only)
        var readonlyProperties = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                           .Where(p => p.CanRead && !p.CanWrite)
                                           .ToArray();

        var fields = objectType.GetFields(BindingFlags.Public | BindingFlags.Instance);

        // Check if there are any custom properties to display
        var hasCustomProperties = writableProperties.Any(HasDebuggerAttribute) ||
                                  readonlyProperties.Any(HasDebuggerAttribute) ||
                                  fields.Any(HasDebuggerAttribute);

        if (!hasCustomProperties)
        {
            ImGui.Text("No debugger fields found");
            ImGui.TextWrapped("Mark fields/properties with [DebuggerField] to display them here");

            return;
        }

        // Draw header if present
        var headerAttr = objectType.GetCustomAttribute<DebuggerHeaderAttribute>();

        if (headerAttr != null)
        {
            ImGui.Text(headerAttr.Header);
        }
        else
        {
            ImGui.Text("Properties");
        }

        ImGui.Separator();

        // Draw writable properties with debugger attributes
        foreach (var property in writableProperties)
        {
            if (HasDebuggerAttribute(property))
            {
                DrawProperty(property, _selectedGameObject);
            }
        }

        // Draw fields with debugger attributes
        foreach (var field in fields)
        {
            if (HasDebuggerAttribute(field))
            {
                DrawField(field, _selectedGameObject);
            }
        }

        // Draw readonly properties (display only) if any
        var readonlyWithAttributes = readonlyProperties.Where(HasDebuggerAttribute).ToArray();
        if (readonlyWithAttributes.Length > 0)
        {
            ImGui.Separator();
            ImGui.Text("Read-only Info");

            foreach (var property in readonlyWithAttributes)
            {
                DrawReadOnlyProperty(property, _selectedGameObject);
            }
        }
    }

    /// <summary>
    /// Checks if a member has any debugger attribute.
    /// </summary>
    private static bool HasDebuggerAttribute(MemberInfo member)
    {
        return member.GetCustomAttribute<DebuggerFieldAttribute>() != null ||
               member.GetCustomAttribute<DebuggerRangeAttribute>() != null;
    }

    /// <summary>
    /// Draws a property with appropriate UI control based on its type and attributes.
    /// </summary>
    private static void DrawProperty(PropertyInfo property, object instance)
    {
        try
        {
            var value = property.GetValue(instance);
            var newValue = DrawPropertyControl(property.Name, property.PropertyType, value, property);

            if (newValue != null && !newValue.Equals(value))
            {
                property.SetValue(instance, newValue);
            }
        }
        catch (Exception ex)
        {
            ImGui.Text($"{property.Name}: Error - {ex.Message}");
        }
    }

    /// <summary>
    /// Draws a field with appropriate UI control based on its type and attributes.
    /// </summary>
    private static void DrawField(FieldInfo field, object instance)
    {
        try
        {
            var value = field.GetValue(instance);
            var newValue = DrawPropertyControl(field.Name, field.FieldType, value, field);

            if (newValue != null && !newValue.Equals(value))
            {
                field.SetValue(instance, newValue);
            }
        }
        catch (Exception ex)
        {
            ImGui.Text($"{field.Name}: Error - {ex.Message}");
        }
    }

    /// <summary>
    /// Draws a readonly property (display only).
    /// </summary>
    private static void DrawReadOnlyProperty(PropertyInfo property, object instance)
    {
        try
        {
            var value = property.GetValue(instance);
            DrawReadOnlyValue(property.Name, property.PropertyType, value);
        }
        catch (Exception ex)
        {
            ImGui.Text($"{property.Name}: Error - {ex.Message}");
        }
    }

    /// <summary>
    /// Draws a readonly value (display only, no editing).
    /// </summary>
    private static void DrawReadOnlyValue(string name, Type type, object? value)
    {
        if (type == typeof(float))
        {
            var floatValue = (float)(value ?? 0f);
            ImGui.Text($"{name}: {floatValue:F3}");
        }
        else if (type == typeof(int))
        {
            var intValue = (int)(value ?? 0);
            ImGui.Text($"{name}: {intValue}");
        }
        else if (type == typeof(double))
        {
            var doubleValue = (double)(value ?? 0.0);
            ImGui.Text($"{name}: {doubleValue:F3}");
        }
        else if (type == typeof(bool))
        {
            var boolValue = (bool)(value ?? false);
            ImGui.Text($"{name}: {boolValue}");
        }
        else if (type == typeof(string))
        {
            var stringValue = (string)(value ?? "");
            ImGui.Text($"{name}: {stringValue}");
        }
        else if (type == typeof(Vector2))
        {
            var xnaVector = (Vector2)(value ?? Vector2.Zero);
            ImGui.Text($"{name}: ({xnaVector.X:F2}, {xnaVector.Y:F2})");
        }
        else if (type == typeof(Vector3))
        {
            var xnaVector = (Vector3)(value ?? Vector3.Zero);
            // If it's a color, show color swatch
            if (name.Contains("Color", StringComparison.OrdinalIgnoreCase))
            {
                var vector = new System.Numerics.Vector3(xnaVector.X, xnaVector.Y, xnaVector.Z);
                ImGui.ColorButton($"##{name}_color", new System.Numerics.Vector4(vector.X, vector.Y, vector.Z, 1f),
                    ImGuiColorEditFlags.NoAlpha | ImGuiColorEditFlags.NoPicker, new System.Numerics.Vector2(20, 20));
                ImGui.SameLine();
            }
            ImGui.Text($"{name}: ({xnaVector.X:F2}, {xnaVector.Y:F2}, {xnaVector.Z:F2})");
        }
        else if (type == typeof(Color))
        {
            var xnaColor = (Color)(value ?? Color.White);
            var vector = new System.Numerics.Vector4(
                xnaColor.R / 255f,
                xnaColor.G / 255f,
                xnaColor.B / 255f,
                xnaColor.A / 255f
            );
            ImGui.ColorButton($"##{name}_color", vector,
                ImGuiColorEditFlags.NoAlpha | ImGuiColorEditFlags.NoPicker, new System.Numerics.Vector2(20, 20));
            ImGui.SameLine();
            ImGui.Text($"{name}: ({xnaColor.R}, {xnaColor.G}, {xnaColor.B}, {xnaColor.A})");
        }
        else if (type.IsEnum)
        {
            var enumValue = value ?? Activator.CreateInstance(type);
            ImGui.Text($"{name}: {enumValue}");
        }
        else
        {
            ImGui.Text($"{name}: {value?.ToString() ?? "null"}");
        }
    }

    /// <summary>
    /// Draws the appropriate UI control for a property based on its type and attributes.
    /// </summary>
    private static object? DrawPropertyControl(string name, Type type, object? value, MemberInfo member)
    {
        var rangeAttr = member.GetCustomAttribute<DebuggerRangeAttribute>();

        // Handle different types
        if (type == typeof(float))
        {
            var floatValue = (float)(value ?? 0f);

            if (rangeAttr != null)
            {
                if (ImGui.SliderFloat(name, ref floatValue, (float)rangeAttr.Min, (float)rangeAttr.Max))
                {
                    return floatValue;
                }
            }
            else
            {
                if (ImGui.DragFloat(name, ref floatValue, 0.1f))
                {
                    return floatValue;
                }
            }
        }
        else if (type == typeof(int))
        {
            var intValue = (int)(value ?? 0);

            if (rangeAttr != null)
            {
                if (ImGui.SliderInt(name, ref intValue, (int)rangeAttr.Min, (int)rangeAttr.Max))
                {
                    return intValue;
                }
            }
            else
            {
                if (ImGui.DragInt(name, ref intValue))
                {
                    return intValue;
                }
            }
        }
        else if (type == typeof(double))
        {
            var doubleValue = (double)(value ?? 0.0);
            var floatValue = (float)doubleValue;

            if (rangeAttr != null)
            {
                if (ImGui.SliderFloat(name, ref floatValue, (float)rangeAttr.Min, (float)rangeAttr.Max))
                {
                    return (double)floatValue;
                }
            }
            else
            {
                if (ImGui.DragFloat(name, ref floatValue, 0.1f))
                {
                    return (double)floatValue;
                }
            }
        }
        else if (type == typeof(bool))
        {
            var boolValue = (bool)(value ?? false);

            if (ImGui.Checkbox(name, ref boolValue))
            {
                return boolValue;
            }
        }
        else if (type == typeof(string))
        {
            var stringValue = (string)(value ?? "");
            var buffer = stringValue;

            if (ImGui.InputText(name, ref buffer, 256))
            {
                return buffer;
            }
        }
        else if (type == typeof(Vector2))
        {
            var xnaVector = (Vector2)(value ?? Vector2.Zero);
            var vector = new System.Numerics.Vector2(xnaVector.X, xnaVector.Y);

            if (ImGui.DragFloat2(name, ref vector, 0.1f))
            {
                return new Vector2(vector.X, vector.Y);
            }
        }
        else if (type == typeof(Vector3))
        {
            var xnaVector = (Vector3)(value ?? Vector3.Zero);
            var vector = new System.Numerics.Vector3(xnaVector.X, xnaVector.Y, xnaVector.Z);

            // If the property name contains "Color", show it as a color picker
            if (name.Contains("Color", StringComparison.OrdinalIgnoreCase))
            {
                if (ImGui.ColorEdit3(name, ref vector))
                {
                    return new Vector3(vector.X, vector.Y, vector.Z);
                }
            }
            else
            {
                if (ImGui.DragFloat3(name, ref vector, 0.1f))
                {
                    return new Vector3(vector.X, vector.Y, vector.Z);
                }
            }
        }
        else if (type == typeof(Color))
        {
            var xnaColor = (Color)(value ?? Color.White);
            var color = new System.Numerics.Vector4(
                xnaColor.R / 255f,
                xnaColor.G / 255f,
                xnaColor.B / 255f,
                xnaColor.A / 255f
            );

            if (ImGui.ColorEdit4(name, ref color))
            {
                return new Color(
                    (byte)(color.X * 255f),
                    (byte)(color.Y * 255f),
                    (byte)(color.Z * 255f),
                    (byte)(color.W * 255f)
                );
            }
        }
        else if (type.IsEnum)
        {
            var enumValue = value ?? Activator.CreateInstance(type);
            var enumNames = Enum.GetNames(type);
            var currentIndex = Array.IndexOf(enumNames, enumValue?.ToString());

            if (ImGui.Combo(name, ref currentIndex, enumNames, enumNames.Length))
            {
                return Enum.Parse(type, enumNames[currentIndex]);
            }
        }
        else
        {
            // For unsupported types, just show the value as text
            ImGui.Text($"{name}: {value?.ToString() ?? "null"} ({type.Name})");
        }

        return null;
    }
}
