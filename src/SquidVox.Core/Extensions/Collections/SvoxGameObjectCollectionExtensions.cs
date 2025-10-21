using FontStashSharp.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SquidVox.Core.Collections;
using SquidVox.Core.Interfaces.GameObjects;

namespace SquidVox.Core.Extensions.Collections;

/// <summary>
/// Extension methods for SvoxGameObjectCollection to provide batch operations.
/// </summary>
public static class SvoxGameObjectCollectionExtensions
{
    /// <summary>
    /// Updates all enabled game objects in the collection.
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    /// <param name="gameTime">Game timing information.</param>
    public static void UpdateAll<T>(this SvoxGameObjectCollection<T> collection, Microsoft.Xna.Framework.GameTime gameTime)
        where T : class, ISVox2dDrawableGameObject
    {
        collection.CheckForZIndexChanges();

        for (var i = 0; i < collection.Count; i++)
        {
            var gameObject = collection[i];
            if (gameObject.IsEnabled)
            {
                gameObject.Update(gameTime);
            }
        }
    }

    /// <summary>
    /// Renders all visible 3D game objects in the collection.
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox3dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of 3D game objects.</param>
    /// <param name="graphicsDevice">The graphics device for rendering.</param>
    public static void RenderAll<T>(this SvoxGameObjectCollection<T> collection, GraphicsDevice graphicsDevice)
        where T : class, ISVox3dDrawableGameObject
    {
        collection.CheckForZIndexChanges();

        for (var i = 0; i < collection.Count; i++)
        {
            var gameObject = collection[i];
            if (gameObject.IsVisible)
            {
                gameObject.Render(graphicsDevice);
            }
        }
    }

    /// <summary>
    /// Renders all visible game objects in the collection, applying scissor clipping for objects with Size set.
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    /// <param name="spriteBatch">SpriteBatch for rendering textures.</param>
    public static void RenderAll<T>(
        this SvoxGameObjectCollection<T> collection,
        SpriteBatch spriteBatch
    )
        where T : class, ISVox2dDrawableGameObject
    {
        collection.CheckForZIndexChanges();

        if (collection.Count == 0) return;

        var graphicsDevice = spriteBatch.GraphicsDevice;
        Rectangle? currentScissor = null;
        bool batchBegun = false;

        for (var i = 0; i < collection.Count; i++)
        {
            var gameObject = collection[i];
            if (!gameObject.IsVisible) continue;

            Rectangle? scissorRect = gameObject.Size != Vector2.Zero ? new Rectangle((int)gameObject.Position.X, (int)gameObject.Position.Y, (int)gameObject.Size.X, (int)gameObject.Size.Y) : null;

            if (scissorRect != currentScissor)
            {
                if (batchBegun)
                {
                    spriteBatch.End();
                    batchBegun = false;
                }

                RasterizerState rasterizerState;
                if (scissorRect.HasValue)
                {
                    rasterizerState = new RasterizerState { ScissorTestEnable = true };
                    graphicsDevice.ScissorRectangle = scissorRect.Value;
                }
                else
                {
                    rasterizerState = new RasterizerState { ScissorTestEnable = false };
                }

                spriteBatch.Begin(rasterizerState: rasterizerState);
                batchBegun = true;
                currentScissor = scissorRect;
            }

            if (!batchBegun)
            {
                spriteBatch.Begin();
                batchBegun = true;
                currentScissor = null;
            }

            gameObject.Render(spriteBatch);
        }

        if (batchBegun)
        {
            spriteBatch.End();
        }
    }

