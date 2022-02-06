namespace C7GameData
{
using System;

public class Player
{
	public string guid { get; set; }
	public int color { get; set; }
	public bool isBarbarians = false;

	public Player(int color)
	{
		guid = Guid.NewGuid().ToString();
		this.color = color;
	}
	public Player(){}

	public bool IsAtPeaceWith(Player other)
	{
		// Right now it's a free-for-all but eventually we'll implement peace treaties and alliances
		return false;
	}
}

}
