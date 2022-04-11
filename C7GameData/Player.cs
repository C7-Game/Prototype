using System.Collections.Generic;

namespace C7GameData
{
	public class Player : Unique
	{
		public int color { get; set; }
		public bool isBarbarians = false;
		//TODO: Refactor front-end so it sends player GUID with requests.
		//We should allow multiple humans, this is a temporary measure.
		public bool isHuman = false;
		public bool hasPlayedThisTurn = false;

		public Civilization civilization;
		private int cityNameIndex = 0;

		public List<MapUnit> units = new List<MapUnit>();
		public List<City> cities = new List<City>();
		public TileKnowledge tileKnowledge = new TileKnowledge();

		public Player(uint color)
		{
			this.color = (int)(color & 0xFFFFFFFF);
		}

		public Player(Civilization civilization, uint color)
		{
			this.civilization = civilization;
			this.color = (int)(color & 0xFFFFFFFF);
		}

		public void AddUnit(MapUnit unit)
		{
			this.units.Add(unit);
		}

		public string GetNextCityName()
		{
			string name = civilization.cityNames[cityNameIndex % civilization.cityNames.Count];
			int bonusLoops = cityNameIndex / civilization.cityNames.Count;
			if (bonusLoops % 2 == 1) {
				name = "New " + name;
			}
			int suffix = (bonusLoops / 2) + 1;
			if (suffix > 1) {
				name = name + " " + suffix; //e.g. for bonusLoops = 2, we'll have "Athens 2"
			}
			cityNameIndex++;
			return name;
		}

		public Player(){}

		public bool IsAtPeaceWith(Player other)
		{
			// Right now it's a free-for-all but eventually we'll implement peace treaties and alliances
			return other == this;
		}

		public bool SitsOutFirstTurn()
		{
			// TODO: Scenarios can also specify that certain players sit out the first turn. E.g. WW2 in the Pacific
			return isBarbarians;
		}
	}
}
