using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace C7GameData {
	public class BarbarianTribe {
		private string name = "Vandals";
		private List<Tile> campLocations = new List<Tile>();
		private TileKnowledge tileKnowledge = new TileKnowledge();
		private List<MapUnit> units = new List<MapUnit>();

		public BarbarianTribe(Tile startingCamp, MapUnit startingUnit) {
			campLocations.Add(startingCamp);
			tileKnowledge.AddTilesToKnown(startingCamp);
			units.Add(startingUnit);
		}

		public ReadOnlyCollection<MapUnit> GetUnits() {
			return units.AsReadOnly();
		}

		public void AddUnit(MapUnit unit) {
			this.units.Add(unit);
		}

		public void RemoveUnit(MapUnit unit) {
			this.units.Remove(unit);
		}

		public ReadOnlyCollection<Tile> GetCamps() {
			return campLocations.AsReadOnly();
		}
	}
}
