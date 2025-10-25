using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SquidVox.Voxel.Primitives;
using SquidVox.Voxel.Types;

namespace SquidVox.Voxel.GameObjects;

/// <summary>
/// Manages lighting calculations and 3D light texture for a chunk.
/// </summary>
public sealed class ChunkLightingManager : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private Texture3D? _lightTexture;
    private Color[]? _lightData;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChunkLightingManager"/> class.
    /// </summary>
    /// <param name="graphicsDevice">Graphics device used for creating textures.</param>
    public ChunkLightingManager(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
    }

    /// <summary>
    /// Gets the 3D light texture for this chunk.
    /// </summary>
    public Texture3D? LightTexture => _lightTexture;

    /// <summary>
    /// Updates the 3D light texture from the chunk's light data.
    /// </summary>
    /// <param name="chunk">The chunk containing light data.</param>
    public void UpdateLightTexture(ChunkEntity chunk)
    {
        if (chunk == null)
        {
            return;
        }

        var expectedCount = ChunkEntity.Size * ChunkEntity.Height * ChunkEntity.Size;

        if (_lightTexture == null ||
            _lightTexture.Width != ChunkEntity.Size ||
            _lightTexture.Height != ChunkEntity.Height ||
            _lightTexture.Depth != ChunkEntity.Size)
        {
            _lightTexture?.Dispose();
            _lightTexture = new Texture3D(
                _graphicsDevice,
                ChunkEntity.Size,
                ChunkEntity.Height,
                ChunkEntity.Size,
                false,
                SurfaceFormat.Color
            );

            _lightData = new Color[expectedCount];
        }

        if (_lightData == null || _lightData.Length != expectedCount)
        {
            _lightData = new Color[expectedCount];
        }

        var data = _lightData;
        if (data == null)
        {
            return;
        }

        var lightLevels = chunk.LightLevels;
        for (int i = 0; i < lightLevels.Length && i < data.Length; i++)
        {
            float normalized = Math.Clamp(lightLevels[i] / 15f, 0f, 1f);
            byte value = (byte)(normalized * 255f);
            data[i] = new Color(value, value, value, (byte)255);
        }

        _lightTexture!.SetData(data);
    }

    /// <summary>
    /// Calculates the color for a block face including lighting and ambient occlusion.
    /// For each face, samples the light level from the ADJACENT BLOCK (not the block itself).
    /// This ensures faces receive light from the exposed side, not from solid blocks.
    /// </summary>
    /// <param name="chunk">The chunk containing the block.</param>
    /// <param name="x">Local X coordinate.</param>
    /// <param name="y">Local Y coordinate.</param>
    /// <param name="z">Local Z coordinate.</param>
    /// <param name="side">The side of the block.</param>
    /// <returns>The calculated face color.</returns>
    public static Color CalculateFaceColor(ChunkEntity? chunk, int x, int y, int z, BlockSide side)
    {
        var ambientOcclusion = 1.0f;

        switch (side)
        {
            case BlockSide.Top:
                ambientOcclusion = 1.0f;
                break;
            case BlockSide.Bottom:
                ambientOcclusion = 0.5f;
                break;
            case BlockSide.North:
            case BlockSide.South:
                ambientOcclusion = 0.8f;
                break;
            case BlockSide.East:
            case BlockSide.West:
                ambientOcclusion = 0.75f;
                break;
        }

        var lightLevel = 0.2f;
        var lightColor = Vector3.One;

        if (chunk != null)
        {
            // Get the adjacent block in the direction of the face
            // This ensures we sample light from the exposed side, not the solid block itself
            int adjX = x;
            int adjY = y;
            int adjZ = z;

            switch (side)
            {
                case BlockSide.Top:
                    adjY += 1;
                    break;
                case BlockSide.Bottom:
                    adjY -= 1;
                    break;
                case BlockSide.North:
                    adjZ -= 1;
                    break;
                case BlockSide.South:
                    adjZ += 1;
                    break;
                case BlockSide.East:
                    adjX += 1;
                    break;
                case BlockSide.West:
                    adjX -= 1;
                    break;
            }

            var sampleX = adjX;
            var sampleY = adjY;
            var sampleZ = adjZ;

            // Check if adjacent block is out of bounds in a way that means "exposed to sky/void"
            bool isOutOfBoundsAbove = (adjY >= ChunkEntity.Height); // Top face exposed to sky
            bool isOutOfBoundsBelow = (adjY < 0); // Bottom face exposed to void
            bool isOutOfBoundsSide = (adjX < 0 || adjX >= ChunkEntity.Size || adjZ < 0 || adjZ >= ChunkEntity.Size);

            var hasSample = false;
            if (isOutOfBoundsAbove)
            {
                // Top faces exposed to sky = full light from above
                lightLevel = 1.0f;
                hasSample = true;
            }
            else if (chunk.IsInBounds(adjX, adjY, adjZ))
            {
                // Adjacent block is in bounds - sample its light
                var adjacentBlock = chunk.GetBlock(adjX, adjY, adjZ);
                if (adjacentBlock.BlockType != BlockType.Air)
                {
                    lightColor = adjacentBlock.LightColor;
                    var rawLight = chunk.GetLightLevel(adjX, adjY, adjZ);
                    lightLevel = Math.Max(0.2f, rawLight / 15f);
                    hasSample = true;
                }
            }

            // If still no sample, fall back to the source block itself
            if (!hasSample && chunk.IsInBounds(x, y, z))
            {
                sampleX = x;
                sampleY = y;
                sampleZ = z;
                var block = chunk.GetBlock(sampleX, sampleY, sampleZ);
                lightColor = block.LightColor;
                var rawLight = chunk.GetLightLevel(sampleX, sampleY, sampleZ);
                lightLevel = Math.Max(0.2f, rawLight / 15f);
            }
        }

        var finalBrightness = ambientOcclusion * lightLevel;
        finalBrightness = Math.Max(0.1f, finalBrightness);

        var colorR = (byte)(finalBrightness * lightColor.X * 255);
        var colorG = (byte)(finalBrightness * lightColor.Y * 255);
        var colorB = (byte)(finalBrightness * lightColor.Z * 255);

        return new Color(colorR, colorG, colorB, (byte)255);
    }

    /// <summary>
    /// Disposes the light texture and data.
    /// </summary>
    public void Dispose()
    {
        _lightTexture?.Dispose();
        _lightTexture = null;
        _lightData = null;
    }
}
