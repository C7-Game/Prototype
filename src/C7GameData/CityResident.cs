namespace C7GameData {
	public class CityResident {
		public CitizenType citizenType;

		// Only relevant if citizenType.IsDefaultCitizen == true
		public Tile tileWorked = Tile.NONE;
		public Civilization nationality;
		public City city;
		//Eventually more things like happiness
	}
}
