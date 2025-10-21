using System.Text.Json.Serialization;
using SquidVox.Voxel.Data.Entities;

namespace SquidVox.Voxel.Json;


[JsonSerializable(typeof(BlockDefinitionData))]
[JsonSerializable(typeof(BlockDefinitionData[]))]
public partial class SquidVoxVoxelJsonContext : JsonSerializerContext
{

}
