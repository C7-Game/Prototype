local YieldType = ENUMS.Tile_YieldType
local Layer = ENUMS.TerrainImprovement_Layer

local terrain_improvements = {
  railroad = {
    tile_modifier = function(yield)
      local yield_improvement = yield.tile.overlays:ImprovementAtLayer(Layer.ResourceDevelopment)

      if not yield_improvement then
        return
      end

      -- bonus production for a mined tile
      if yield_improvement.key == "mine" and yield.type == YieldType.Production then
        yield.bonus = yield.bonus + 1
      end

      -- bonus food for an irrigated tile
      if yield_improvement.key == "irrigation" and yield.type == YieldType.Food then
        yield.bonus = yield.bonus + 1
      end
    end,
  },
}

return terrain_improvements
