using System.Collections.Generic;

namespace C7GameData.Save {

	public class SavePlayer {
		public ID id;
		public int colorIndex;
		public bool barbarian;
		public bool human = false;
		public bool hasPlayedCurrentTurn = false;

		public string civilization;
		public int cityNameIndex = 0;

		public List<TileLocation> tileKnowledge = new List<TileLocation>();

		// The list of techs known by this player.
		public HashSet<ID> knownTechs = new();

		// The tech the player is currently researching.
		public ID currentlyResearchedTech;

		// The civilopedia name of the era this player is in.
		//
		// The civilopedia name is what is used for art lookups, not the actual
		// name.
		public string eraCivilopediaName;

		public int turnsUntilPriorityReevaluation = 0;

		// The values of the science/happiness/tax sliders (tax is implicit)
		// A value of 1 => 10%, a value of 10 => 100%.
		//
		// INVARIANT: LuxuryRate + ScienceRate + TaxRate = 10
		public int luxuryRate = 0;
		public int scienceRate = 5;
		public int taxRate = 5;

		// The amount of gold this player has.
		public int gold = 0;

		// The number of "beakers" (gold) spent on the currently researched
		// tech.
		public int beakers = 0;

		// The number of turns the player has been researching the current tech.
		public int turnsResearched = 0;

		public Player ToPlayer(GameMap map, List<Civilization> civilizations) {
			Player player = new Player{
				id = id,
				isBarbarians = barbarian,
				isHuman = human,
				hasPlayedThisTurn = hasPlayedCurrentTurn,
				colorIndex = colorIndex,
				civilization = civilization is not null ? civilizations.Find(civ => civ.name == civilization) : null,
				cityNameIndex = cityNameIndex,
				tileKnowledge = new TileKnowledge(),
				knownTechs = knownTechs,
				eraCivilopediaName = eraCivilopediaName,
				luxuryRate = luxuryRate,
				scienceRate = scienceRate,
				taxRate = taxRate,
				gold = gold,
			};
			foreach (TileLocation tile in tileKnowledge) {
				player.tileKnowledge.AddTileToKnown(map.tileAt(tile.X, tile.Y));
			}
			foreach (ID techId in player.civilization.startingTechs) {
				if (!player.knownTechs.Contains(techId)) {
					player.knownTechs.Add(techId);
				}
			}

			// Because of the custom setter we need to set the researched tech
			// and then set the beakers and turns researched - otherwise they'd
			// be reset by the setter.
			player.SetCurrentlyResearchedTech(currentlyResearchedTech);
			player.beakers = beakers;
			player.turnsResearched = turnsResearched;

			return player;
		}

		public SavePlayer() { }

		public SavePlayer(Player player) {
			id = player.id;
			colorIndex = player.colorIndex;
			barbarian = player.isBarbarians;
			human = player.isHuman;
			hasPlayedCurrentTurn = player.hasPlayedThisTurn;
			civilization = player.civilization?.name;
			// TODO: this should be computed by looking at cities defined in the save
			// so that adding cities in the save structure doesn't require updating this value
			cityNameIndex = player.cityNameIndex;
			tileKnowledge = player.tileKnowledge.AllKnownTiles().ConvertAll(tile => new TileLocation(tile));
			turnsUntilPriorityReevaluation = player.turnsUntilPriorityReevaluation;
			knownTechs = player.knownTechs;
			currentlyResearchedTech = player.currentlyResearchedTech;
			eraCivilopediaName = player.eraCivilopediaName;
			luxuryRate = player.luxuryRate;
			scienceRate = player.scienceRate;
			taxRate = player.taxRate;
			gold = player.gold;
			beakers = player.beakers;
			turnsResearched = player.turnsResearched;
		}
	}
}
