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

		// The amount of gold this player has.
		public int gold = 0;

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
				currentlyResearchedTech = currentlyResearchedTech,
				eraCivilopediaName = eraCivilopediaName,
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
			gold = player.gold;
		}
	}
}
