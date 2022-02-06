using System.Collections.Generic;

namespace C7GameData
{
using System;

public class Player
{
	public string guid { get; set; }
	public int color { get; set; }
	public bool isBarbarians = false;
	//TODO: Refactor front-end so it sends player GUID with requests.
	//We should allow multiple humans, this is a temporary measure.
	public bool isHuman = false;
	public List<MapUnit> units = new List<MapUnit>();

	public Player(int color)
	{
		guid = Guid.NewGuid().ToString();
		this.color = color;
	}

	public void AddUnit(MapUnit unit)
	{
		this.units.Add(unit);
	}

	public Player(){}
}

}
