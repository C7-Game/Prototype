local HEAD_SIZE = 48
local HEAD_SIZE_WITH_BORDER = 50
local NUM_ERAS = 4
local MOODS_PER_ERA = 4

local popheads = {
  extra_data = {
    path = "Art/SmallHeads/popHeads.pcx",
  },
}

local function validate_input(pair)
  local resident = pair.cityResident
  local era_num = pair.eraNum

  if resident:GetType().Name ~= "CityResident" then
    error "Expected a CityResident object"
  end

  if type(era_num) ~= "number" then
    error "Expected a number"
  end

  return resident, era_num
end

local function get_laborer(resident, era_num)
  local column = nil

  if resident.mood == resident.mood.Content then
    column = 0
  elseif resident.mood == resident.mood.Happy then
    column = 1
  elseif resident.mood == resident.mood.Unhappy then
    column = 3
  end

  if column == nil then
    error "Unknown mood type"
  end

  local x = 1
  local y = MOODS_PER_ERA * HEAD_SIZE_WITH_BORDER * era_num + HEAD_SIZE_WITH_BORDER * column + 1

  return { x, y, HEAD_SIZE, HEAD_SIZE }
end

local function get_specialist(resident, era_num)
  local citizen_type = resident.citizenType
  local x = HEAD_SIZE_WITH_BORDER * era_num

  local num_rows_of_laborers = NUM_ERAS * MOODS_PER_ERA
  local y = HEAD_SIZE_WITH_BORDER * num_rows_of_laborers + HEAD_SIZE_WITH_BORDER * (citizen_type.SpecialistIndex - 1)

  return { x + 1, y + 1, HEAD_SIZE, HEAD_SIZE }
end

function popheads:map_object_to_sprite(pair)
  local resident, eru_num = validate_input(pair)
  local citizen_type = resident.citizenType

  local crop_region
  if citizen_type.IsDefaultCitizen then
    crop_region = get_laborer(resident, eru_num)
  else
    crop_region = get_specialist(resident, eru_num)
  end

  return { crop_region = crop_region, path = self.extra_data.path }
end

return popheads
