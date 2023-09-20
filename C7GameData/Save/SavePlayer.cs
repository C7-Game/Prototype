using System.Collections.Generic;

namespace C7GameData.Save {

	public class SavePlayer {
		public ID id;
		public uint color;
		public bool barbarian;
		public bool human = false;
		public bool hasPlayedCurrentTurn = false;

		public string civilization;
		public int cityNameIndex = 0;

		public List<TileLocation> tileKnowledge = new List<TileLocation>();

		public int turnsUntilPriorityReevaluation = 0;

		public Player ToPlayer(GameMap map, List<Civilization> civilizations) {
			Player player = new Player{
				color = color,
				id = id,
				isBarbarians = barbarian,
				isHuman = human,
				hasPlayedThisTurn = hasPlayedCurrentTurn,
				civilization = civilization is not null ? civilizations.Find(civ => civ.name == civilization) : null,
				cityNameIndex = cityNameIndex,
				tileKnowledge = new TileKnowledge(),
			};
			foreach (TileLocation tile in tileKnowledge) {
				player.tileKnowledge.AddTileToKnown(map.tileAt(tile.x, tile.y));
			}
			return player;
		}

		public SavePlayer() {}

		public SavePlayer(Player player) {
			id = player.id;
			color = player.color;
			barbarian = player.isBarbarians;
			human = player.isHuman;
			hasPlayedCurrentTurn = player.hasPlayedThisTurn;
			civilization = player.civilization?.name;
			// TODO: this should be computed by looking at cities defined in the save
			// so that adding cities in the save structure doesn't require updating this value
			cityNameIndex = player.cityNameIndex;
			tileKnowledge = player.tileKnowledge.AllKnownTiles().ConvertAll(tile => new TileLocation(tile));
			turnsUntilPriorityReevaluation = player.turnsUntilPriorityReevaluation;
		}
	}
}
