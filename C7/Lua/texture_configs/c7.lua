--[[
	This configuration file returns a table with texture definitions for "modern" graphics.
	It produces this table by copying the config of the civ3 assets and replaces the paths to the original Civilization 3 PCX textures with modern PNG versions.
	To add new replacement texture to the config modify the "c7_texture_list" table.
--]]
local civ3_textures = require "civ3"
local utils = require "utils"

local c7_texture_list = {
  "Art/buttonsFINAL.png",
  "Art/TileInfo.png",
  "Art/X-o_ALLstates-sprite.png",
  "Art/SmallHeads/popHeads.png",
  "Art/Terrain/Mountains-snow.png",
  "Art/Terrain/Mountains.png",
  "Art/Terrain/TerrainBuildings.png",
  "Art/Terrain/Volcanos forests.png",
  "Art/Terrain/Volcanos jungles.png",
  "Art/Terrain/Volcanos.png",
  "Art/Terrain/grassland forests.png",
  "Art/Terrain/hill forests.png",
  "Art/Terrain/hill jungle.png",
  "Art/Terrain/irrigation DESETT.png",
  "Art/Terrain/irrigation PLAINS.png",
  "Art/Terrain/irrigation TUNDRA.png",
  "Art/Terrain/irrigation.png",
  "Art/Terrain/marsh.png",
  "Art/Terrain/mountain forests.png",
  "Art/Terrain/mountain jungles.png",
  "Art/Terrain/mtnRivers.png",
  "Art/Terrain/deltaRivers.png",
  "Art/Terrain/plains forests.png",
  "Art/Terrain/tnt.png",
  "Art/Terrain/roads.png",
  "Art/Terrain/railroads.png",
  "Art/Terrain/tundra forests.png",
  "Art/Terrain/wCSO.png",
  "Art/Terrain/wOOO.png",
  "Art/Terrain/wSSS.png",
  "Art/Terrain/xdgc.png",
  "Art/Terrain/xdgp.png",
  "Art/Terrain/xdpc.png",
  "Art/Terrain/xggc.png",
  "Art/Terrain/xhills.png",
  "Art/Terrain/xpgc.png",
  "Art/Terrain/xtgc.png",
  "Art/Cities/rMIDEAST.png",
  "Art/Cities/MIDEASTWALL.png",
  "Art/city screen/CityIcons.png",
  "Art/city screen/ProdButton.png",
  "Art/city screen/background.png",
  "Art/city screen/cityMgmtButtons.png",
  "Art/resources.png",
  "Art/WorldSetup/CLIMTEMPAGEDepress.png",
  "Art/WorldSetup/CLIMTEMPAGERollovers.png",
  "Art/WorldSetup/age.png",
  "Art/WorldSetup/background.png",
  "Art/WorldSetup/climate.png",
  "Art/WorldSetup/landmassWaterSMALL.png",
  "Art/WorldSetup/landmassWaterSMALLdepress.png",
  "Art/WorldSetup/landmassWaterSMALLrollovers.png",
  "Art/WorldSetup/landmassWaterlarge.png",
  "Art/WorldSetup/temperature.png",
  "Art/interface/NormButtons.png",
  "Art/interface/rolloverbuttons.png",
  "Art/interface/highlightedbuttons.png",
  "Art/interface/box left color.png",
  "Art/interface/box right color.png",
  "Art/interface/nextturn states color.png",
  "Art/interface/consoleButtons.png",
  "Art/SmallHeads/popupDOMESTIC.png",
  "Art/SmallHeads/popupTRADE.png",
  "Art/SmallHeads/popupMILITARY.png",
  "Art/SmallHeads/popupFOREIGN.png",
  "Art/SmallHeads/popupCULTURE.png",
  "Art/SmallHeads/popupSCIENCE.png",
  "Art/Advisors/domestic_icons_aux.png",
  "Art/Advisors/domesticBUTTON.png",
  "Art/Cities/city icons.png",
  "Art/popupborders.png",
  "Art/interface/menuButtons.png",
  "Art/Advisors/dialogbox.png",
  "Art/Advisors/domestic.png",
  "Art/Tech Chooser/scienceNAV.png",
  "Art/Credits/credits_background.png",
  "Art/city screen/ProductionQueueBox.png",
  "Art/Advisors/non_required.png",
  "Art/Advisors/techboxes.png",
  "Art/Advisors/military.png",
  "Art/interface/MovementLED.png",
  "Art/Advisors/science_ancient.png",
  "Art/Advisors/science_middle.png",
  "Art/Advisors/science_industrial_new.png",
  "Art/Advisors/science_modern.png",
  "Art/exitBox-backgroundStates.png",
  "Art/PlayerSetup/playerSetup.png",
  "Art/Diplomacy/talk_offer.png",
  "Art/Diplomacy/counter.png",
  "Art/PalaceView/bkgr.png",
  "Art/city screen/luxuryicons_small.png",
  "Art/Terrain/FogOfWar.png",
  "Art/Terrain/Territory.png",
  "Art/Units/units_32.png",
  "Art/city screen/buildings-small.png",
  "Art/city screen/buildings-large.png",
  "Art/Cursor.png"
}

