using System.Collections.Generic;

namespace C7GameData {
	public class BarbarianTribe {
		private string name = "Vandals";
		private List<Tile> campLocations = new List<Tile>();
		private TileKnowledge tileKnowledge = new TileKnowledge();
		private List<MapUnit> units = new List<MapUnit>();

		public BarbarianTribe(Tile startingCamp, MapUnit startingUnit) {
			campLocations.Add(startingCamp);
			units.Add(startingUnit);
		}
	}
}