    /// <summary>
    /// Updates and renders all game objects that are enabled and visible.
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    /// <param name="gameTime">Game timing information.</param>
    /// <param name="spriteBatch">SpriteBatch for rendering textures.</param>
    public static void UpdateAndRenderAll<T>(
        this SvoxGameObjectCollection<T> collection,
        GameTime gameTime,
        SpriteBatch spriteBatch
    )
        where T : class, ISVox2dDrawableGameObject
    {
        collection.CheckForZIndexChanges();

        for (var i = 0; i < collection.Count; i++)
        {
            var gameObject = collection[i];

            if (gameObject.IsEnabled)
            {
                gameObject.Update(gameTime);
            }

            if (gameObject.IsVisible)
            {
                gameObject.Render(spriteBatch);
            }
        }
    }

    /// <summary>
    /// Updates game objects within a specific ZIndex range.
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    /// <param name="gameTime">Game timing information.</param>
    /// <param name="minZIndex">Minimum ZIndex (inclusive).</param>
    /// <param name="maxZIndex">Maximum ZIndex (inclusive).</param>
    public static void UpdateRange<T>(
        this SvoxGameObjectCollection<T> collection,
        Microsoft.Xna.Framework.GameTime gameTime,
        int minZIndex,
        int maxZIndex
    )
        where T : class, ISVox2dDrawableGameObject
    {
        collection.CheckForZIndexChanges();

        for (var i = 0; i < collection.Count; i++)
        {
            var gameObject = collection[i];
            if (gameObject.IsEnabled && gameObject.ZIndex >= minZIndex && gameObject.ZIndex <= maxZIndex)
            {
                gameObject.Update(gameTime);
            }
        }
    }

    /// <summary>
    /// Renders game objects within a specific ZIndex range.
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    /// <param name="spriteBatch">SpriteBatch for rendering textures.</param>
    /// <param name="minZIndex">Minimum ZIndex (inclusive).</param>
    /// <param name="maxZIndex">Maximum ZIndex (inclusive).</param>
    public static void RenderRange<T>(
        this SvoxGameObjectCollection<T> collection,
        SpriteBatch spriteBatch,
        int minZIndex,
        int maxZIndex
    )
        where T : class, ISVox2dDrawableGameObject
    {
        collection.CheckForZIndexChanges();

        for (var i = 0; i < collection.Count; i++)
        {
            var gameObject = collection[i];
            if (gameObject.IsVisible && gameObject.ZIndex >= minZIndex && gameObject.ZIndex <= maxZIndex)
            {
                gameObject.Render(spriteBatch);
            }
        }
    }

    /// <summary>
    /// Gets all enabled game objects from the collection.
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    /// <returns>Enumerable of enabled game objects.</returns>
    public static IEnumerable<T> GetEnabled<T>(this SvoxGameObjectCollection<T> collection)
        where T : class, ISVox2dDrawableGameObject
    {
        collection.CheckForZIndexChanges();
        return collection.GetEnabledGameObjects();
    }

    /// <summary>
    /// Gets all disabled game objects from the collection.
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    /// <returns>Enumerable of disabled game objects.</returns>
    public static IEnumerable<T> GetDisabled<T>(this SvoxGameObjectCollection<T> collection)
        where T : class, ISVox2dDrawableGameObject
    {
        collection.CheckForZIndexChanges();

        for (var i = 0; i < collection.Count; i++)
        {
            var gameObject = collection[i];
            if (!gameObject.IsEnabled)
            {
                yield return gameObject;
            }
        }
    }

    /// <summary>
    /// Gets all visible game objects from the collection.
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    /// <returns>Enumerable of visible game objects.</returns>
    public static IEnumerable<T> GetVisible<T>(this SvoxGameObjectCollection<T> collection)
        where T : class, ISVox2dDrawableGameObject
    {
        collection.CheckForZIndexChanges();
        return collection.GetVisibleGameObjects();
    }

    /// <summary>
    /// Gets all invisible game objects from the collection.
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    /// <returns>Enumerable of invisible game objects.</returns>
    public static IEnumerable<T> GetInvisible<T>(this SvoxGameObjectCollection<T> collection)
        where T : class, ISVox2dDrawableGameObject
    {
        collection.CheckForZIndexChanges();

        for (var i = 0; i < collection.Count; i++)
        {
            var gameObject = collection[i];
            if (!gameObject.IsVisible)
            {
                yield return gameObject;
            }
        }
    }

