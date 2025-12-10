local YieldType = ENUMS.Tile_YieldType
local ResourceCategory = ENUMS.ResourceCategory
local Civ3FoliageAction = ENUMS.TerrainType_Civ3FoliageAction

local terraforms = {}

terraforms.validators = {
  mine = function(context)
    local mine = context.terraform.Improvement
    return mine:GetYieldBonus(context.tile.overlayTerrainType, YieldType.Production) > 0
  end,
  irrigate = function(context)
    local irrigation = context.terraform.Improvement
    return context.tile:CanBeIrrigated(irrigation, context.player)
  end,
  clear_wetlands = function(context)
    return context.tile.overlayTerrainType.allowedFoliageAction == Civ3FoliageAction.ClearWetlands
  end,
  clear_forest = function(context)
    return context.tile.overlayTerrainType.allowedFoliageAction == Civ3FoliageAction.ClearForest
  end,
}

terraforms.effects = {
  clear_wetlands = function(context)
    context.tile:ClearTerrainOverlay()
  end,
  clear_forest = function(context)
    context.tile:MaybeAwardForestClearingShields()
    context.tile:ClearTerrainOverlay()
  end,
}

terraforms.ai_score = {}

local commerce_points = 1
local shield_points = 3
local food_points = 5

local resource_points = 20
local clear_forest_points = 2
local clear_wetlands_points = 3
local railroad_transport_points = 10

-- placeholder
function terraforms.ai_score.default(_)
  return 0
end

function terraforms.ai_score.clear_wetlands(context)
  return clear_wetlands_points
end

function terraforms.ai_score.railroad(context)
  return railroad_transport_points
end

function terraforms.ai_score.clear_forest(context)
  if context.tile.hasHadForestCleared then
    return 0
  end

  -- TODO: Take the city's current production into account.
  -- There is no sense in chopping the forest, if the chopping bonus would be lost to overflow
  return clear_forest_points
end

function terraforms.ai_score.yield_improvement(context)
  local terrain_type = context.tile.overlayTerrainType
  local improvement = context.terraform.Improvement
  local player = context.player

  -- Player is in despotism. Apply "mine green, irrigate brown" heuristics
  if player.government.hasTilePenalty then
    if terrain_type.Key == "grassland" and improvement.key == "irrigation" then
      return 0
    end
    if terrain_type.Key == "plains" and improvement.key == "mine" then
      return 0
    end
  end

  local commerce_diff = improvement.GetYieldBonus(terrain_type, YieldType.Commerce)
  local shield_diff = improvement.GetYieldBonus(terrain_type, YieldType.Production)
  local food_diff = improvement.GetYieldBonus(terrain_type, YieldType.Food)

  return commerce_diff * commerce_points + shield_diff * shield_points + food_diff * food_points
end

function terraforms.ai_score.road(context)
  local base_score = terraforms.ai_score.yield_improvement(context)

  -- Provide an additional bonus for roading resources
  local resource = context.tile.Resource
  local category = resource.Category

  local important_resource = category == ResourceCategory.LUXURY or category == ResourceCategory.STRATEGIC
  local knows_about_resource = context.player.KnowsAboutResource(resource)

  if important_resource and knows_about_resource then
    return base_score + resource_points
  end

  return base_score
end

return terraforms
