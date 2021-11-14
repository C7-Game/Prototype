namespace C7GameData
{
using System;

public class Player
{
	public string guid { get; }
	public int color { get; }

	public Player(int color)
	{
		guid = Guid.NewGuid().ToString();
		this.color = color;
	}
}

}
