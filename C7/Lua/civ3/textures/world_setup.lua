local WORLD_SETUP = "Art/WorldSetup/"

local landmass_types = { "pangaea", "continents", "archipelago" }

local world_settings = {
  climate = { "arid", "normal", "wet" },
  temperature = { "warm", "temperate", "cool" },
  age = { "billion3", "billion4", "billion5" },
}

local world_setup = {
  background = WORLD_SETUP .. "background.pcx",
}

local function make_entry(path, x, y)
  local entry = {
    path = WORLD_SETUP .. path,
    crop_region = { x, y, 75, 50 },
    shadows = false,
  }

  return entry
end

-- Textures for landmass type contols
local column_idx = 0
for _, percent in ipairs { 80, 70, 60 } do
  for _, landmass in ipairs(landmass_types) do
    local key = landmass .. percent
    local x = 76 * column_idx + 1
    local y = 1

    column_idx = column_idx + 1

    world_setup[key] = {
      normal = make_entry("landmassWaterSMALL.pcx", x, y),
      hover = make_entry("landmassWaterSMALLrollovers.pcx", x, y),
      pressed = make_entry("landmassWaterSMALLdepress.pcx", x, y),
    }
  end
end

-- Textures for climate, temperature and age contols
local buttons_state_ys = {
  climate = 1,
  temperature = 124,
  age = 281,
}

for category, keys in pairs(world_settings) do
  for column_idx, key in ipairs(keys) do
    local x = 76 * (column_idx - 1) + 1

    local y = 339
    local y_button_states = buttons_state_ys[category]

    world_setup[key] = {
      normal = make_entry(category .. ".pcx", x, y),
      hover = make_entry("CLIMTEMPAGERollovers.pcx", x, y_button_states),
      pressed = make_entry("CLIMTEMPAGEDepress.pcx", x, y_button_states),
    }
  end
end

world_setup.large = {}

local function make_large_entry(path, x, y)
  return {
    path = WORLD_SETUP .. path,
    crop_region = { x, y, 300, 200 },
    shadows = false,
  }
end

local x_step = 301
local landmass_y = {
  [80] = 1,
  [70] = 276,
  [60] = 551,
}
for column, landmass in ipairs(landmass_types) do
  for percent, y in pairs(landmass_y) do
    local x = (column - 1) * x_step + 1
    local key = landmass .. percent
    world_setup.large[key] = make_large_entry("landmassWaterlarge.pcx", x, y)
  end
end

for category, keys in pairs(world_settings) do
  for column, key in ipairs(keys) do
    local x = (column - 1) * x_step + 1
    local y = 1
    world_setup.large[key] = make_large_entry(category .. ".pcx", x, y)
  end
end

return world_setup
