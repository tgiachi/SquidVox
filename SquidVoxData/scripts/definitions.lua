---@meta

---
--- SquidCraft v0.0.1 Lua API
--- Auto-generated on 2025-10-21 15:27:20
---

--- Global constants

---
--- VERSION constant
--- Value: "0.0.1"
---
---@type string
VERSION = "0.0.1"

---
--- ENGINE constant
--- Value: "SquidVox"
---
---@type string
ENGINE = "SquidVox"

---
--- PLATFORM constant
--- Value: "Unix"
---
---@type string
PLATFORM = "Unix"


---
--- ConsoleModule module
---
--- Console API for logging and debugging
---
---@class console
console.log = function() end
console.info = function() end
console.warn = function() end
console.error = function() end
console.debug = function() end
console.trace = function() end
console.clear = function() end
console.assert = function() end
console = {}

---
--- No description available
---
---@param args any[] The args table
function console.log(args) end

---
--- No description available
---
---@param args any[] The args table
function console.info(args) end

---
--- No description available
---
---@param args any[] The args table
function console.warn(args) end

---
--- No description available
---
---@param args any[] The args table
function console.error(args) end

---
--- No description available
---
---@param args any[] The args table
function console.debug(args) end

---
--- No description available
---
---@param args any[] The args table
function console.trace(args) end

---
--- No description available
---
function console.clear() end

---
--- No description available
---
---@param condition boolean The condition flag
---@param args any[] The args table
function console.assert(condition, args) end

---
--- WindowModule module
---
--- Provides functions to create and manage in-game windows.
---
---@class window
window.set_title = function() end
window.set_size = function() end
window.get_size = function() end
window.get_title = function() end
window = {}

---
--- Sets the title of the game window.
---
---@param title string The title text
function window.set_title(title) end

---
--- Sets the size of the game window.
---
---@param width number The width value
---@param height number The height value
function window.set_size(width, height) end

---
--- Gets the current size of the game window.
---
---@return Vector2 The result as Vector2
function window.get_size() end

---
--- Gets the current title of the game window.
---
---@return string The resulting text
function window.get_title() end

---
--- ImGuiModule module
---
--- ImGui Module for integrating ImGui functionality.
---
---@class imgui
imgui.show_demo_window = function() end
imgui.hide_demo_window = function() end
imgui.create_debugger_obj = function() end
imgui.add_debugger = function() end
imgui = {}

---
--- Shows the ImGui demo window.
---
function imgui.show_demo_window() end

---
--- Hides the ImGui demo window.
---
function imgui.hide_demo_window() end

---
--- Creates a new ImGui debugger window with the specified title and callback.
---
---@param window_title string The windowtitle text
---@param call_back function The callback of type function
---@return LuaImGuiDebuggerObject The result as LuaImGuiDebuggerObject
function imgui.create_debugger_obj(window_title, call_back) end

---
--- Adds an ImGui debugger window to the render layer.
---
---@param debugger LuaImGuiDebuggerObject The debugger of type LuaImGuiDebuggerObject
function imgui.add_debugger(debugger) end

---
--- AssetManagerModule module
---
--- Provides asset management functionalities.
---
---@class assets
assets.load_font = function() end
assets.load_texture = function() end
assets.load_atlas = function() end
assets = {}

---
--- Loads a font from a file.
---
---@param name string The name text
---@param filename string The filename text
function assets.load_font(name, filename) end

---
--- Loads a texture from a file.
---
---@param filename string The filename text
---@param name string The name text
function assets.load_texture(filename, name) end

---
--- Loads a texture atlas from a file.
---
---@param filename string The filename text
---@param name string The name text
---@param tile_width number The tilewidth value
---@param tile_height number The tileheight value
---@param spacing number The spacing value (optional)
---@param margin number The margin value (optional)
function assets.load_atlas(filename, name, tile_width, tile_height, spacing, margin) end

---
--- InputManagerModule module
---
--- Input Manager Module
---
---@class input_manager
input_manager.bind_key = function() end
input_manager.bind_key_context = function() end
input_manager.unbind_key = function() end
input_manager.clear_bindings = function() end
input_manager.set_context = function() end
input_manager.get_context = function() end
input_manager.is_key_down = function() end
input_manager.is_key_pressed = function() end
input_manager = {}

---
--- Binds a key to a callback action.
---
---@param key_binding string The keybinding text
---@param callback Closure The callback of type Closure
function input_manager.bind_key(key_binding, callback) end

---
--- Binds a key to a callback action with a specific context.
---
---@param key_binding string The keybinding text
---@param callback DynValue The callback of type DynValue
---@param context_name string The contextname text
function input_manager.bind_key_context(key_binding, callback, context_name) end

---
--- Unbinds a key.
---
---@param key_binding string The keybinding text
function input_manager.unbind_key(key_binding) end

---
--- Clears all key bindings.
---
function input_manager.clear_bindings() end

