using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Graphics;
using SquidVox.Core.Collections;
using SquidVox.Core.Context;
using SquidVox.Core.Interfaces.Services;
using SquidVox.World3d.Rendering;
using SquidVox.World3d.Scripts;

namespace SquidVox.World3d.GameObjects.Debug;

/// <summary>
/// ImGui debugger for visualising texture atlas tiles.
/// </summary>
public sealed class TextureAtlasDebugger : IDisposable
{
    private readonly IAssetManagerService _assetManagerService;
    private readonly ImGuiRenderLayer _imguiLayer;
    private readonly string _windowTitle;
    private readonly int _columns;
    private readonly float _tilePreviewSize;
    private readonly Dictionary<int, (Texture2D Texture, IntPtr Handle)> _tileCache = new();
    private Texture2DAtlas? _currentAtlas;
    private string? _currentAtlasName;
    private IReadOnlyList<string> _atlasNames = [];
    private string _atlasName = string.Empty;
    private int _selectedAtlasIndex = -1;
    private int _selectedIndex = -1;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextureAtlasDebugger"/> class.
    /// </summary>
    public TextureAtlasDebugger(
        string windowTitle = "Texture Atlas Inspector",
        int columns = 8,
        float tilePreviewSize = 48f
    )
    {
        _assetManagerService = SquidVoxEngineContext.GetService<IAssetManagerService>();
        _imguiLayer = SquidVoxEngineContext.GetService<RenderLayerCollection>().GetLayer<ImGuiRenderLayer>();
        _windowTitle = windowTitle;
        _columns = Math.Max(1, columns);
        _tilePreviewSize = Math.Max(16f, tilePreviewSize);
    }

    /// <summary>
    /// Creates the debugger object for registration with the ImGui layer.
    /// </summary>
    public LuaImGuiDebuggerObject CreateDebugger()
    {
        return new LuaImGuiDebuggerObject(_windowTitle, DrawWindow);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        ClearCache();
    }

