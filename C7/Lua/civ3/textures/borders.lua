local texture_width = 128
local texture_height = 72

--[[
The PCX image contains 4 rows and 2 columns:
Each row corresponds to a border direction.
The first column contains border textures for regular terrain.
The second column contains border textures for hills and mountains
-]]
local borders = { extra_data = { path = "Art/Terrain/Territory.pcx" } }

function borders:find_column(border_config)
  local column = 0
  if border_config.isHilly then
    column = 1
  end
  return column
end

function borders:find_row(border_config)
  local direction = border_config.direction
  local direction_to_row = {
    [direction.NORTHWEST] = 0,
    [direction.NORTHEAST] = 1,
    [direction.SOUTHWEST] = 2,
    [direction.SOUTHEAST] = 3,
  }
  return direction_to_row[direction]
end

function borders:map_object_to_sprite(border_config)
  local column = self:find_column(border_config)
  local row = self:find_row(border_config)

  return {
    path = self.extra_data.path,
    transparent_color_indexes = { 1, 254, 255 },
    crop_region = { column * texture_width, row * texture_height, texture_width, texture_height },
  }
end

return borders