    /// <summary>
    /// Gets all game objects that are both enabled and visible.
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    /// <returns>Enumerable of active game objects.</returns>
    public static IEnumerable<T> GetActive<T>(this SvoxGameObjectCollection<T> collection)
        where T : class, ISVox2dDrawableGameObject
    {
        collection.CheckForZIndexChanges();
        return collection.GetActiveGameObjects();
    }

    /// <summary>
    /// Gets game objects that are visible and enabled ordered by descending ZIndex.
    /// Useful for hit-testing scenarios where top-most game objects should be evaluated first.
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="gameObjects">Enumerable of game objects to evaluate.</param>
    /// <returns>Enumerable of visible and enabled game objects ordered by ZIndex (highest first).</returns>
    public static IEnumerable<T> GetActiveDescendingByZIndex<T>(this IEnumerable<T> gameObjects)
        where T : class, ISVox2dDrawableGameObject
    {
        var filtered = new List<T>();

        foreach (var gameObject in gameObjects)
        {
            if (gameObject.IsVisible && gameObject.IsEnabled)
            {
                filtered.Add(gameObject);
            }
        }

        if (filtered.Count == 0)
        {
            yield break;
        }

        filtered.Sort(static (left, right) => right.ZIndex.CompareTo(left.ZIndex));

        for (var i = 0; i < filtered.Count; i++)
        {
            yield return filtered[i];
        }
    }

