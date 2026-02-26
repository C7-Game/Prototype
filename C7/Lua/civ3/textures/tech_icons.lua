local tech_icons = {
  small = {},
}

function tech_icons.small:map_object_to_sprite(tech)
  if (tech:GetType().Name ~= "Tech") then
    error "Expected a Tech object"
  end

  return {
    path = tech.SmallIconPath,
  }
end

return tech_icons
