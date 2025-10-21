using System.Text.Json.Serialization;
using SquidVox.Voxel.Data.Entities;
using SquidVox.Voxel.Types;

namespace SquidVox.Voxel.Json;

[JsonSerializable(typeof(BlockDefinitionData))]
[JsonSerializable(typeof(BlockDefinitionData[]))]
[JsonConverter(typeof(JsonStringEnumConverter<BlockType>))]
public partial class SquidVoxVoxelJsonContext : JsonSerializerContext
{
}