---
--- Sets the current input context.
---
---@param context_name string The contextname text
function input_manager.set_context(context_name) end

---
--- Gets the current input context.
---
---@return string The resulting text
function input_manager.get_context() end

---
--- Checks if a key is currently down.
---
---@param key_name string The keyname text
---@return boolean The result of the operation
function input_manager.is_key_down(key_name) end

---
--- Checks if a key was just pressed.
---
---@param key_name string The keyname text
---@return boolean The result of the operation
function input_manager.is_key_pressed(key_name) end

---
--- RenderLayerModule module
---
--- Global Render Layer Module
---
---@class global_render_layer
global_render_layer.get_2d_render_layer = function() end
global_render_layer = {}

---
--- Gets the 2D render layer.
---
---@return GameObjectRenderLayer The result as GameObjectRenderLayer
function global_render_layer.get_2d_render_layer() end

---
--- BlockManagerModule module
---
--- Module for managing voxel blocks.
---
---@class block_manager
block_manager.new_definition = function() end
block_manager.register_block = function() end
block_manager.from_json = function() end
block_manager = {}

---
--- Creates a new block definition data instance.
---
---@return BlockDefinitionData The result as BlockDefinitionData
function block_manager.new_definition() end

---
--- Registers a new block definition.
---
---@param block_definition BlockDefinitionData The blockdefinition of type BlockDefinitionData
function block_manager.register_block(block_definition) end

---
--- Loads block definitions from a JSON file.
---
---@param file_name string The filename text
function block_manager.from_json(file_name) end


---
--- Enum SquidVox.Voxel.Types.BlockType
---
---@class block_type
---@field Air number # Value: 0
---@field Dirt number # Value: 1
---@field Grass number # Value: 2
---@field Stone number # Value: 3
---@field Water number # Value: 4
---@field Sand number # Value: 5
---@field Wood number # Value: 6
---@field Leaves number # Value: 7
---@field Glass number # Value: 8
---@field Brick number # Value: 9
---@field Bedrock number # Value: 10
---@field Snow number # Value: 11
---@field Lava number # Value: 12
---@field Flower number # Value: 13
---@field TallGrass number # Value: 14

block_type = {}


---
--- Enum SquidVox.Voxel.Types.BlockSide
---
---@class block_side
---@field Top number # Value: 0
---@field Bottom number # Value: 1
---@field North number # Value: 2
---@field South number # Value: 3
---@field East number # Value: 4
---@field West number # Value: 5

block_side = {}


---
--- Enum SquidVox.Core.Enums.RenderLayer
---
---@class render_layer
---@field Background number # Value: 0
---@field World3D number # Value: 50
---@field World2D number # Value: 100
---@field Effects number # Value: 150
---@field GameUI number # Value: 200
---@field Overlay number # Value: 800
---@field DebugUI number # Value: 900

render_layer = {}


---
--- Enum MoonSharp.Interpreter.DataType
---
---@class data_type
---@field Nil number # Value: 0
---@field Void number # Value: 1
---@field Boolean number # Value: 2
---@field Number number # Value: 3
---@field String number # Value: 4
---@field Function number # Value: 5
---@field Table number # Value: 6
---@field Tuple number # Value: 7
---@field UserData number # Value: 8
---@field Thread number # Value: 9
---@field ClrFunction number # Value: 10
---@field TailCallRequest number # Value: 11
---@field YieldRequest number # Value: 12

data_type = {}



---
--- Class SquidVox.Voxel.Data.Entities.BlockDefinitionData
---
---@class BlockDefinitionData
---@field block_type block_type
---@field sides table<block_side, string>
---@field is_transparent boolean
---@field is_liquid boolean
---@field is_solid boolean
---@field is_billboard boolean
---@field is_windable boolean
---@field wind_speed number
---@field height number


---
--- Class SquidVox.World3d.Rendering.GameObjectRenderLayer
---
---@class GameObjectRenderLayer
---@field layer render_layer
---@field enabled boolean
---@field has_focus boolean


---
--- Class MoonSharp.Interpreter.DynValue
---
---@class DynValue
---@field reference_id number
---@field type data_type
---@field function Closure
---@field number number
---@field tuple DynValue[]
---@field coroutine Coroutine
---@field table Table
---@field boolean boolean
---@field string string
---@field callback CallbackFunction
---@field tail_call_data TailCallData
---@field yield_request YieldRequest
---@field user_data UserData
---@field read_only boolean


---
--- Class MoonSharp.Interpreter.Closure
---
---@class Closure
---@field entry_point_byte_code_location number
---@field owner_script Script
---@field reference_id number


---
--- Class SquidVox.World3d.Scripts.LuaImGuiDebuggerObject
---
---@class LuaImGuiDebuggerObject
---@field window_title string


---
--- Class Microsoft.Xna.Framework.Vector2
---
---@class Vector2

