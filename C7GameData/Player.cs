namespace C7GameData
{
using System;

public class Player
{
	public string guid { get; set; }
	public int color { get; set; }

	public Player(int color)
	{
		guid = Guid.NewGuid().ToString();
		this.color = color;
	}
	public Player(){}
}

}
