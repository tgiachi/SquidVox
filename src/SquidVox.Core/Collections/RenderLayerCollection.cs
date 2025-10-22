using System.Runtime.InteropServices;
using FontStashSharp.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SquidVox.Core.Enums;
using SquidVox.Core.Interfaces.GameObjects;
using SquidVox.Core.Interfaces.Rendering;

namespace SquidVox.Core.Collections;

/// <summary>
/// High-performance collection for managing render layers.
/// Optimized for fast iteration (every frame) with minimal allocations.
/// Supports multiple layers with the same priority.
/// </summary>
public class RenderLayerCollection
{
    private readonly List<IRenderableLayer> _layers;
    private readonly Dictionary<RenderLayer, List<IRenderableLayer>> _layersByEnum;
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
        _layersByEnum = new Dictionary<RenderLayer, List<IRenderableLayer>>(Enum.GetValues<RenderLayer>().Length);
        _isDirty = false;
    }

    /// <summary>
    /// Initializes a new instance of the RenderLayerCollection class with a specified capacity.
    /// </summary>
    /// <param name="capacity">Initial capacity for the collection.</param>
    public RenderLayerCollection(int capacity)
    {
        _layers = new List<IRenderableLayer>(capacity);
        _layersByEnum = new Dictionary<RenderLayer, List<IRenderableLayer>>(capacity);
        _isDirty = false;
    }

    /// <summary>
    /// Adds a render layer to the collection.
    /// Multiple layers with the same priority are allowed.
    /// </summary>
    /// <param name="layer">The layer to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when layer is null.</exception>
    public void Add(IRenderableLayer layer)
    {
        ArgumentNullException.ThrowIfNull(layer);

        _layers.Add(layer);

        if (!_layersByEnum.TryGetValue(layer.Layer, out var layerList))
        {
            layerList = new List<IRenderableLayer>();
            _layersByEnum[layer.Layer] = layerList;
        }

        layerList.Add(layer);
        _isDirty = true;
    }

    /// <summary>
    /// Removes all render layers with the specified RenderLayer enum.
    /// </summary>
    /// <param name="layerEnum">The RenderLayer enum of the layers to remove.</param>
    /// <returns>True if at least one layer was removed, false if not found.</returns>
    public bool Remove(RenderLayer layerEnum)
    {
        if (_layersByEnum.TryGetValue(layerEnum, out var layerList))
        {
            foreach (var layer in layerList)
            {
                _layers.Remove(layer);
            }
            _layersByEnum.Remove(layerEnum);
            _isDirty = true;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes a specific render layer from the collection.
    /// </summary>
    /// <param name="layer">The layer to remove.</param>
    /// <returns>True if the layer was removed, false if not found.</returns>
    public bool Remove(IRenderableLayer layer)
    {
        ArgumentNullException.ThrowIfNull(layer);

        if (_layers.Remove(layer))
        {
            if (_layersByEnum.TryGetValue(layer.Layer, out var layerList))
            {
                layerList.Remove(layer);
                if (layerList.Count == 0)
                {
                    _layersByEnum.Remove(layer.Layer);
                }
            }
            _isDirty = true;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the first render layer with the specified RenderLayer enum.
    /// </summary>
    /// <param name="layerEnum">The RenderLayer enum.</param>
    /// <returns>The first layer if found, otherwise null.</returns>
    public IRenderableLayer? GetLayer(RenderLayer layerEnum)
    {
        if (_layersByEnum.TryGetValue(layerEnum, out var layerList) && layerList.Count > 0)
        {
            return layerList[0];
        }
        return null;
    }

    /// <summary>
    /// Gets all render layers with the specified RenderLayer enum.
    /// </summary>
    /// <param name="layerEnum">The RenderLayer enum.</param>
    /// <returns>List of layers with the specified priority, or empty list if none found.</returns>
    public IReadOnlyList<IRenderableLayer> GetLayers(RenderLayer layerEnum)
    {
        if (_layersByEnum.TryGetValue(layerEnum, out var layerList))
        {
            return layerList.AsReadOnly();
        }
        return [];
    }

    /// <summary>
    /// Gets the first render layer of the specified type.
    /// </summary>
    /// <typeparam name="TLayer">The type of the layer to get.</typeparam>
    /// <returns>The first layer of the specified type if found, otherwise null.</returns>
    public TLayer? GetLayer<TLayer>() where TLayer : IRenderableLayer
    {
        foreach (var layer in _layers)
        {
            if (layer is TLayer tLayer)
            {
                return tLayer;
            }
        }
        return default;
    }

    /// <summary>
    /// Gets the first component of the specified type from all layers.
    /// </summary>
    /// <typeparam name="T">The type of the component to get.</typeparam>
    /// <returns>The first component of the specified type if found, otherwise null.</returns>
    public T? GetComponent<T>() where T : class
    {
        foreach (var layer in _layers)
        {
            if (!layer.Enabled)
            {
                continue;
            }

            var component = layer.GetComponent<T>();
            if (component != null)
            {
                return component;
            }
        }
        return null;
    }

    /// <summary>
    /// Checks if at least one layer with the specified RenderLayer enum exists.
    /// </summary>
    /// <param name="layerEnum">The RenderLayer enum to check.</param>
    /// <returns>True if at least one layer exists, false otherwise.</returns>
    public bool Contains(RenderLayer layerEnum)
    {
        return _layersByEnum.ContainsKey(layerEnum) && _layersByEnum[layerEnum].Count > 0;
    }

    /// <summary>
    /// Tries to get the first render layer with the specified RenderLayer enum.
    /// </summary>
    /// <param name="layerEnum">The RenderLayer enum.</param>
    /// <param name="layer">The first layer if found, otherwise null.</param>
    /// <returns>True if at least one layer was found, false otherwise.</returns>
    public bool TryGetLayer(RenderLayer layerEnum, out IRenderableLayer? layer)
    {
        if (_layersByEnum.TryGetValue(layerEnum, out var layerList) && layerList.Count > 0)
        {
            layer = layerList[0];
            return true;
        }
        layer = null;
        return false;
    }

    /// <summary>
    /// Enables all render layers with the specified RenderLayer enum.
    /// </summary>
    /// <param name="layerEnum">The RenderLayer enum of the layers to enable.</param>
    /// <returns>True if at least one layer was found and enabled, false otherwise.</returns>
    public bool Enable(RenderLayer layerEnum)
    {
        if (_layersByEnum.TryGetValue(layerEnum, out var layerList))
        {
            foreach (var layer in layerList)
            {
                layer.Enabled = true;
            }
            return true;
        }

        return false;
    }

    /// <summary>
    /// Disables all render layers with the specified RenderLayer enum without removing them.
    /// </summary>
    /// <param name="layerEnum">The RenderLayer enum of the layers to disable.</param>
    /// <returns>True if at least one layer was found and disabled, false otherwise.</returns>
    public bool Disable(RenderLayer layerEnum)
    {
        if (_layersByEnum.TryGetValue(layerEnum, out var layerList))
        {
            foreach (var layer in layerList)
            {
                layer.Enabled = false;
            }
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
    /// Updates all enabled layers in priority order.
    /// </summary>
    /// <param name="gameTime">Game timing information.</param>
    public void UpdateAll(GameTime gameTime)
    {
        EnsureSorted();

        foreach (var layer in _layers)
        {
            if (layer.Enabled)
            {
                layer.Update(gameTime);
            }
        }
    }

    /// <summary>
    /// Renders all enabled layers in priority order.
    /// </summary>
    /// <param name="spriteBatcher">SpriteBatcher for rendering textures.</param>
    public void RenderAll(SpriteBatch spriteBatcher)
    {
        EnsureSorted();

        foreach (var layer in _layers)
        {
            if (layer.Enabled)
            {
                layer.Render(spriteBatcher);
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

    /// <summary>
    /// Handles keyboard input for all enabled layers.
    /// </summary>
    /// <param name="keyboardState">The current keyboard state.</param>
    /// <param name="gameTime">Game timing information.</param>
    public void HandleKeyboardAll(KeyboardState keyboardState, GameTime gameTime)
    {
        EnsureSorted();

        foreach (var layer in _layers)
        {
            if (layer.Enabled)
            {
                layer.HandleKeyboard(keyboardState, gameTime);
            }
        }
    }

    /// <summary>
    /// Handles mouse input for all enabled layers.
    /// </summary>
    /// <param name="mouseState">The current mouse state.</param>
    /// <param name="gameTime">Game timing information.</param>
    public void HandleMouseAll(MouseState mouseState, GameTime gameTime)
    {
        EnsureSorted();

        foreach (var layer in _layers)
        {
            if (layer.Enabled)
            {
                layer.HandleMouse(mouseState, gameTime);
            }
        }
    }
}
