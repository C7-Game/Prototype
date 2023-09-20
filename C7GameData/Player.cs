using System.Collections.Generic;
using System.Linq;
using C7Engine.AI.StrategicAI;

namespace C7GameData
{
using System;

public class Player
{
	public ID id { get; internal set; }
	public uint color { get; set; }
	public bool isBarbarians = false;
	//TODO: Refactor front-end so it sends player GUID with requests.
	//We should allow multiple humans, this is a temporary measure.
	public bool isHuman = false;
	public bool hasPlayedThisTurn = false;

	public Civilization civilization;
	internal int cityNameIndex = 0;

	public List<MapUnit> units = new List<MapUnit>();
	public List<City> cities = new List<City>();
	public TileKnowledge tileKnowledge = new TileKnowledge();

	//Ordered list of priority data.  First is most important.
	public List<StrategicPriority> strategicPriorityData = new List<StrategicPriority>();
	public int turnsUntilPriorityReevaluation = 0;

	public Player(ID id, uint color)
	{
		this.id = id;
		this.color = color & 0xFFFFFFFF;
	}

	public Player(ID id, Civilization civilization, uint color)
	{
		this.civilization = civilization;
		this.id = id;
		this.color = color & 0xFFFFFFFF;
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

	// Once we have technologies, not all resources will be known at the start.
	// Eventually, perhaps there will be other gates around resource access as well
	// For now, just always return true, but have this method so we have that structure
	// in place.
	public bool KnowsAboutResource(Resource resource) {
		return true;
	}

	public override string ToString() {
		if (civilization != null)
			return civilization.cityNames.First();
		return "";
	}
}

}
