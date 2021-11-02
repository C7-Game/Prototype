using Godot;
public class TurnCounterComponent : GameComponent
{
	int _turnCount = 0;

	public void OnTurnStarted()
	{
		_turnCount++;
		GD.Print(string.Format("Turn {0}", _turnCount));
	}
}
