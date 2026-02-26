local function rules()
  return GAME_DATA().rules
end

local inflows = {}

-- to match an item in a list based on a predicate
local function any(list, predicate)
  for _, v in ipairs(list) do
    if predicate(v) then
      return true
    end
  end
  return false
end
  
local function doubles_wealth_production(tech)
  return tech.DoublesWealthProduction == true
end

-- context is [ Player player, City city ]
-- this is the actual (minimal) implementation we would do for Wealth, for conquests
local function extra_commerce_calculation(context)
  local player = context.player
  local city = context.city
  
  local useful_shields = city.CurrentProductionYield().useful
  local known_techs = player.GetKnownTechs()
  local double_effect = any(known_techs, doubles_wealth_production)
  local ratio = double_effect and (rules().ShieldCostPerGold / 2) or rules().ShieldCostPerGold
  
  return math.max(1, useful_shields / ratio)
end

-- Any and all of the table values below should return an integer
-- that will be added to the respective base value.
--
-- for example commerce will be added to the overall commerce income, culture to the current culture per turn.
--
-- maintenance, unitsupport and corruption are the ones that will be subtracted by their current base value
-- so if you want MORE corruption/maintenance/unit support, the number should be negative

inflows.result = {
  wealth = {
    commerce = function(context)
      return extra_commerce_calculation(context)
    end,
  },
  -- add new inflow(s) as the example below.
  -- The respective json entry with the yieldCalculation having the path to this new item
  -- should be added in the json save file, under inflows
  -- not all fields like commerce, culture, science etc are mandatory, as long as they reflect the ones in the json
  
  --placeholder = {
  --  commerce = function(context)
  --    -- replace with a hadcoded value, a method call, etc
  --    return 0
  --  end,
  --  culture = function(context)
  --    -- replace with a hadcoded value, a method call, etc
  --    return 0
  --  end,
  --  science = function(context)
  --    -- replace with a hadcoded value, a method call, etc
  --    return 0
  --  end,
  --  happiness = function(context)
  --    -- replace with a hadcoded value, a method call, etc
  --    return 0
  --  end,
  --  maintenance = function(context)
  --    -- replace with a hadcoded value, a method call, etc
  --    return 0
  --  end,
  --  unitsupport = function(context)
  --    -- replace with a hadcoded value, a method call, etc
  --    return 0
  --  end,
  --  corruption = function(context)
  --    -- replace with a hadcoded value, a method call, etc
  --    return 0
  --  end,
  --},
  
  
  -- json example of an entry
  
  --{
  --  "name": "Placeholder", <- this is the display name in the production box
  --  "iconRowIndex": 29,
  --  "localYield": [
  --    {
  --      "yieldType": "commerce",
  --      "yieldCalculation": "inflows.result.placeholder.commerce" 
  --    },
  --    {
  --      "yieldType": "culture",
  --      "yieldCalculation": "inflows.result.placeholder.culture"
  --    },
  --    {
  --      "yieldType": "science",
  --      "yieldCalculation": "inflows.result.placeholder.science"
  --    },
  --    {
  --      "yieldType": "happiness",
  --      "yieldCalculation": "inflows.result.placeholder.happiness"
  --    },
  --    {
  --      "yieldType": "maintenance",
  --      "yieldCalculation": "inflows.result.placeholder.maintenance"
  --    },
  --    {
  --      "yieldType": "unitsupport",
  --      "yieldCalculation": "inflows.result.placeholder.unitsupport"
  --    },
  --    {
  --      "yieldType": "corruption",
  --      "yieldCalculation": "inflows.result.placeholder.corruption"
  --    }
  --  ]
  --}
}

return inflows
