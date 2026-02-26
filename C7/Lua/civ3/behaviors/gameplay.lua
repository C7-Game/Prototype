-- A generic lua script for orchestrating behaviours during gameplay.
-- The idea for this is to host script paths that are not serialized in the json,
-- but rather "hardcoded" in the code

local function game_data()
  return GAME_DATA()
end
  
local function rules()
  return GAME_DATA().rules
end
  
local gameplay = {}

local function disband_reward(context)
  local unit = context
  local unit_shield_cost = unit.unitType.shieldCost;
  
  local has_city = unit.location.HasCity
  local is_unit_on_own_city = unit.location.OwningPlayer() == unit.owner
  
  if has_city and is_unit_on_own_city then
    local city = unit.location.owningCity;
    local item_produced = city.itemBeingProduced
    
      if (item_produced:GetType().Name == "Building" and item_produced.IsGreatWonder() == true) then
        return
      end
        
      if (item_produced:GetType().Name == "Inflow") then
        return
      end

    local reward = math. floor(unit_shield_cost * rules().ShieldRateForDisbanding)
    game_data().SendMessageToUiFromLua("Our "..unit.name.." has been converted to "..reward.." shields.", unit.location)
    city.SetStoredShields(reward, true)
  end
end
  
gameplay = {
  units = {
    -- Context = MapUnit mapUnit
    disband = function(context)
      disband_reward(context)
    end,
  },
}

return gameplay