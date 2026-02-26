local ICON_WIDTH = 32
local ICON_HEIGHT = 32
local ICONS_PER_ROW = 14

local unit_icons = {
  extra_data = {
    path = "Art/Units/units_32.pcx",
  },
}

function unit_icons:map_object_to_sprite(unit_prototype)
  if (unit_prototype:GetType().Name ~= "UnitPrototype") then
    error "Expected a UnitPrototype object"
  end

  local x = 1 + (ICON_WIDTH + 1) * (unit_prototype.iconIndex % ICONS_PER_ROW)
  local y = 1 + (ICON_HEIGHT + 1) * math.floor(unit_prototype.iconIndex / ICONS_PER_ROW)

  return {
    path = self.extra_data.path,
    crop_region = { x, y, ICON_WIDTH, ICON_HEIGHT },
  }
end

return unit_icons
