using SquidVox.Core.Attributes.Scripts;
using SquidVox.Core.Data.Directories;
using SquidVox.Core.Json;
using SquidVox.Voxel.Data.Entities;
using SquidVox.Voxel.Interfaces;
using SquidVox.Voxel.Interfaces.Services;
using SquidVox.Voxel.Json;

namespace SquidVox.Voxel.Modules;

[ScriptModule("block_manager", "Module for managing voxel blocks.")]
public class BlockManagerModule
{
    private readonly IBlockManagerService _blockManagerService;
    private readonly DirectoriesConfig _directoriesConfig;

    public BlockManagerModule(IBlockManagerService blockManagerService, DirectoriesConfig directoriesConfig)
    {
        _blockManagerService = blockManagerService;
        _directoriesConfig = directoriesConfig;
    }

    [ScriptFunction("new_definition", "Creates a new block definition data instance.")]
    public BlockDefinitionData CreateNew()
    {
        return new BlockDefinitionData();
    }

    [ScriptFunction("register_block", "Registers a new block definition.")]
    public void RegisterBlock(BlockDefinitionData blockDefinition)
    {
        _blockManagerService.AddBlockDefinition(blockDefinition);
    }

    [ScriptFunction("from_json", "Loads block definitions from a JSON file.")]
    public void LoadFromJson(string fileName)
    {
        var path = Path.Combine(_directoriesConfig.Root, fileName);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Block definition file not found: {path}");
        }

        var jsonObject = JsonUtils.DeserializeFromFile<BlockDefinitionData[]>(path);

        foreach (var blockDefinition in jsonObject)
        {
            _blockManagerService.AddBlockDefinition(blockDefinition);
        }
    }
}
