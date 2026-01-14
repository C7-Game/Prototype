local CITY_WIDTH = 166
local CITY_HEIGHT = 95

local cities = {
  extra_data = {
    path = "Art/Cities/rMIDEAST.pcx",
    wallsPath = "Art/Cities/MIDEASTWALL.pcx",
  },
}

function cities:map_object_to_sprite(city_graphics_details)
  if (city_graphics_details:GetType().Name ~= "CityGraphicsDetails") then
    error "Expected a CityGraphicsDetails object"
  end

  if city_graphics_details.hasWalls then
    return {
      path = self.extra_data.wallsPath,
      crop_region = { 0, city_graphics_details.eraIndex * CITY_HEIGHT, CITY_WIDTH, CITY_HEIGHT },
      shadows = false,
    } 
  end 

  return {
    path = self.extra_data.path,
    crop_region = { city_graphics_details.sizeRank * CITY_WIDTH, city_graphics_details.eraIndex * CITY_HEIGHT, CITY_WIDTH, CITY_HEIGHT },
    shadows = false,
  }
end
  
return cities