using System.Collections.Concurrent;
using DryIoc;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using SquidVox.Core.Context;
using SquidVox.Core.GameObjects;
using SquidVox.Voxel.Interfaces.Services;
using SquidVox.Voxel.Primitives;
using SquidVox.Voxel.Systems;
using SquidVox.Voxel.Types;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;
using SysVector3 = System.Numerics.Vector3;

namespace SquidVox.Voxel.GameObjects;

/// <summary>
/// Manages multiple chunks with dynamic loading/unloading and frustum culling.
/// </summary>
public sealed class WorldGameObject : Base3dGameObject, IDisposable
{
    private readonly ILogger _logger = Log.ForContext<WorldGameObject>();
    private readonly IBlockManagerService _blockManagerService;

    private readonly ConcurrentDictionary<XnaVector3, ChunkGameObject> _chunks = new();
    private readonly ConcurrentQueue<(XnaVector3 Position, ChunkEntity Chunk)> _pendingChunks = new();
    private readonly Queue<ChunkGameObject> _meshBuildQueue = new();


    private readonly Particle3dGameObject _particleGameObject;
    private readonly ChunkLightSystem _lightSystem;
    private readonly WaterSimulationSystem _waterSystem;

    private bool _isDisposed;
    private BoundingFrustum? _frustum;
    private (int X, int Y, int Z)? _lastPlayerChunk;
    // private Color _lastSunColor;

    public WorldGameObject(CameraGameObject camera)
    {
        Camera = camera ?? throw new ArgumentNullException(nameof(camera));
        _blockManagerService = SquidVoxEngineContext.Container.Resolve<IBlockManagerService>();
        _particleGameObject = new Particle3dGameObject();
        _lightSystem = new ChunkLightSystem();
        _waterSystem = new WaterSimulationSystem();

        Camera.IsBlockSolid = pos => IsBlockSolidForCollision(pos, includeWater: false);
    }

    /// <summary>
    /// Delegate for asynchronous chunk generation.
    /// </summary>
    /// <param name="chunkX">The X coordinate of the chunk.</param>
    /// <param name="chunkY">The Y coordinate of the chunk.</param>
    /// <param name="chunkZ">The Z coordinate of the chunk.</param>
    /// <returns>A task that represents the asynchronous operation, containing the generated chunk entity.</returns>
    public delegate Task<ChunkEntity> ChunkGeneratorAsyncHandler(int chunkX, int chunkY, int chunkZ);

    /// <summary>
    /// Gets or sets the asynchronous chunk generator delegate.
    /// </summary>
    public ChunkGeneratorAsyncHandler? ChunkGenerator { get; set; }

    /// <summary>
    /// Gets the camera component used by the world.
    /// </summary>
    public CameraGameObject Camera { get; }

    /// <summary>
    /// Gets a read-only dictionary of loaded chunks.
    /// </summary>
    public IReadOnlyDictionary<XnaVector3, ChunkGameObject> Chunks => _chunks;

    /// <summary>
    /// Gets or sets the render distance for chunks.
    /// </summary>
    public float ViewRange { get; set; } = 200f;

    /// <summary>
    /// Gets or sets the pre-load distance for chunks.
    /// </summary>
    public float GenerationRange { get; set; } = 250f;