-- Build lookup table from c7_texture_list without extensions
local lookup = {}
for _, path in ipairs(c7_texture_list) do
  lookup[utils.strip_extension(path)] = path
end

local function path_transformer(path)
  local stripped = utils.strip_extension(path)
  return lookup[stripped] or path
end

local c7_textures = utils.transform_paths(civ3_textures, path_transformer)

-- For ease of editing, we define the civ colors as hex codes, not 1x1 px images
c7_textures.civ_colors.color_0 = { path = "", hex_color = "F0F8FF" } -- Alice Blue (whiteish)
c7_textures.civ_colors.color_1 = { path = "", hex_color = "E6194B" } -- Red
c7_textures.civ_colors.color_2 = { path = "", hex_color = "F58231" } -- Orange
c7_textures.civ_colors.color_3 = { path = "", hex_color = "FFE119" } -- Yellow
c7_textures.civ_colors.color_4 = { path = "", hex_color = "3CB44B" } -- Green
c7_textures.civ_colors.color_5 = { path = "", hex_color = "4363D8" } -- Blue
c7_textures.civ_colors.color_6 = { path = "", hex_color = "000075" } -- Navy
c7_textures.civ_colors.color_7 = { path = "", hex_color = "FABED4" } -- Pink
c7_textures.civ_colors.color_8 = { path = "", hex_color = "911EB4" } -- Purple
c7_textures.civ_colors.color_9 = { path = "", hex_color = "9A6324" } -- Brown
c7_textures.civ_colors.color_10 = { path = "", hex_color = "AAFFC3" } -- Mint
c7_textures.civ_colors.color_11 = { path = "", hex_color = "42D4F4" } -- Cyan
c7_textures.civ_colors.color_12 = { path = "", hex_color = "F032E6" } -- Magenta
c7_textures.civ_colors.color_13 = { path = "", hex_color = "808000" } -- Olive
c7_textures.civ_colors.color_14 = { path = "", hex_color = "DCBEFF" } -- Lavender
c7_textures.civ_colors.color_15 = { path = "", hex_color = "A9A9A9" } -- Grey
c7_textures.civ_colors.color_16 = { path = "", hex_color = "008080" } -- Teal
c7_textures.civ_colors.color_17 = { path = "", hex_color = "FFD700" } -- Gold
c7_textures.civ_colors.color_18 = { path = "", hex_color = "800000" } -- Maroon
c7_textures.civ_colors.color_19 = { path = "", hex_color = "00FF00" } -- Lime
c7_textures.civ_colors.color_20 = { path = "", hex_color = "FFC0CB" } -- Hot Pink
c7_textures.civ_colors.color_21 = { path = "", hex_color = "4682B4" } -- Steel Blue
c7_textures.civ_colors.color_22 = { path = "", hex_color = "D2B48C" } -- Tan
c7_textures.civ_colors.color_23 = { path = "", hex_color = "FF7F50" } -- Coral
c7_textures.civ_colors.color_24 = { path = "", hex_color = "6A5ACD" } -- Slate Blue
c7_textures.civ_colors.color_25 = { path = "", hex_color = "2E8B57" } -- Sea Green
c7_textures.civ_colors.color_26 = { path = "", hex_color = "DAA520" } -- Goldenrod
c7_textures.civ_colors.color_27 = { path = "", hex_color = "C71585" } -- Medium Violet Red
c7_textures.civ_colors.color_28 = { path = "", hex_color = "556B2F" } -- Dark Olive Green
c7_textures.civ_colors.color_29 = { path = "", hex_color = "8B4513" } -- Saddle Brown
c7_textures.civ_colors.color_30 = { path = "", hex_color = "B0C4DE" } -- Light Steel Blue
c7_textures.civ_colors.color_31 = { path = "", hex_color = "696969" } -- Dim Gray

c7_textures.animations.cursor = {
  path = "Art/Animations/Cursor.png",
  animation_rows = 2,
  animation_cols = 9,
  frame_duration = 0.6,
}
c7_textures.animations.disorder = {
  path = "Art/Animations/DisorderDefault.png",
  animation_rows = 1,
  animation_cols = 6,
  frame_duration = 0.6,
}

function c7_textures.tech_icons.small:map_object_to_sprite(tech)
  return {
    path = "Art/Tech Chooser/Icons/placeholder.png",
  }
end

function c7_textures.leader_heads:map_object_to_sprite(player_or_civ)
  return {
    path = "Art/Advisors/placeholder_leaderhead.png",
  }
end

function c7_textures.borders:find_column(_)
  return 0
end

return c7_textures
