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
	
	public void OnNewUnitSelected(ParameterWrapper wrappedMapUnit)
	{
		MapUnit NewUnit = wrappedMapUnit.GetValue<MapUnit>();
		GD.Print("The newly selected unit's name is: " + NewUnit.unitType.name);
		LowerRightInfoBox.UpdateUnitInfo(NewUnit);
	}
	
	private void OnTurnEnded()
	{
		LowerRightInfoBox.StopToggling();
	}
	
	private void OnTurnStarted()
	{		
		//TODO: Remove this signal handler, probably
	}
	
	private void OnNoMoreAutoselectableUnits()
	{
		LowerRightInfoBox.SetEndOfTurnStatus();
	}
}
