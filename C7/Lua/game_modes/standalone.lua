local unit_replacement_art_map = {
  ["Settler"] = "Carthaginian Settler",
  ["Worker"] = "Carthaginian Worker",
  ["Scout"] = "Euro Scout",
  ["Explorer"] = "German Explorer",
  ["Warrior"] = "Tribal Mediterranean Warrior",
  ["Archer"] = "Chichimeca Archer",
  ["Spearman"] = "European Spearman",
  ["Swordsman"] = "European Swordsman",
  ["Chariot"] = "thracian chariot",
  ["Horseman"] = "Serbian Horseman",
  ["Pikeman"] = "Elf Spearman",
  ["Longbowman"] = "Longbowman",
  ["Musketman"] = "Generic Musketman 1600",
  ["Knight"] = "Medieval European Horse Spearman",
  ["Cavalry"] = "1750 British Dragoon",
  ["Catapult"] = "Dark Ages Onager",
  ["Cannon"] = "French Artillery 17th C",
  ["Galley"] = "Bireme",
  ["Caravel"] = "Cog",
  ["Frigate"] = "HeavyFrigate",
  ["Galleon"] = "East_Indiaman1",
  ["Privateer"] = "Pirate Ship",
  ["Medieval Infantry"] = "Gothic Swordsman",
  ["Trebuchet"] = "Medieval Trebuchet",
  ["Crusader"] = "Black Hospitaller Swordsman",
  ["Ancient Cavalry"] = "Oscan Companion",
  ["Curragh"] = "MinoanGalley",
}

return function(civ3_game_mode)
  local updated_unit_prototypes = {}

  for _, unit_prototype in ipairs(civ3_game_mode.unitPrototypes) do
    local replacement_art = unit_replacement_art_map[unit_prototype.name]

    -- Only preserve a unit if we have an art replacement for it
    if replacement_art then
      unit_prototype.artName = replacement_art

      -- Remove the unit upgrade if we don't have a sprite for it
      local upgrade = unit_prototype.upgradeTo
      if not unit_replacement_art_map[upgrade] then
        unit_prototype.upgradeTo = nil
      end

      table.insert(updated_unit_prototypes, unit_prototype)
    end
  end

  civ3_game_mode.unitPrototypes = updated_unit_prototypes

  return civ3_game_mode
end
