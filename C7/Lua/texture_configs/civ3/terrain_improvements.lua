local terrain_improvements = {}

local TERRAIN = "Art/Terrain/"

--[[
TerrainBuildings.pcx contains multiple pieces of art in a grid, with each
item being 128x64 pixesl.

The basic version is:
  Fortress (ancient)     | Colony (an)   | Barb camp
  Fortress (medieval)    | Colony (me)   | Mine
  Fortress (industrial)  | Colony (in)   | Empty
  Fortress (modern)      | Colony (mo)   | Empty
--]]
local function make_entry(x, y)
  return {
    path = TERRAIN .. "TerrainBuildings.pcx",
    crop_region = { 128 * x, 64 * y, 128, 64 },
    shadows = false,
  }
end

terrain_improvements = {
  mine = make_entry(2, 1),
  fortress = make_entry(0, 0),
  barricade = make_entry(3, 0),
}

terrain_improvements.irrigation = {
  grass = TERRAIN .. "irrigation.pcx",
  desert = TERRAIN .. "irrigation DESETT.pcx",
  plains = TERRAIN .. "irrigation PLAINS.pcx",
  tundra = TERRAIN .. "irrigation TUNDRA.pcx",
}

terrain_improvements.railroad = TERRAIN .. "railroads.pcx"

terrain_improvements.road = TERRAIN .. "roads.pcx"

return terrain_improvements