    /// <summary>
    /// Finds the first game object with the specified name (case-insensitive).
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    /// <param name="name">Name to search for.</param>
    /// <returns>First game object with matching name, or null if not found.</returns>
    public static T? FindByName<T>(this SvoxGameObjectCollection<T> collection, string name)
        where T : class, ISVox2dDrawableGameObject
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        for (var i = 0; i < collection.Count; i++)
        {
            var gameObject = collection[i];
            if (string.Equals(gameObject.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return gameObject;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds all game objects with the specified name (case-insensitive).
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    /// <param name="name">Name to search for.</param>
    /// <returns>Enumerable of game objects with matching name.</returns>
    public static IEnumerable<T> FindAllByName<T>(this SvoxGameObjectCollection<T> collection, string name)
        where T : class, ISVox2dDrawableGameObject
    {
        if (string.IsNullOrEmpty(name))
        {
            yield break;
        }

        for (var i = 0; i < collection.Count; i++)
        {
            var gameObject = collection[i];
            if (string.Equals(gameObject.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                yield return gameObject;
            }
        }
    }

    /// <summary>
    /// Gets game objects of a specific type.
    /// </summary>
    /// <typeparam name="T">Base type implementing ISVox2dDrawableGameObject</typeparam>
    /// <typeparam name="TSpecific">Specific type to filter for.</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    /// <returns>Enumerable of game objects of the specified type.</returns>
    public static IEnumerable<TSpecific> OfType<T, TSpecific>(this SvoxGameObjectCollection<T> collection)
        where T : class, ISVox2dDrawableGameObject
        where TSpecific : class, T
    {
        for (var i = 0; i < collection.Count; i++)
        {
            if (collection[i] is TSpecific specificGameObject)
            {
                yield return specificGameObject;
            }
        }
    }

    /// <summary>
    /// Updates all enabled game objects in a specific ZIndex layer.
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    /// <param name="gameTime">Game timing information.</param>
    /// <param name="zIndex">Exact ZIndex to update.</param>
    public static void UpdateLayer<T>(this SvoxGameObjectCollection<T> collection, Microsoft.Xna.Framework.GameTime gameTime, int zIndex)
        where T : class, ISVox2dDrawableGameObject
    {
        collection.CheckForZIndexChanges();

        for (var i = 0; i < collection.Count; i++)
        {
            var gameObject = collection[i];
            if (gameObject.IsEnabled && gameObject.ZIndex == zIndex)
            {
                gameObject.Update(gameTime);
            }
        }
    }

    /// <summary>
    /// Renders all visible game objects in a specific ZIndex layer.
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    /// <param name="spriteBatch">SpriteBatch for rendering textures.</param>
    /// <param name="zIndex">Exact ZIndex to render.</param>
    public static void RenderLayer<T>(
        this SvoxGameObjectCollection<T> collection,
        SpriteBatch spriteBatch,
        int zIndex
    )
        where T : class, ISVox2dDrawableGameObject
    {
        collection.CheckForZIndexChanges();

        for (var i = 0; i < collection.Count; i++)
        {
            var gameObject = collection[i];
            if (gameObject.IsVisible && gameObject.ZIndex == zIndex)
            {
                gameObject.Render(spriteBatch);
            }
        }
    }

    /// <summary>
    /// Counts game objects matching a predicate.
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    /// <param name="predicate">Predicate to match game objects.</param>
    /// <returns>Number of matching game objects.</returns>
    public static int CountWhere<T>(this SvoxGameObjectCollection<T> collection, Func<T, bool> predicate)
        where T : class, ISVox2dDrawableGameObject
    {
        var count = 0;
        for (var i = 0; i < collection.Count; i++)
        {
            if (predicate(collection[i]))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Checks if any game object matches a predicate.
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    /// <param name="predicate">Predicate to match game objects.</param>
    /// <returns>True if any game object matches.</returns>
    public static bool Any<T>(this SvoxGameObjectCollection<T> collection, Func<T, bool> predicate)
        where T : class, ISVox2dDrawableGameObject
    {
        for (var i = 0; i < collection.Count; i++)
        {
            if (predicate(collection[i]))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if all game objects match a predicate.
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    /// <param name="predicate">Predicate to match game objects.</param>
    /// <returns>True if all game objects match.</returns>
    public static bool All<T>(this SvoxGameObjectCollection<T> collection, Func<T, bool> predicate)
        where T : class, ISVox2dDrawableGameObject
    {
        for (var i = 0; i < collection.Count; i++)
        {
            if (!predicate(collection[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Enables all game objects in the collection.
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    public static void EnableAll<T>(this SvoxGameObjectCollection<T> collection)
        where T : class, ISVox2dDrawableGameObject
    {
        for (var i = 0; i < collection.Count; i++)
        {
            collection[i].IsEnabled = true;
        }
    }

    /// <summary>
    /// Disables all game objects in the collection.
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    public static void DisableAll<T>(this SvoxGameObjectCollection<T> collection)
        where T : class, ISVox2dDrawableGameObject
    {
        for (var i = 0; i < collection.Count; i++)
        {
            collection[i].IsEnabled = false;
        }
    }

    /// <summary>
    /// Makes all game objects visible in the collection.
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    public static void ShowAll<T>(this SvoxGameObjectCollection<T> collection)
        where T : class, ISVox2dDrawableGameObject
    {
        for (var i = 0; i < collection.Count; i++)
        {
            collection[i].IsVisible = true;
        }
    }

    /// <summary>
    /// Makes all game objects invisible in the collection.
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    public static void HideAll<T>(this SvoxGameObjectCollection<T> collection)
        where T : class, ISVox2dDrawableGameObject
    {
        for (var i = 0; i < collection.Count; i++)
        {
            collection[i].IsVisible = false;
        }
    }

    /// <summary>
    /// Sets the ZIndex for all game objects in the collection.
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    /// <param name="zIndex">ZIndex value to set.</param>
    public static void SetZIndexAll<T>(this SvoxGameObjectCollection<T> collection, int zIndex)
        where T : class, ISVox2dDrawableGameObject
    {
        for (var i = 0; i < collection.Count; i++)
        {
            collection[i].ZIndex = zIndex;
        }

        collection.ForceSort();
    }

    /// <summary>
    /// Offsets the ZIndex for all game objects in the collection.
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    /// <param name="offset">ZIndex offset to apply.</param>
    public static void OffsetZIndexAll<T>(this SvoxGameObjectCollection<T> collection, int offset)
        where T : class, ISVox2dDrawableGameObject
    {
        for (var i = 0; i < collection.Count; i++)
        {
            collection[i].ZIndex += offset;
        }

        collection.ForceSort();
    }

    /// <summary>
    /// Gets the minimum ZIndex value in the collection.
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    /// <returns>Minimum ZIndex value, or 0 if collection is empty.</returns>
    public static int GetMinZIndex<T>(this SvoxGameObjectCollection<T> collection)
        where T : class, ISVox2dDrawableGameObject
    {
        if (collection.Count == 0)
        {
            return 0;
        }

        var minZIndex = int.MaxValue;

        for (var i = 0; i < collection.Count; i++)
        {
            if (collection[i].ZIndex < minZIndex)
            {
                minZIndex = collection[i].ZIndex;
            }
        }

        return minZIndex;
    }

    /// <summary>
    /// Gets the maximum ZIndex value in the collection.
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    /// <returns>Maximum ZIndex value, or 0 if collection is empty.</returns>
    public static int GetMaxZIndex<T>(this SvoxGameObjectCollection<T> collection)
        where T : class, ISVox2dDrawableGameObject
    {
        if (collection.Count == 0)
        {
            return 0;
        }

        var maxZIndex = int.MinValue;

        for (var i = 0; i < collection.Count; i++)
        {
            if (collection[i].ZIndex > maxZIndex)
            {
                maxZIndex = collection[i].ZIndex;
            }
        }

        return maxZIndex;
    }

    /// <summary>
    /// Initializes all game objects in the collection that implement ISVoxInitializable.
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    public static void InitializeAll<T>(this SvoxGameObjectCollection<T> collection)
        where T : class, ISVox2dDrawableGameObject
    {
        for (var i = 0; i < collection.Count; i++)
        {
            if (collection[i] is ISVoxInitializable initializable)
            {
                initializable.Initialize();
            }
        }
    }

    /// <summary>
    /// Gets all game objects that implement ISVoxInitializable.
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    /// <returns>Enumerable of initializable game objects.</returns>
    public static IEnumerable<ISVoxInitializable> GetInitializable<T>(this SvoxGameObjectCollection<T> collection)
        where T : class, ISVox2dDrawableGameObject
    {
        for (var i = 0; i < collection.Count; i++)
        {
            if (collection[i] is ISVoxInitializable initializable)
            {
                yield return initializable;
            }
        }
    }

    /// <summary>
    /// Adds a game object to the collection and initializes it if it implements ISVoxInitializable.
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    /// <param name="gameObject">Game object to add.</param>
    public static void AddAndInitialize<T>(this SvoxGameObjectCollection<T> collection, T gameObject)
        where T : class, ISVox2dDrawableGameObject
    {
        collection.Add(gameObject);

        if (gameObject is ISVoxInitializable initializable)
        {
            initializable.Initialize();
        }
    }

    /// <summary>
    /// Handles keyboard input for all game objects that have focus and implement ISVoxInputReceiver.
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    /// <param name="keyboardState">The current keyboard state.</param>
    /// <param name="gameTime">Game timing information.</param>
    public static void HandleKeyboardInput<T>(this SvoxGameObjectCollection<T> collection, KeyboardState keyboardState, Microsoft.Xna.Framework.GameTime gameTime)
        where T : class, ISVox2dDrawableGameObject
    {
        for (var i = 0; i < collection.Count; i++)
        {
            if (collection[i] is ISVoxInputReceiver inputReceiver && inputReceiver.HasFocus)
            {
                inputReceiver.HandleKeyboard(keyboardState, gameTime);
            }
        }
    }

    /// <summary>
    /// Handles mouse input for all game objects that have focus and implement ISVoxInputReceiver.
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    /// <param name="mouseState">The current mouse state.</param>
    /// <param name="gameTime">Game timing information.</param>
    public static void HandleMouseInput<T>(this SvoxGameObjectCollection<T> collection, MouseState mouseState, Microsoft.Xna.Framework.GameTime gameTime)
        where T : class, ISVox2dDrawableGameObject
    {
        for (var i = 0; i < collection.Count; i++)
        {
            if (collection[i] is ISVoxInputReceiver inputReceiver && inputReceiver.HasFocus)
            {
                inputReceiver.HandleMouse(mouseState, gameTime);
            }
        }
    }

    /// <summary>
    /// Handles both keyboard and mouse input for all game objects that have focus and implement ISVoxInputReceiver.
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    /// <param name="keyboardState">The current keyboard state.</param>
    /// <param name="mouseState">The current mouse state.</param>
    /// <param name="gameTime">Game timing information.</param>
    public static void HandleInput<T>(
        this SvoxGameObjectCollection<T> collection,
        KeyboardState keyboardState,
        MouseState mouseState,
        Microsoft.Xna.Framework.GameTime gameTime
    )
        where T : class, ISVox2dDrawableGameObject
    {
        for (var i = 0; i < collection.Count; i++)
        {
            if (collection[i] is ISVoxInputReceiver inputReceiver && inputReceiver.HasFocus)
            {
                inputReceiver.HandleKeyboard(keyboardState, gameTime);
                inputReceiver.HandleMouse(mouseState, gameTime);
            }
        }
    }

    /// <summary>
    /// Gets all game objects that implement ISVoxInputReceiver.
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    /// <returns>Enumerable of input receiver game objects.</returns>
    public static IEnumerable<ISVoxInputReceiver> GetInputReceivers<T>(this SvoxGameObjectCollection<T> collection)
        where T : class, ISVox2dDrawableGameObject
    {
        for (var i = 0; i < collection.Count; i++)
        {
            if (collection[i] is ISVoxInputReceiver inputReceiver)
            {
                yield return inputReceiver;
            }
        }
    }

    /// <summary>
    /// Gets all game objects that have input focus.
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    /// <returns>Enumerable of game objects with focus.</returns>
    public static IEnumerable<ISVoxInputReceiver> GetFocusedInputReceivers<T>(this SvoxGameObjectCollection<T> collection)
        where T : class, ISVox2dDrawableGameObject
    {
        for (var i = 0; i < collection.Count; i++)
        {
            if (collection[i] is ISVoxInputReceiver inputReceiver && inputReceiver.HasFocus)
            {
                yield return inputReceiver;
            }
        }
    }

    /// <summary>
    /// Sets focus to a specific game object and removes focus from all others.
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    /// <param name="gameObject">Game object to give focus to.</param>
    public static void SetFocus<T>(this SvoxGameObjectCollection<T> collection, T gameObject)
        where T : class, ISVox2dDrawableGameObject
    {
        for (var i = 0; i < collection.Count; i++)
        {
            if (collection[i] is ISVoxInputReceiver inputReceiver)
            {
                inputReceiver.HasFocus = ReferenceEquals(collection[i], gameObject);
            }
        }
    }

    /// <summary>
    /// Removes focus from all game objects.
    /// </summary>
    /// <typeparam name="T">Type implementing ISVox2dDrawableGameObject</typeparam>
    /// <param name="collection">Collection of game objects.</param>
    public static void ClearFocus<T>(this SvoxGameObjectCollection<T> collection)
        where T : class, ISVox2dDrawableGameObject
    {
        for (var i = 0; i < collection.Count; i++)
        {
            if (collection[i] is ISVoxInputReceiver inputReceiver)
            {
                inputReceiver.HasFocus = false;
            }
        }
    }
}
