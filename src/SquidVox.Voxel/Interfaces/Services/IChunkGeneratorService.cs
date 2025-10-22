using Microsoft.Xna.Framework;
using SquidVox.Voxel.Interfaces.Generation.Pipeline;
using SquidVox.Voxel.Primitives;

namespace SquidVox.Voxel.Interfaces.Services;

public interface IChunkGeneratorService
{

    int Seed { get; set; }
    Task<ChunkEntity> GetChunkByWorldPosition(Vector3 position);

    Task<IEnumerable<ChunkEntity>> GetChunksByPositions(IEnumerable<Vector3> positions);

    Task GenerateInitialChunksAsync();

    void AddGeneratorStep(IGeneratorStep generationStep);

    void ClearCache();

    bool RemoveGeneratorStep(string stepName);

}
