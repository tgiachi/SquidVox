using System.Collections;
using SquidVox.Core.Interfaces.GameObjects;

namespace SquidVox.Core.Collections;

/// <summary>
/// High-performance sorted collection for ISVox2dDrawableGameObject objects, automatically sorted by ZIndex.
/// </summary>
/// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
public class SvoxGameObjectCollection<T> : IEnumerable<T>
    where T : class, ISVox2dDrawableGameObject
{
    private readonly List<T> _gameObjects;
    private readonly Dictionary<Type, List<T>> _gameObjectsByType;
    private readonly Dictionary<T, int> _gameObjectToIndex;
    private readonly Dictionary<T, int> _lastKnownZIndex;
    private bool _isDirty;
    private bool _isTypeCacheDirty;

    public SvoxGameObjectCollection()
    {
        _gameObjects = [];
        _gameObjectToIndex = new Dictionary<T, int>();
        _lastKnownZIndex = new Dictionary<T, int>();
        _gameObjectsByType = new Dictionary<Type, List<T>>();
        _isDirty = false;
        _isTypeCacheDirty = false;
    }

    public SvoxGameObjectCollection(int capacity)
    {
        _gameObjects = new List<T>(capacity);
        _gameObjectToIndex = new Dictionary<T, int>(capacity);
        _lastKnownZIndex = new Dictionary<T, int>(capacity);
        _gameObjectsByType = new Dictionary<Type, List<T>>();
        _isDirty = false;
        _isTypeCacheDirty = false;
    }

    /// <summary>
    /// Number of game objects in the collection.
    /// </summary>
    public int Count => _gameObjects.Count;

    /// <summary>
    /// Gets game object at specified index (after sorting).
    /// </summary>
    /// <param name="index">Index of the game object.</param>
    /// <returns>Game object at the specified index.</returns>
    public T this[int index]
    {
        get
        {
            EnsureSorted();
            return _gameObjects[index];
        }
    }

    /// <summary>
    /// Gets enumerator for the collection (sorted by ZIndex).
    /// </summary>
    /// <returns>Enumerator for the collection.</returns>
    public IEnumerator<T> GetEnumerator()
    {
        EnsureSorted();
        return _gameObjects.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Adds a game object to the collection.
    /// </summary>
    /// <param name="gameObject">Game object to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when game object is null.</exception>
    /// <exception cref="ArgumentException">Thrown when game object already exists in collection.</exception>
    public void Add(T gameObject)
    {
        ArgumentNullException.ThrowIfNull(gameObject);

        if (_gameObjectToIndex.ContainsKey(gameObject))
        {
            throw new ArgumentException("Game object already exists in collection", nameof(gameObject));
        }

        _gameObjects.Add(gameObject);
        _gameObjectToIndex[gameObject] = _gameObjects.Count - 1;
        _lastKnownZIndex[gameObject] = gameObject.ZIndex;
        _isDirty = true;
        _isTypeCacheDirty = true;
    }

    /// <summary>
    /// Removes a game object from the collection.
    /// </summary>
    /// <param name="gameObject">Game object to remove.</param>
    /// <returns>True if game object was removed, false if not found.</returns>
    public bool Remove(T gameObject)
    {
        if (gameObject == null || !_gameObjectToIndex.ContainsKey(gameObject))
        {
            return false;
        }

        _gameObjects.Remove(gameObject);
        _gameObjectToIndex.Remove(gameObject);
        _lastKnownZIndex.Remove(gameObject);
        _isDirty = true;
        _isTypeCacheDirty = true;
        RebuildIndexMap();

        return true;
    }

    /// <summary>
    /// Removes game object at specified index.
    /// </summary>
    /// <param name="index">Index of game object to remove.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index is out of range.</exception>
    public void RemoveAt(int index)
    {
        EnsureSorted();

        if (index < 0 || index >= _gameObjects.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var gameObject = _gameObjects[index];

        _gameObjects.RemoveAt(index);
        _gameObjectToIndex.Remove(gameObject);
        _lastKnownZIndex.Remove(gameObject);
        _isTypeCacheDirty = true;
        RebuildIndexMap();
    }

    /// <summary>
    /// Checks if collection contains the specified game object.
    /// </summary>
    /// <param name="gameObject">Game object to check.</param>
    /// <returns>True if game object exists in collection.</returns>
    public bool Contains(T gameObject)
    {
        return gameObject != null && _gameObjectToIndex.ContainsKey(gameObject);
    }

    /// <summary>
    /// Checks if collection contains any game object of the specified type (with caching for performance).
    /// </summary>
    /// <typeparam name="TGameObject">Type of game object to check for.</typeparam>
    /// <returns>True if any game object of the specified type exists in collection.</returns>
    public bool Contains<TGameObject>()
        where TGameObject : class, T
    {
        EnsureTypeCacheUpdated();
        return _gameObjectsByType.ContainsKey(typeof(TGameObject))
               && _gameObjectsByType[typeof(TGameObject)].Count > 0;
    }

    /// <summary>
    /// Clears all game objects from the collection.
    /// </summary>
    public void Clear()
    {
        _gameObjects.Clear();
        _gameObjectToIndex.Clear();
        _lastKnownZIndex.Clear();
        _gameObjectsByType.Clear();
        _isDirty = false;
        _isTypeCacheDirty = false;
    }

    /// <summary>
    /// Gets game objects within a specific ZIndex range.
    /// </summary>
    /// <param name="minZIndex">Minimum ZIndex (inclusive).</param>
    /// <param name="maxZIndex">Maximum ZIndex (inclusive).</param>
    /// <returns>Enumerable of game objects within the specified range.</returns>
    public IEnumerable<T> GetGameObjectsInZRange(int minZIndex, int maxZIndex)
    {
        EnsureSorted();

        foreach (var gameObject in _gameObjects)
        {
            if (gameObject.ZIndex >= minZIndex && gameObject.ZIndex <= maxZIndex)
            {
                yield return gameObject;
            }
        }
    }

    /// <summary>
    /// Gets all enabled game objects.
    /// </summary>
    /// <returns>Enumerable of enabled game objects.</returns>
    public IEnumerable<T> GetEnabledGameObjects()
    {
        EnsureSorted();

        foreach (var gameObject in _gameObjects)
        {
            if (gameObject.IsEnabled)
            {
                yield return gameObject;
            }
        }
    }

    /// <summary>
    /// Gets all visible game objects.
    /// </summary>
    /// <returns>Enumerable of visible game objects.</returns>
    public IEnumerable<T> GetVisibleGameObjects()
    {
        EnsureSorted();

        foreach (var gameObject in _gameObjects)
        {
            if (gameObject.IsVisible)
            {
                yield return gameObject;
            }
        }
    }

    /// <summary>
    /// Gets all game objects that are both enabled and visible.
    /// </summary>
    /// <returns>Enumerable of enabled and visible game objects.</returns>
    public IEnumerable<T> GetActiveGameObjects()
    {
        EnsureSorted();

        foreach (var gameObject in _gameObjects)
        {
            if (gameObject.IsEnabled && gameObject.IsVisible)
            {
                yield return gameObject;
            }
        }
    }

    /// <summary>
    /// Gets game objects of a specific type (with caching for performance).
    /// </summary>
    /// <typeparam name="TGameObject">Type of game object to retrieve.</typeparam>
    /// <returns>Enumerable of game objects of the specified type.</returns>
    public IEnumerable<TGameObject> GetGameObjectsOfType<TGameObject>()
        where TGameObject : class, T
    {
        EnsureTypeCacheUpdated();

        if (_gameObjectsByType.TryGetValue(typeof(TGameObject), out var gameObjects))
        {
            foreach (var gameObject in gameObjects)
            {
                if (gameObject is TGameObject typedGameObject)
                {
                    yield return typedGameObject;
                }
            }
        }
    }

    /// <summary>
    /// Gets the first game object of a specific type, or null if not found.
    /// </summary>
    /// <typeparam name="TGameObject">Type of game object to retrieve.</typeparam>
    /// <returns>First game object of the specified type, or null.</returns>
    public TGameObject? GetFirstGameObjectOfType<TGameObject>()
        where TGameObject : class, T
    {
        EnsureTypeCacheUpdated();

        if (_gameObjectsByType.TryGetValue(typeof(TGameObject), out var gameObjects) && gameObjects.Count > 0)
        {
            return gameObjects[0] as TGameObject;
        }

        return null;
    }

    /// <summary>
    /// Gets a game object by name.
    /// </summary>
    /// <param name="name">Name of the game object to find.</param>
    /// <returns>First game object with the specified name, or null if not found.</returns>
    public T? GetGameObjectByName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        foreach (var gameObject in _gameObjects)
        {
            if (gameObject.Name == name)
            {
                return gameObject;
            }
        }

        return null;
    }

    /// <summary>
    /// Forces a resort of the collection.
    /// </summary>
    public void ForceSort()
    {
        _isDirty = true;
        EnsureSorted();
    }

    /// <summary>
    /// Converts collection to array (sorted by ZIndex).
    /// </summary>
    /// <returns>Array of game objects sorted by ZIndex.</returns>
    public T[] ToArray()
    {
        EnsureSorted();
        return _gameObjects.ToArray();
    }

    /// <summary>
    /// Checks if any game object's ZIndex has changed and marks collection as dirty if needed.
    /// </summary>
    public void CheckForZIndexChanges()
    {
        foreach (var kvp in _lastKnownZIndex)
        {
            var gameObject = kvp.Key;
            var lastKnownZIndex = kvp.Value;

            if (gameObject.ZIndex != lastKnownZIndex)
            {
                _lastKnownZIndex[gameObject] = gameObject.ZIndex;
                _isDirty = true;
            }
        }
    }

    private void EnsureTypeCacheUpdated()
    {
        if (!_isTypeCacheDirty)
        {
            return;
        }

        _gameObjectsByType.Clear();

        foreach (var gameObject in _gameObjects)
        {
            var gameObjectType = gameObject.GetType();
            List<T>? typeList;

            // Add the exact type
            if (!_gameObjectsByType.TryGetValue(gameObjectType, out typeList))
            {
                typeList = new List<T>();
                _gameObjectsByType[gameObjectType] = typeList;
            }

            typeList.Add(gameObject);

            // Add all base types and interfaces that are assignable from T
            var currentType = gameObjectType.BaseType;
            while (currentType != null && typeof(T).IsAssignableFrom(currentType))
            {
                if (!_gameObjectsByType.TryGetValue(currentType, out typeList))
                {
                    typeList = new List<T>();
                    _gameObjectsByType[currentType] = typeList;
                }

                typeList.Add(gameObject);
                currentType = currentType.BaseType;
            }

            // Add all interfaces that are assignable from T
            foreach (var interfaceType in gameObjectType.GetInterfaces())
            {
                if (typeof(T).IsAssignableFrom(interfaceType))
                {
                    if (!_gameObjectsByType.TryGetValue(interfaceType, out typeList))
                    {
                        typeList = new List<T>();
                        _gameObjectsByType[interfaceType] = typeList;
                    }

                    typeList.Add(gameObject);
                }
            }
        }

        _isTypeCacheDirty = false;
    }

    private void EnsureSorted()
    {
        if (!_isDirty)
        {
            return;
        }

        _gameObjects.Sort((x, y) => x.ZIndex.CompareTo(y.ZIndex));
        RebuildIndexMap();
        _isDirty = false;
    }

    private void RebuildIndexMap()
    {
        _gameObjectToIndex.Clear();
        for (var i = 0; i < _gameObjects.Count; i++)
        {
            _gameObjectToIndex[_gameObjects[i]] = i;
        }
    }
}
