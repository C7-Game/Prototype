using Godot;
using System;
using C7GameData;
using Serilog;

public class GameStatus : MarginContainer
{

	private ILogger log = LogManager.ForContext<GameStatus>();

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
		MapUnit newUnit = wrappedMapUnit.GetValue<MapUnit>();
		log.Information("Selected unit: " + newUnit + " at " + newUnit.location);
		LowerRightInfoBox.UpdateUnitInfo(newUnit, newUnit.location.overlayTerrainType);
	}

	private void OnTurnEnded()
	{
		LowerRightInfoBox.StopToggling();
	}

	private void OnTurnStarted(int turnNumber)
	{
		//Oh hai, we do need this handler here!
		LowerRightInfoBox.SetTurn(turnNumber);
	}

	private void OnNoMoreAutoselectableUnits()
	{
		LowerRightInfoBox.SetEndOfTurnStatus();
	}
}
