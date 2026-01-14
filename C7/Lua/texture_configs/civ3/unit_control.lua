local INTERFACE = "Art/interface/"

local function make_entry(x, y)
  local entry = {
    normal = {
      path = INTERFACE .. "NormButtons.PCX",
      alpha = INTERFACE .. "ButtonAlpha.pcx",
      crop_region = { x * 32, y * 32, 32, 32 },
    },
    hover = {
      path = INTERFACE .. "rolloverbuttons.PCX",
      alpha = INTERFACE .. "ButtonAlpha.pcx",
      crop_region = { x * 32, y * 32, 32, 32 },
    },
    pressed = {
      path = INTERFACE .. "highlightedbuttons.PCX",
      alpha = INTERFACE .. "ButtonAlpha.pcx",
      crop_region = { x * 32, y * 32, 32, 32 },
    },
  }

  return entry
end

local unit_control = {
  unit_hold = make_entry(0, 0),
  unit_wait = make_entry(1, 0),
  unit_fortify = make_entry(2, 0),
  unit_disband = make_entry(3, 0),
  unit_goto = make_entry(4, 0),
  unit_explore = make_entry(5, 0),

  unit_build_city = make_entry(5, 2),
  unit_build_road = make_entry(6, 2),
  unit_build_railroad = make_entry(7, 2),

  unit_build_fortress = make_entry(0, 3),
  unit_build_mine = make_entry(1, 3),
  unit_irrigate = make_entry(2, 3),
  unit_clear_forest = make_entry(3, 3),
  unit_clear_wetlands = make_entry(4, 3),
  unit_plant_forest = make_entry(5, 3),
  unit_clear_damage = make_entry(6, 3),
  unit_automate = make_entry(7, 3),
  unit_build_airfield = make_entry(1, 4),
  unit_build_radar_tower = make_entry(2, 4),
  unit_build_outpost = make_entry(3, 4),
  unit_build_barricade = make_entry(4, 4),

  movement_indicators = {
    path = INTERFACE .. "MovementLED.PCX",
    crop_region = { 0, 0, 36, 8 },
  },
}

return unit_control