    private void DrawWindow()
    {
        if (!ImGui.Begin(_windowTitle))
        {
            ImGui.End();
            return;
        }

        ImGui.InputInt("Selected Index", ref _selectedIndex);

        ImGui.Separator();

        var atlasNames = _assetManagerService.GetAtlasTextureNames();
        if (atlasNames == null || atlasNames.Count == 0)
        {
            ImGui.TextColored(new System.Numerics.Vector4(1f, 0.8f, 0.2f, 1f), "No texture atlases loaded.");
            ImGui.End();
            return;
        }

        // Check if atlas names changed
        bool namesChanged = !_atlasNames.SequenceEqual(atlasNames);
        if (namesChanged)
        {
            // Try to preserve the selected atlas
            int newIndex = atlasNames.IndexOf(_atlasName);
            if (newIndex >= 0)
            {
                _selectedAtlasIndex = newIndex;
            }
            else
            {
                _selectedAtlasIndex = 0;
            }
            _atlasNames = atlasNames;
        }

        if (_selectedAtlasIndex < 0 || _selectedAtlasIndex >= _atlasNames.Count)
        {
            _selectedAtlasIndex = 0;
        }

        var currentAtlasName = _atlasNames[_selectedAtlasIndex];
        if (!string.Equals(_atlasName, currentAtlasName, StringComparison.Ordinal))
        {
            _atlasName = currentAtlasName;
            TryEnsureAtlas(_atlasName, out _);
        }

        if (ImGui.BeginCombo("Atlas", _atlasName))
        {
            for (var i = 0; i < _atlasNames.Count; i++)
            {
                var isSelected = i == _selectedAtlasIndex;
                if (ImGui.Selectable(_atlasNames[i], isSelected))
                {
                    if (_selectedAtlasIndex != i)
                    {
                        _selectedAtlasIndex = i;
                        _atlasName = _atlasNames[i];
                        TryEnsureAtlas(_atlasName, out _);
                    }
                }

                if (isSelected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }

            ImGui.EndCombo();
        }

        if (!TryEnsureAtlas(_atlasName, out var atlas))
        {
            ImGui.TextColored(new System.Numerics.Vector4(1f, 0.5f, 0.5f, 1f), $"Atlas '{_atlasName}' not found.");
            ImGui.End();
            return;
        }

        var tiles = atlas.Select(region => region).ToArray();
        if (tiles.Length == 0)
        {
            ImGui.Text("Atlas contains no tiles.");
            ImGui.End();
            return;
        }

        if (_selectedIndex >= tiles.Length)
        {
            _selectedIndex = tiles.Length - 1;
        }

        if (_selectedIndex < -1)
        {
            _selectedIndex = -1;
        }

        ImGui.Text($"Tiles: {tiles.Length}");

        var columnWidth = _tilePreviewSize + 12f;
        if (ImGui.BeginTable("atlas_tiles", _columns, ImGuiTableFlags.ScrollY | ImGuiTableFlags.BordersInner))
        {
            for (var i = 0; i < _columns; i++)
            {
                ImGui.TableSetupColumn($"Col {i}", ImGuiTableColumnFlags.WidthFixed, columnWidth);
            }

            for (var index = 0; index < tiles.Length; index++)
            {
                if (index % _columns == 0)
                {
                    ImGui.TableNextRow();
                }

                ImGui.TableSetColumnIndex(index % _columns);
                DrawTile(index, tiles[index]);
            }

            ImGui.EndTable();
        }

        ImGui.End();
    }

    private void DrawTile(int index, Texture2DRegion region)
    {
        if (!_tileCache.TryGetValue(index, out var cached))
        {
            if (!TryCreateTileTexture(region, out cached))
            {
                ImGui.Text("N/A");
                return;
            }

            _tileCache[index] = cached;
        }

        var isSelected = _selectedIndex == index;
        if (isSelected)
        {
            var cursor = ImGui.GetCursorScreenPos();
            var size = new System.Numerics.Vector2(_tilePreviewSize + 10f, _tilePreviewSize + 10f);
            var color = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.2f, 0.6f, 1f, 0.35f));
            ImGui.GetWindowDrawList().AddRectFilled(cursor, cursor + size, color);
        }

        ImGui.Image(cached.Handle, new System.Numerics.Vector2(_tilePreviewSize, _tilePreviewSize));

        if (ImGui.IsItemClicked())
        {
            _selectedIndex = index;
        }

        ImGui.Text($"#{index}");
    }

    private bool TryEnsureAtlas(string atlasName, out Texture2DAtlas atlas)
    {
        if (_currentAtlas != null && string.Equals(_currentAtlasName, atlasName, StringComparison.Ordinal))
        {
            atlas = _currentAtlas;
            return true;
        }

        ClearCache();

        var nextAtlas = _assetManagerService.GetTextureAtlas(atlasName);
        if (nextAtlas == null)
        {
            atlas = null!;
            return false;
        }

        _currentAtlas = nextAtlas;
        _currentAtlasName = atlasName;
        _selectedIndex = -1;
        atlas = nextAtlas;
        return true;
    }

    private bool TryCreateTileTexture(Texture2DRegion region, out (Texture2D Texture, IntPtr Handle) entry)
    {
        var texture = _assetManagerService.CreateTextureFromRegion(region);
        if (texture == null)
        {
            entry = default;
            return false;
        }

        var handle = _imguiLayer.BindTexture(texture);
        entry = (texture, handle);
        return true;
    }

    private void ClearCache()
    {
        foreach (var (_, value) in _tileCache)
        {
            if (value.Handle != IntPtr.Zero)
            {
                _imguiLayer.UnbindTexture(value.Handle);
            }

            value.Texture?.Dispose();
        }

        _tileCache.Clear();
        _currentAtlas = null;
        _currentAtlasName = null;
        _atlasNames = Array.Empty<string>();
        _selectedAtlasIndex = -1;
    }
}