    /// <summary>
    /// Gets or sets a value indicating whether frustum culling is enabled.
    /// </summary>
    public bool EnableFrustumCulling { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum distance for block raycasting.
    /// </summary>
    public float MaxRaycastDistance { get; set; } = 10f;

    /// <summary>
    /// Gets or sets the visible chunks grid distance.
    /// </summary>
    public int ChunkLoadDistance { get; set; } = 2;

    /// <summary>
    /// Gets or sets the pre-load chunks grid distance.
    /// </summary>
    public int GenerationDistance { get; set; } = 3;

    /// <summary>
    /// Gets or sets the vertical chunk load distance (above and below player).
    /// </summary>
    public int VerticalChunkDistance { get; set; } = 1;

    /// <summary>
    /// Gets or sets the maximum number of chunk meshes to build per frame.
    /// </summary>
    public int MaxChunkBuildsPerFrame { get; set; } = 2;

    /// <summary>
    /// Gets or sets whether to render chunks in wireframe mode.
    /// </summary>
    public bool EnableWireframe { get; set; }

    /// <summary>
    /// Gets or sets whether chunks should use greedy meshing during mesh generation.
    /// </summary>
    public bool UseGreedyMeshing { get; set; }

    private bool _fogEnabled = true;
    private Vector3 _fogColor = new Vector3(0.6f, 0.75f, 0.9f);
    private float _fogStart = 80f;
    private float _fogEnd = 150f;

    /// <summary>
    /// Gets or sets whether distance fog is applied to chunks.
    /// </summary>
    public bool FogEnabled
    {
        get => _fogEnabled;
        set
        {
            if (_fogEnabled == value)
            {
                return;
            }

            _fogEnabled = value;
            foreach (var chunk in _chunks.Values)
            {
                chunk.FogEnabled = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the fog color applied during rendering.
    /// </summary>
    public Vector3 FogColor
    {
        get => _fogColor;
        set
        {
            if (_fogColor == value)
            {
                return;
            }

            _fogColor = value;
            foreach (var chunk in _chunks.Values)
            {
                chunk.FogColor = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the distance at which fog begins.
    /// </summary>
    public float FogStart
    {
        get => _fogStart;
        set
        {
            if (Math.Abs(_fogStart - value) < float.Epsilon)
            {
                return;
            }

            _fogStart = value;
            foreach (var chunk in _chunks.Values)
            {
                chunk.FogStart = value;
                if (chunk.FogEnd <= value)
                {
                    chunk.FogEnd = value + 1f;
                }
            }

            if (_fogEnd <= _fogStart)
            {
                FogEnd = _fogStart + 1f;
            }
        }
    }

    /// <summary>
    /// Gets or sets the distance at which fog becomes fully opaque.
    /// </summary>
    public float FogEnd
    {
        get => _fogEnd;
        set
        {
            var adjusted = MathF.Max(_fogStart + 1f, value);

            if (Math.Abs(_fogEnd - adjusted) < float.Epsilon)
            {
                return;
            }

            _fogEnd = adjusted;
            foreach (var chunk in _chunks.Values)
            {
                chunk.FogEnd = adjusted;
            }
        }
    }

    /// <summary>
    /// Gets or sets the ambient light color for chunk rendering.
    /// </summary>
    public Vector3 AmbientLight { get; set; } = new Vector3(0.5f, 0.5f, 0.5f);

    /// <summary>
    /// Gets or sets the directional light direction for chunk rendering.
    /// </summary>
    public Vector3 LightDirection { get; set; } = new Vector3(0.8f, 1.0f, 0.7f);

    /// <summary>
    /// Gets the currently selected block from raycasting.
    /// </summary>
    public (ChunkGameObject? Chunk, int X, int Y, int Z)? SelectedBlock { get; private set; }

    public void CalculateInitialLighting(ChunkEntity chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);

        _lightSystem.CalculateInitialSunlight(chunk);
    }

    /// <summary>
    /// Adds a chunk to the world asynchronously.
    /// </summary>
    /// <param name="chunk">The chunk entity to add.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task AddChunkAsync(ChunkEntity chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);

        var chunkPosition = chunk.Position;

        _pendingChunks.Enqueue((chunkPosition, chunk));
        _logger.Debug("Chunk queued for addition at position {Position}", chunkPosition);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes a chunk from the world at the specified position.
    /// </summary>
    /// <param name="position">The position of the chunk to remove.</param>
    /// <returns>True if the chunk was removed; otherwise, false.</returns>
    public bool RemoveChunk(XnaVector3 position)
    {
        if (_chunks.TryRemove(position, out var chunkComponent))
        {
            chunkComponent.Dispose();
            _logger.Debug("Chunk removed at position {Position}", position);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the chunk component at the specified position.
    /// </summary>
    /// <param name="position">The position of the chunk.</param>
    /// <returns>The chunk component if found; otherwise, null.</returns>
    public ChunkGameObject? GetChunk(SysVector3 position)
    {
        return _chunks.GetValueOrDefault(position);
    }

    /// <summary>
    /// Gets the chunk entity at the specified world position.
    /// </summary>
    /// <param name="position">The world position.</param>
    /// <returns>The chunk entity if found; otherwise, null.</returns>
    public ChunkEntity? GetChunkEntity(XnaVector3 position)
    {
        var chunkPos = new SysVector3(position.X, position.Y, position.Z);
        return _chunks.TryGetValue(chunkPos, out var chunk) ? chunk.Chunk : null;
    }

    /// <summary>
    /// Determines whether the block at the specified world position is solid.
    /// </summary>
    /// <param name="worldPosition">The world position to check.</param>
    /// <returns>True if the block is solid; otherwise, false.</returns>
    public bool IsBlockSolid(XnaVector3 worldPosition)
    {
        return IsBlockSolidForCollision(worldPosition, false);
    }

    /// <summary>
    /// Determines whether the block at the specified world position is solid for collision purposes.
    /// </summary>
    /// <param name="worldPosition">The world position to check.</param>
    /// <param name="includeWater">Whether to consider water as solid.</param>
    /// <returns>True if the block is solid; otherwise, false.</returns>
    public bool IsBlockSolidForCollision(XnaVector3 worldPosition, bool includeWater = false)
    {
        var blockX = (int)MathF.Floor(worldPosition.X);
        var blockY = (int)MathF.Floor(worldPosition.Y);
        var blockZ = (int)MathF.Floor(worldPosition.Z);

        var chunkX = MathF.Floor(blockX / (float)ChunkEntity.Size) * ChunkEntity.Size;
        var chunkZ = MathF.Floor(blockZ / (float)ChunkEntity.Size) * ChunkEntity.Size;

        var chunkEntity = GetChunkEntity(new XnaVector3(chunkX, 0f, chunkZ));
        if (chunkEntity == null)
        {
            return false;
        }

        var localX = blockX - (int)chunkEntity.Position.X;
        var localY = blockY - (int)chunkEntity.Position.Y;
        var localZ = blockZ - (int)chunkEntity.Position.Z;

        if (!chunkEntity.IsInBounds(localX, localY, localZ))
        {
            return false;
        }

        var block = chunkEntity.GetBlock(localX, localY, localZ);
        if (block == null || block.BlockType == BlockType.Air)
        {
            return false;
        }

        // L'acqua non è solida per le collisioni (puoi camminarci attraverso)
        if (!includeWater && block.BlockType == BlockType.Water)
        {
            return false;
        }

        var definition = _blockManagerService.GetBlockDefinition(block.BlockType);
        return definition?.IsSolid ?? false;
    }

    /// <summary>
    /// Clears all loaded chunks from the world.
    /// </summary>
    public void ClearChunks()
    {
        foreach (var chunk in _chunks.Values)
        {
            chunk.Dispose();
        }

        _chunks.Clear();
        _logger.Information("All chunks cleared");
    }

    /// <summary>
    /// Updates the world component, processing chunks, camera, and systems.
    /// </summary>
    /// <param name="gameTime">The game time information.</param>
    protected override void OnUpdate(GameTime gameTime)
    {
        ProcessPendingChunks();

        ProcessMeshBuildQueue();

        Camera.Update(gameTime);

        // Update day/night cycle
        // _dayNightCycle.Update(gameTime);

        // Check if sun color changed significantly
        // var currentSunColor = _dayNightCycle.GetSunColor();
        // var colorDifference = Math.Abs(currentSunColor.R - _lastSunColor.R) +
        //                      Math.Abs(currentSunColor.G - _lastSunColor.G) +
        //                      Math.Abs(currentSunColor.B - _lastSunColor.B);

        // if (colorDifference > 0.01f) // If color changed by more than 1%
        // {
        //     _lastSunColor = currentSunColor;

        //     // Invalidate all chunk meshes for lighting update
        //     foreach (var chunk in _chunks.Values)
        //     {
        //         if (chunk.HasMesh)
        //         {
        //             chunk.InvalidateGeometry();
        //             _meshBuildQueue.Enqueue(chunk);
        //         }
        //     }

        //     _logger.Debug("Invalidated all chunk meshes for sun color change (diff: {Difference:F3})", colorDifference);
        // }

        // Update particle component with camera matrices
        _particleGameObject.View = Camera.View;
        _particleGameObject.Projection = Camera.Projection;
        _particleGameObject.Update(gameTime);

        UpdateChunkLoading();

        UpdateBlockSelection();

        UpdateWaterSimulation();

        foreach (var chunk in _chunks.Values)
        {
            chunk.Update(gameTime);
        }
    }

    private void UpdateWaterSimulation()
    {
        _waterSystem.Update(
            GetChunkAtWorldPosition,
            (chunk, x, y, z) => chunk.GetBlock(x, y, z),
            (chunk, x, y, z, block) =>
            {
                chunk.SetBlock(x, y, z, block);
                chunk.IsLightingDirty = true;
                var chunkPos = new SysVector3(chunk.Position.X, chunk.Position.Y, chunk.Position.Z);
                if (_chunks.TryGetValue(chunkPos, out var chunkComponent))
                {
                    chunkComponent.InvalidateGeometry();
                    _meshBuildQueue.Enqueue(chunkComponent);
                }
            }
        );
    }

    private ChunkEntity? GetChunkAtWorldPosition(XnaVector3 worldPos)
    {
        var chunkX = MathF.Floor(worldPos.X / ChunkEntity.Size) * ChunkEntity.Size;
        var chunkZ = MathF.Floor(worldPos.Z / ChunkEntity.Size) * ChunkEntity.Size;
        var chunkPos = new SysVector3(chunkX, 0f, chunkZ);

        if (_chunks.TryGetValue(chunkPos, out var chunkComponent))
        {
            return chunkComponent.Chunk;
        }

        return null;
    }

    private void ProcessMeshBuildQueue()
    {
        var built = 0;
        while (built < MaxChunkBuildsPerFrame && _meshBuildQueue.TryDequeue(out var chunk))
        {
            chunk.BuildMeshImmediate();
            built++;
        }

        if (_meshBuildQueue.Count > 0)
        {
            _logger.Verbose("Mesh build queue: {Remaining} chunks remaining", _meshBuildQueue.Count);
        }
    }

    private void UpdateChunkLoading()
    {
        if (ChunkGenerator == null)
        {
            return;
        }

        var cameraPos = Camera.Position;
        var playerChunkX = (int)MathF.Floor(cameraPos.X / ChunkEntity.Size);
        var playerChunkY = (int)MathF.Floor(cameraPos.Y / ChunkEntity.Height);
        var playerChunkZ = (int)MathF.Floor(cameraPos.Z / ChunkEntity.Size);

        var currentPlayerChunk = (playerChunkX, playerChunkY, playerChunkZ);

        if (_lastPlayerChunk == currentPlayerChunk)
        {
            return;
        }

        _lastPlayerChunk = currentPlayerChunk;
        _logger.Information(
            "Player moved to chunk ({ChunkX}, {ChunkY}, {ChunkZ})",
            playerChunkX,
            playerChunkY,
            playerChunkZ
        );

        LoadChunksAroundPlayer(playerChunkX, playerChunkY, playerChunkZ);
        UnloadDistantChunks(playerChunkX, playerChunkY, playerChunkZ);
    }

    private void LoadChunksAroundPlayer(int centerX, int centerY, int centerZ)
    {
        var loadedNewChunks = false;

        _logger.Information(
            "Loading chunks around player: Y=[{MinY}, {MaxY}], X=[{MinX}, {MaxX}], Z=[{MinZ}, {MaxZ}]",
            centerY - VerticalChunkDistance,
            centerY + VerticalChunkDistance,
            centerX - GenerationDistance,
            centerX + GenerationDistance,
            centerZ - GenerationDistance,
            centerZ + GenerationDistance
        );

        for (int y = centerY - VerticalChunkDistance; y <= centerY + VerticalChunkDistance; y++)
        {
            for (int x = centerX - GenerationDistance; x <= centerX + GenerationDistance; x++)
            {
                for (int z = centerZ - GenerationDistance; z <= centerZ + GenerationDistance; z++)
                {
                    var chunkPos = new SysVector3(x * ChunkEntity.Size, y * ChunkEntity.Height, z * ChunkEntity.Size);

                    if (!_chunks.ContainsKey(chunkPos))
                    {
                        _ = RequestChunkAsync(x, y, z);
                        loadedNewChunks = true;
                    }
                }
            }
        }

        if (loadedNewChunks)
        {
            foreach (var chunk in _chunks.Values)
            {
                if (chunk.HasMesh)
                {
                    chunk.InvalidateGeometry();
                    _meshBuildQueue.Enqueue(chunk);
                }
            }
        }
    }

    private async Task RequestChunkAsync(int chunkX, int chunkY, int chunkZ)
    {
        try
        {
            _logger.Debug("Requesting chunk ({X}, {Y}, {Z})", chunkX, chunkY, chunkZ);

            var chunk = await ChunkGenerator!(chunkX, chunkY, chunkZ);

            if (chunk == null)
            {
                _logger.Warning("Chunk generator returned null for chunk ({X}, {Y}, {Z})", chunkX, chunkY, chunkZ);
                return;
            }


            await AddChunkAsync(chunk);

            _logger.Debug("Chunk ({X}, {Y}, {Z}) received", chunkX, chunkY, chunkZ);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load chunk ({X}, {Y}, {Z})", chunkX, chunkY, chunkZ);
        }
    }

    private void UnloadDistantChunks(int centerX, int centerY, int centerZ)
    {
        var unloadDistance = GenerationDistance + 1;
        var unloadDistanceY = VerticalChunkDistance + 1;

        var chunksToRemove = _chunks.Keys
            .Where(pos =>
                {
                    var chunkX = (int)(pos.X / ChunkEntity.Size);
                    var chunkY = (int)(pos.Y / ChunkEntity.Height);
                    var chunkZ = (int)(pos.Z / ChunkEntity.Size);
                    var distanceX = Math.Abs(chunkX - centerX);
                    var distanceY = Math.Abs(chunkY - centerY);
                    var distanceZ = Math.Abs(chunkZ - centerZ);
                    return distanceX > unloadDistance || distanceY > unloadDistanceY || distanceZ > unloadDistance;
                }
            )
            .ToList();

        foreach (var pos in chunksToRemove)
        {
            RemoveChunk(pos);
        }

        if (chunksToRemove.Count > 0)
        {
            _logger.Information("Unloaded {Count} distant chunks", chunksToRemove.Count);
        }
    }

    private void UpdateBlockSelection()
    {
        var ray = Camera.GetPickRay();
        SelectedBlock = RaycastBlock(ray);
    }

    /// <summary>
    /// Performs raycasting to find the first solid block hit by the ray.
    /// </summary>
    /// <param name="ray">The ray to cast.</param>
    /// <returns>The hit block information, or null if no block was hit.</returns>
    public (ChunkGameObject? Chunk, int X, int Y, int Z)? RaycastBlock(Ray ray)
    {
        var step = 0.1f;
        var currentDistance = 0f;

        while (currentDistance < MaxRaycastDistance)
        {
            var point = ray.Position + ray.Direction * currentDistance;

            var chunkX = MathF.Floor(point.X / ChunkEntity.Size) * ChunkEntity.Size;
            var chunkZ = MathF.Floor(point.Z / ChunkEntity.Size) * ChunkEntity.Size;
            var chunkPos = new SysVector3(chunkX, 0f, chunkZ);

            if (_chunks.TryGetValue(chunkPos, out var chunk) && chunk.Chunk != null)
            {
                var relativePos = point - chunk.Position;

                var blockX = (int)MathF.Floor(relativePos.X);
                var blockY = (int)MathF.Floor(relativePos.Y);
                var blockZ = (int)MathF.Floor(relativePos.Z);

                if (blockX >= 0 && blockX < ChunkEntity.Size &&
                    blockY >= 0 && blockY < ChunkEntity.Height &&
                    blockZ >= 0 && blockZ < ChunkEntity.Size)
                {
                    var block = chunk.Chunk.GetBlock(blockX, blockY, blockZ);

                    if (block != null && block.BlockType != BlockType.Air)
                    {
                        return (chunk, blockX, blockY, blockZ);
                    }
                }
            }

            currentDistance += step;
        }

        return null;
    }

    /// <summary>
    /// Spawns particles at the specified position.
    /// </summary>
    /// <param name="position">The position to spawn particles at.</param>
    /// <param name="count">The number of particles to spawn.</param>
    /// <param name="spread">The spread of the particles.</param>
    /// <param name="speed">The speed of the particles.</param>
    /// <param name="lifeTime">The lifetime of the particles.</param>
    /// <param name="color">The color of the particles.</param>
    public void SpawnParticles(
        XnaVector3 position, int count, float spread = 1f, float speed = 5f, float lifeTime = 2f, Color? color = null
    )
    {
        _particleGameObject.SpawnParticles(position, count, spread, speed, lifeTime, color);
    }

    /// <summary>
    /// Invalidates the geometry of the specified chunk and adjacent chunks affected by the block change.
    /// </summary>
    /// <param name="chunk">The chunk containing the block.</param>
    /// <param name="blockX">The X coordinate of the block.</param>
    /// <param name="blockY">The Y coordinate of the block.</param>
    /// <param name="blockZ">The Z coordinate of the block.</param>
    public void InvalidateBlockAndAdjacentChunks(ChunkGameObject chunk, int blockX, int blockY, int blockZ)
    {
        if (chunk == null || chunk.Chunk == null)
            return;

        var affectedChunks = new HashSet<ChunkGameObject> { chunk };

        // Check if the block is on the edge of the chunk and add adjacent chunks
        var chunkSize = ChunkEntity.Size;

        // Check each face of the block
        var neighborOffsets = new[]
        {
            (1, 0, 0),  // East
            (-1, 0, 0), // West
            (0, 1, 0),  // Up
            (0, -1, 0), // Down
            (0, 0, 1),  // North
            (0, 0, -1)  // South
        };

        foreach (var (dx, dy, dz) in neighborOffsets)
        {
            var neighborX = blockX + dx;
            var neighborY = blockY + dy;
            var neighborZ = blockZ + dz;

            // If neighbor is outside current chunk bounds, find the adjacent chunk
            if (neighborX < 0 || neighborX >= chunkSize ||
                neighborY < 0 || neighborY >= ChunkEntity.Height ||
                neighborZ < 0 || neighborZ >= chunkSize)
            {
                // Calculate which chunk the neighbor block belongs to
                var worldX = chunk.Chunk.Position.X + neighborX;
                var worldY = chunk.Chunk.Position.Y + neighborY;
                var worldZ = chunk.Chunk.Position.Z + neighborZ;

                var neighborChunkX = (int)MathF.Floor(worldX / chunkSize) * chunkSize;
                var neighborChunkZ = (int)MathF.Floor(worldZ / chunkSize) * chunkSize;

                var neighborChunkPos = new SysVector3(neighborChunkX, 0, neighborChunkZ);

                if (_chunks.TryGetValue(neighborChunkPos, out var neighborChunk))
                {
                    affectedChunks.Add(neighborChunk);
                }
            }
        }

        // Invalidate all affected chunks
        foreach (var affectedChunk in affectedChunks)
        {
            affectedChunk.InvalidateGeometry();
            _meshBuildQueue.Enqueue(affectedChunk);

            _logger.Debug("Chunk invalidated at {Position}", affectedChunk.Position);
        }

        // Recalculate cross-chunk lighting for all affected chunks
        if (affectedChunks.Any())
        {
            var affectedChunkEntities = affectedChunks
                .Where(c => c.Chunk != null)
                .Select(c => c.Chunk!)
                .ToList();

            _lightSystem.CalculateCrossChunkLighting(affectedChunkEntities, GetChunkEntityForLighting);

            _logger.Debug("Cross-chunk lighting recalculated for {Count} chunks", affectedChunks.Count);
        }

        QueueWaterUpdatesAroundBlock(chunk.Chunk, blockX, blockY, blockZ);
    }

    /// <summary>
    /// Queues water updates around the specified block.
    /// </summary>
    /// <param name="chunk">The chunk containing the block.</param>
    /// <param name="x">The X coordinate of the block.</param>
    /// <param name="y">The Y coordinate of the block.</param>
    /// <param name="z">The Z coordinate of the block.</param>
    public void QueueWaterUpdatesAroundBlock(ChunkEntity chunk, int x, int y, int z)
    {
        _waterSystem.QueueWaterUpdate(chunk, x, y - 1, z);
        _waterSystem.QueueWaterUpdate(chunk, x + 1, y, z);
        _waterSystem.QueueWaterUpdate(chunk, x - 1, y, z);
        _waterSystem.QueueWaterUpdate(chunk, x, y, z + 1);
        _waterSystem.QueueWaterUpdate(chunk, x, y, z - 1);
        _waterSystem.QueueWaterUpdate(chunk, x, y + 1, z);
    }

    private ChunkEntity? GetChunkEntityForLighting(int chunkX, int chunkZ)
    {
        var chunkPos = new SysVector3(chunkX * ChunkEntity.Size, 0f, chunkZ * ChunkEntity.Size);
        return GetChunkEntity(new XnaVector3(chunkPos.X, chunkPos.Y, chunkPos.Z));
    }

    /// <summary>
    /// Invalidates the geometry of the specified chunk and queues it for rebuild.
    /// </summary>
    /// <param name="chunk">The chunk to invalidate.</param>
    public void InvalidateChunkGeometry(ChunkGameObject chunk)
    {
        if (chunk != null)
        {
            chunk.InvalidateGeometry();
            _meshBuildQueue.Enqueue(chunk);
            _logger.Debug("Chunk geometry invalidated and queued for rebuild at {Position}", chunk.Position);

            // Also invalidate neighboring chunks for proper face culling and lighting
            var affectedChunks = new HashSet<ChunkGameObject> { chunk };
            InvalidateNeighborChunks(chunk, affectedChunks);

            // Recalculate cross-chunk lighting for all affected chunks
            var affectedChunkEntities = affectedChunks
                .Where(c => c.Chunk != null)
                .Select(c => c.Chunk!)
                .ToList();

            _lightSystem.CalculateCrossChunkLighting(affectedChunkEntities, GetChunkEntityForLighting);
            _logger.Debug("Cross-chunk lighting recalculated for {Count} chunks", affectedChunks.Count);
        }
    }

    private void InvalidateNeighborChunks(ChunkGameObject chunk, HashSet<ChunkGameObject> affectedChunks)
    {
        var chunkPos = new SysVector3(chunk.Position.X, chunk.Position.Y, chunk.Position.Z);
        var chunkSize = (float)ChunkEntity.Size;

        // Check all 4 neighboring positions (horizontal neighbors for lighting)
        var neighborOffsets = new[]
        {
            new SysVector3(chunkSize, 0, 0),  // East
            new SysVector3(-chunkSize, 0, 0), // West
            new SysVector3(0, 0, chunkSize),  // North
            new SysVector3(0, 0, -chunkSize)  // South
        };

        foreach (var offset in neighborOffsets)
        {
            var neighborPos = chunkPos + offset;
            if (_chunks.TryGetValue(neighborPos, out var neighborChunk))
            {
                if (affectedChunks.Add(neighborChunk))
                {
                    neighborChunk.InvalidateGeometry();
                    _meshBuildQueue.Enqueue(neighborChunk);
                    _logger.Debug("Neighbor chunk invalidated at {Position}", neighborPos);
                }
            }
        }
    }

    /// <summary>
    /// Draws all visible chunks and particles.
    /// </summary>
    /// <param name="gameTime">The game time information.</param>
    protected override void OnDraw3d(GameTime gameTime)
    {
        if (EnableFrustumCulling)
        {
            _frustum = new BoundingFrustum(Camera.View * Camera.Projection);
        }

        var cameraPosition = Camera.Position;
        var visibleChunks = 0;
        var culledChunks = 0;

        RasterizerState? previousRasterizerState = null;
        if (EnableWireframe)
        {
            previousRasterizerState = SquidVoxEngineContext.GraphicsDevice.RasterizerState;
            SquidVoxEngineContext.GraphicsDevice.RasterizerState = new RasterizerState
            {
                FillMode = FillMode.WireFrame,
                CullMode = CullMode.None
            };
        }

        foreach (var chunk in _chunks.Values)
        {
            if (ShouldRenderChunk(chunk, cameraPosition))
            {
                DrawChunk(chunk, gameTime);
                visibleChunks++;
            }
            else
            {
                culledChunks++;
            }
        }

        if (EnableWireframe && previousRasterizerState != null)
        {
            SquidVoxEngineContext.GraphicsDevice.RasterizerState = previousRasterizerState;
        }

        if (culledChunks > 0)
        {
            _logger.Verbose("Rendered {Visible} chunks, culled {Culled} chunks", visibleChunks, culledChunks);
        }

        _particleGameObject.Draw3d(gameTime);
    }

    private bool ShouldRenderChunk(ChunkGameObject chunk, XnaVector3 cameraPosition)
    {
        if (chunk.Chunk == null)
        {
            return false;
        }

        var chunkPos = chunk.Position;
        var chunkCenter = new XnaVector3(
            chunkPos.X + ChunkEntity.Size * 0.5f,
            chunkPos.Y + ChunkEntity.Height * 0.5f,
            chunkPos.Z + ChunkEntity.Size * 0.5f
        );

        var distance = XnaVector3.Distance(cameraPosition, chunkCenter);
        if (distance > ViewRange)
        {
            return false;
        }

        if (EnableFrustumCulling && _frustum != null)
        {
            var chunkRadius = MathF.Sqrt(
                ChunkEntity.Size * ChunkEntity.Size +
                ChunkEntity.Height * ChunkEntity.Height +
                ChunkEntity.Size * ChunkEntity.Size
            ) * 0.5f;

            var chunkSphere = new BoundingSphere(chunkCenter, chunkRadius);

            if (_frustum.Contains(chunkSphere) == ContainmentType.Disjoint)
            {
                return false;
            }
        }

        return true;
    }

    private void DrawChunk(ChunkGameObject chunk, GameTime gameTime)
    {
        if (chunk.Chunk == null)
        {
            return;
        }

        chunk.TextureEnabled = !EnableWireframe;
        chunk.AmbientLight = AmbientLight;
        chunk.LightDirection = LightDirection;
        chunk.DrawWithCamera(gameTime, Camera.View, Camera.Projection);
    }

    private void ProcessPendingChunks()
    {
        while (_pendingChunks.TryDequeue(out var pending))
        {
            var (position, chunk) = pending;

            if (_chunks.ContainsKey(position))
            {
                _logger.Warning("Chunk at position {Position} already exists, skipping", position);
                continue;
            }

            var chunkComponent = new ChunkGameObject
            {
                AutoRotate = false,
                BlockScale = 1f,
                RenderTransparentBlocks = true,
                EnableFadeIn = false,
                GetNeighborChunk = GetChunkEntity,
                FogEnabled = FogEnabled,
                FogColor = FogColor,
                FogStart = FogStart,
                FogEnd = FogEnd,
                UseGreedyMeshing = UseGreedyMeshing
            };

            chunkComponent.SetChunk(chunk);

            // Calculate initial lighting for the new chunk
            // For now, use single-chunk lighting for new chunks
            _lightSystem.CalculateInitialSunlight(chunk);

            if (_chunks.TryAdd(position, chunkComponent))
            {
                _meshBuildQueue.Enqueue(chunkComponent);
                _logger.Information("Chunk added at position {Position}, queued for mesh build", position);
            }
            else
            {
                _logger.Warning("Failed to add chunk at position {Position}", position);
                chunkComponent.Dispose();
            }
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        ClearChunks();
    }
}
