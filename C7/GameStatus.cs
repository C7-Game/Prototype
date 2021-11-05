using Godot;
using System;
using C7GameData;

public class GameStatus : MarginContainer
{

	LowerRightInfoBox LowerRightInfoBox = new LowerRightInfoBox();
	Timer endTurnAlertTimer;

	[Signal] public delegate void BlinkyEndTurnButtonPressed();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		MarginLeft = -(294 + 5);
		MarginTop = -(137 + 1);
		AddChild(LowerRightInfoBox);
	}
	
	public void OnNewUnitSelected(string mapUnit)
	{
		GD.Print("The newly selected unit is " + mapUnit);
	}
	
	private void OnTurnEnded()
	{
		LowerRightInfoBox.StopToggling();
	}
	
	private void OnTurnStarted()
	{		
		//Set a timer so the end turn button starts blinking after awhile.
		//Obviously once we have more game mechanics, it won't happen automatically
		//after 5 seconds.
		endTurnAlertTimer = new Timer();
		endTurnAlertTimer.WaitTime = 5.0f;
		endTurnAlertTimer.OneShot = true;
		endTurnAlertTimer.Connect("timeout", LowerRightInfoBox, "toggleEndTurnButton");
		AddChild(endTurnAlertTimer);
		endTurnAlertTimer.Start();
	}
}

