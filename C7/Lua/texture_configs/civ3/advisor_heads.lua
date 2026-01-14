local IMAGE_SIZE = 150
local CROP_HEIGHT = 110

local advisor_heads = {
  extra_data = {
    domestic_path = "Art/SmallHeads/popupDOMESTIC.pcx",
    trade_path = "Art/SmallHeads/popupTRADE.pcx",
    military_path = "Art/SmallHeads/popupMILITARY.pcx",
    foreign_path = "Art/SmallHeads/popupFOREIGN.pcx",
    culture_path = "Art/SmallHeads/popupCULTURE.pcx",
    science_path = "Art/SmallHeads/popupSCIENCE.pcx",
  },
}

function advisor_heads:map_object_to_sprite(details)
  if details:GetType().Name ~= "AdvisorGraphicsDetails" then
    error "Expected a AdvisorGraphicsDetails object"
  end

  -- Note: we need to use self.<advisor> for the c7.lua
  -- conversion to work.
  local path = nil
  if details.advisor == details.advisor.Domestic then
    path = self.extra_data.domestic_path
  elseif details.advisor == details.advisor.Trade then
    path = self.extra_data.trade_path
  elseif details.advisor == details.advisor.Military then
    path = self.extra_data.military_path
  elseif details.advisor == details.advisor.Foreign then
    path = self.extra_data.foreign_path
  elseif details.advisor == details.advisor.Culture then
    path = self.extra_data.culture_path
  elseif details.advisor == details.advisor.Science then
    path = self.extra_data.science_path
  end

  local mood_col = nil
  if details.mood == details.mood.Happy then
    mood_col = 0
  elseif details.mood == details.mood.Angry then
    mood_col = 1
  elseif details.mood == details.mood.Sad then
    mood_col = 2
  elseif details.mood == details.mood.Surprised then
    mood_col = 3
  end

  return {
    path = path,
    crop_region = {
      1 + mood_col * IMAGE_SIZE,
      (details.eraIndex + 1) * IMAGE_SIZE - CROP_HEIGHT,
      IMAGE_SIZE - 1,
      CROP_HEIGHT,
    },
    shadows = false,
  }
end

return advisor_heads
