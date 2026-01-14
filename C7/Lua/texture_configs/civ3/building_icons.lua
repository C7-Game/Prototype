local SMALL_ICON_WIDTH = 32
local SMALL_ICON_HEIGHT = 32
local LARGE_ICON_WIDTH = 50
local LARGE_ICON_HEIGHT = 40

local building_icons = {
  small = {
    extra_data = {
      path = "Art/city screen/buildings-small.pcx",
    },
  },
  large = {
    extra_data = {
	  path = "Art/city screen/buildings-large.pcx",
	}
  }
}

function building_icons.small:map_object_to_sprite(building)
  if (building:GetType().Name ~= "Building") then
    error "Expected a Building object"
  end

  local y = 1 + 33 * (1 + building.iconRowIndex)

  return {
    path = self.extra_data.path,
    crop_region = { 33, y, SMALL_ICON_WIDTH, SMALL_ICON_HEIGHT },
  }
end

function building_icons.large:map_object_to_sprite(building)
  if (building:GetType().Name ~= "Building") then
    error "Expected a Building object"
  end

  local y = 33 + (LARGE_ICON_HEIGHT + 1) * building.iconRowIndex

  return {
    path = self.extra_data.path,
    crop_region = { 33, y, LARGE_ICON_WIDTH, LARGE_ICON_HEIGHT },
  }
end

return building_icons
