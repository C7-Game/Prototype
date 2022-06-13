using Godot;
using C7Engine;
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

	public void RegisterEvents()
	{
		ComponentManager.Instance.GetComponent<CalendarComponent>().TurnStarted += (obj, args) => LowerRightInfoBox.SetTurn(args.Turn.TurnDate);
	}
	
	public void OnNewUnitSelected(ParameterWrapper wrappedMapUnit)
	{
		MapUnit newUnit = wrappedMapUnit.GetValue<MapUnit>();
		GD.Print("Selected unit: " + newUnit + " at " + newUnit.location);
		LowerRightInfoBox.UpdateUnitInfo(newUnit, newUnit.location.overlayTerrainType);
	}
	
	private void OnTurnEnded()
	{
		LowerRightInfoBox.StopToggling();
	}
	
	private void OnTurnStarted(int turnNumber)
	{		
		//Oh hai, we do need this handler here!
		//LowerRightInfoBox.SetTurn(turnNumber);
	}
	
	private void OnNoMoreAutoselectableUnits()
	{
		LowerRightInfoBox.SetEndOfTurnStatus();
	}
}
