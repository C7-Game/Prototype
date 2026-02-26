local IMAGE_SIZE = 115

local leader_heads = {}

function leader_heads:map_object_to_sprite(player_or_civ)
  local era_index = 0
  local path = ""
  if (player_or_civ:GetType().Name == "Civilization") then
    era_index = 0
    path = player_or_civ.leaderArtFile
  elseif  (player_or_civ:GetType().Name == "Player") then
    era_index = player_or_civ.EraIndex()
    path = player_or_civ.civilization.leaderArtFile
  else
    error "Expected a Player or Civilization object"
  end

  -- TODO: track moods
  local xOffset = era_index * IMAGE_SIZE
  local yOffset = IMAGE_SIZE  -- 0 is annoyed, 115*2 is mad.

  return {
    path = path,
    crop_region = { xOffset, yOffset, IMAGE_SIZE, IMAGE_SIZE },
    shadows = false,
    transparent_color_indexes = { 255 },
  }
end

return leader_heads
