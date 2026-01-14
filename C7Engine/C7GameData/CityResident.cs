namespace C7GameData {
	public class CityResident {
		public CitizenType citizenType;

		// Only relevant if citizenType.IsDefaultCitizen == true
		public Tile tileWorked = Tile.NONE;
		public Civilization nationality;
		public City city;

		// Only relevant if citizenType.IsDefaultCitizen == true
		public enum Mood {
			Happy,
			Content,
			Unhappy
		};
		public Mood mood;
	}
}
