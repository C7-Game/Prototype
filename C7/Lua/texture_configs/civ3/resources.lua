local RESOURCE_SIZE = 50
local LUXURY_SMALL_SIZE = 22

local resources = {}

resources.large = {
  extra_data = {
    path = "Art/resources.pcx",
  },
}

function resources.large:map_object_to_sprite(resource)
  if resource:GetType().Name ~= "Resource" then
    error "Expected a Resource object"
  end

  local icon = resource.Icon
  local row = math.floor(icon / 6)
  local col = icon % 6

  return {
    path = self.extra_data.path,
    crop_region = { col * RESOURCE_SIZE, row * RESOURCE_SIZE, RESOURCE_SIZE, RESOURCE_SIZE },
    shadows = false,
  }
end

resources.small = {
  extra_data = {
    path = "Art/city screen/luxuryicons_small.pcx",
  },
}

function resources.small:map_object_to_sprite(resource)
  if resource:GetType().Name ~= "Resource" then
    error "Expected a Resource object"
  end

  local icon = resource.Icon
  local col = icon - 8

  if col < 0 then
    error "Invalid resource icon index"
  end

  return {
    path = self.extra_data.path,
    crop_region = { col * LUXURY_SMALL_SIZE, 0, LUXURY_SMALL_SIZE, LUXURY_SMALL_SIZE },
    shadows = false,
  }
end

return resources
