using Godot;
using System;
using C7GameData;
using Serilog;

public partial class GameStatus : MarginContainer
{

	private ILogger log = LogManager.ForContext<GameStatus>();

	LowerRightInfoBox LowerRightInfoBox = new LowerRightInfoBox();
	Timer endTurnAlertTimer;

	[Signal] public delegate void BlinkyEndTurnButtonPressedEventHandler();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		OffsetLeft = -(294 + 5);
		OffsetTop = -(137 + 1);
		AddChild(LowerRightInfoBox);
	}

	public void OnNewUnitSelected(ParameterWrapper<MapUnit> wrappedMapUnit)
	{
		MapUnit newUnit = wrappedMapUnit.Value;
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
