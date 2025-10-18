using System.Runtime.InteropServices;
using FontStashSharp.Interfaces;
using SquidVox.Core.Enums;
using SquidVox.Core.Interfaces.Rendering;
using TrippyGL;

namespace SquidVox.Core.Collections;

/// <summary>
/// High-performance collection for managing render layers.
/// Optimized for fast iteration (every frame) with minimal allocations.
/// </summary>
public class RenderLayerCollection
{
    private readonly List<IRenderableLayer> _layers;
    private readonly Dictionary<RenderLayer, IRenderableLayer> _layersByEnum;
    private bool _isDirty;

    /// <summary>
    /// Gets the number of layers in the collection.
    /// </summary>
    public int Count => _layers.Count;

    /// <summary>
    /// Initializes a new instance of the RenderLayerCollection class.
    /// </summary>
    public RenderLayerCollection()
    {
        _layers = new List<IRenderableLayer>(Enum.GetValues<RenderLayer>().Length); // Pre-allocate for common case
        _layersByEnum = new Dictionary<RenderLayer, IRenderableLayer>(Enum.GetValues<RenderLayer>().Length);
        _isDirty = false;
    }

    /// <summary>
    /// Initializes a new instance of the RenderLayerCollection class with a specified capacity.
    /// </summary>
    /// <param name="capacity">Initial capacity for the collection.</param>
    public RenderLayerCollection(int capacity)
    {
        _layers = new List<IRenderableLayer>(capacity);
        _layersByEnum = new Dictionary<RenderLayer, IRenderableLayer>(capacity);
        _isDirty = false;
    }

    /// <summary>
    /// Adds a render layer to the collection.
    /// </summary>
    /// <param name="layer">The layer to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when layer is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a layer with the same RenderLayer enum already exists.</exception>
    public void Add(IRenderableLayer layer)
    {
        ArgumentNullException.ThrowIfNull(layer);

        if (_layersByEnum.ContainsKey(layer.Layer))
        {
            throw new InvalidOperationException($"A render layer with priority {layer.Layer} already exists.");
        }

        _layers.Add(layer);
        _layersByEnum[layer.Layer] = layer;
        _isDirty = true;
    }

    /// <summary>
    /// Removes a render layer by its RenderLayer enum.
    /// </summary>
    /// <param name="layerEnum">The RenderLayer enum of the layer to remove.</param>
    /// <returns>True if the layer was removed, false if not found.</returns>
    public bool Remove(RenderLayer layerEnum)
    {
        if (_layersByEnum.TryGetValue(layerEnum, out var layer))
        {
            _layers.Remove(layer);
            _layersByEnum.Remove(layerEnum);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes a render layer from the collection.
    /// </summary>
    /// <param name="layer">The layer to remove.</param>
    /// <returns>True if the layer was removed, false if not found.</returns>
    public bool Remove(IRenderableLayer layer)
    {
        ArgumentNullException.ThrowIfNull(layer);

        if (_layers.Remove(layer))
        {
            _layersByEnum.Remove(layer.Layer);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets a render layer by its RenderLayer enum.
    /// </summary>
    /// <param name="layerEnum">The RenderLayer enum.</param>
    /// <returns>The layer if found, otherwise null.</returns>
    public IRenderableLayer? GetLayer(RenderLayer layerEnum)
    {
        return _layersByEnum.TryGetValue(layerEnum, out var layer) ? layer : null;
    }

    /// <summary>
    /// Checks if a layer with the specified RenderLayer enum exists.
    /// </summary>
    /// <param name="layerEnum">The RenderLayer enum to check.</param>
    /// <returns>True if the layer exists, false otherwise.</returns>
    public bool Contains(RenderLayer layerEnum)
    {
        return _layersByEnum.ContainsKey(layerEnum);
    }

    /// <summary>
    /// Tries to get a render layer by its RenderLayer enum.
    /// </summary>
    /// <param name="layerEnum">The RenderLayer enum.</param>
    /// <param name="layer">The layer if found, otherwise null.</param>
    /// <returns>True if the layer was found, false otherwise.</returns>
    public bool TryGetLayer(RenderLayer layerEnum, out IRenderableLayer? layer)
    {
        return _layersByEnum.TryGetValue(layerEnum, out layer);
    }

    /// <summary>
    /// Enables a render layer.
    /// </summary>
    /// <param name="layerEnum">The RenderLayer enum of the layer to enable.</param>
    /// <returns>True if the layer was found and enabled, false otherwise.</returns>
    public bool Enable(RenderLayer layerEnum)
    {
        if (_layersByEnum.TryGetValue(layerEnum, out var layer))
        {
            layer.Enabled = true;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Disables a render layer without removing it.
    /// </summary>
    /// <param name="layerEnum">The RenderLayer enum of the layer to disable.</param>
    /// <returns>True if the layer was found and disabled, false otherwise.</returns>
    public bool Disable(RenderLayer layerEnum)
    {
        if (_layersByEnum.TryGetValue(layerEnum, out var layer))
        {
            layer.Enabled = false;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Clears all layers from the collection.
    /// </summary>
    public void Clear()
    {
        _layers.Clear();
        _layersByEnum.Clear();
        _isDirty = false;
    }

    /// <summary>
    /// Ensures layers are sorted by their RenderLayer priority.
    /// Only sorts if the collection has been modified.
    /// </summary>
    private void EnsureSorted()
    {
        if (_isDirty)
        {
            _layers.Sort((a, b) => a.Layer.CompareTo(b.Layer));
            _isDirty = false;
        }
    }

    /// <summary>
    /// Renders all enabled layers in priority order.
    /// </summary>
    /// <param name="textureBatcher">TextureBatcher for rendering textures.</param>
    /// <param name="fontRenderer">Font renderer for drawing text.</param>
    public void RenderAll(TextureBatcher textureBatcher, IFontStashRenderer fontRenderer)
    {
        EnsureSorted();

        foreach (var layer in _layers)
        {
            if (layer.Enabled)
            {
                layer.Render(textureBatcher, fontRenderer);
            }
        }
    }

    /// <summary>
    /// Gets a read-only span of all layers sorted by priority.
    /// Use this for custom iteration scenarios.
    /// </summary>
    /// <returns>A read-only span of layers.</returns>
    public ReadOnlySpan<IRenderableLayer> GetLayersSpan()
    {
        EnsureSorted();
        return CollectionsMarshal.AsSpan(_layers);
    }

    /// <summary>
    /// Gets an enumerator for iterating over all layers.
    /// Layers are automatically sorted before enumeration.
    /// </summary>
    /// <returns>An enumerator.</returns>
    public List<IRenderableLayer>.Enumerator GetEnumerator()
    {
        EnsureSorted();
        return _layers.GetEnumerator();
    }

    /// <summary>
    /// Executes an action for each enabled layer in priority order.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    public void ForEachEnabled(Action<IRenderableLayer> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        EnsureSorted();

        foreach (var layer in _layers)
        {
            if (layer.Enabled)
            {
                action(layer);
            }
        }
    }

    /// <summary>
    /// Gets the number of enabled layers.
    /// </summary>
    /// <returns>The count of enabled layers.</returns>
    public int GetEnabledCount()
    {
        return _layers.Count(layer => layer.Enabled);
    }
}
