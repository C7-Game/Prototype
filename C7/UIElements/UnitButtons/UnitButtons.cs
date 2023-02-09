using Godot;
using System.Collections.Generic;
using C7GameData;
using Serilog;

public partial class UnitButtons : VBoxContainer
{

	private ILogger log = LogManager.ForContext<UnitButtons>();

	[Signal] public delegate void UnitButtonPressedEventHandler(string button);

	private Dictionary<string, UnitControlButton> buttonMap = new Dictionary<string, UnitControlButton>();
	HBoxContainer primaryControls;
	HBoxContainer specializedControls;
	HBoxContainer advancedControls;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//You can hide buttons like this.  This will come in handy later!
		//Remember to re-calc the margin after hiding/unhiding buttons, as that may affect the width.
		//this.GetNode<FortifyButton>("PrimaryUnitControls/FortifyButton").Hide();

		primaryControls = GetNode<HBoxContainer>("PrimaryUnitControls");
		specializedControls = GetNode<HBoxContainer>("SpecializedUnitControls");

		AddNewButton(primaryControls, new UnitControlButton("hold", Godot.Key.Space, 0, 0, onButtonPressed));
		AddNewButton(primaryControls, new UnitControlButton("wait", Godot.Key.W,  1, 0, onButtonPressed));
		AddNewButton(primaryControls, new UnitControlButton("fortify", Godot.Key.F,  2, 0, onButtonPressed));
		AddNewButton(primaryControls, new UnitControlButton("disband", Godot.Key.D, 3, 0, onButtonPressed));
		AddNewButton(primaryControls, new UnitControlButton("goTo", Godot.Key.G, 4, 0, onButtonPressed));
		AddNewButton(primaryControls, new UnitControlButton("explore", 5, 0, onButtonPressed));
		AddNewButton(primaryControls, new UnitControlButton("sentry", 6, 0, onButtonPressed));
		AddNewButton(primaryControls, new UnitControlButton("sentryEnemyOnly", 2, 5, onButtonPressed));

		//   ******* SPECIALIZED CONTROLS *************
		AddNewButton(specializedControls, new UnitControlButton("load", 7, 0, onButtonPressed));
		AddNewButton(specializedControls, new UnitControlButton("unload", 0, 1, onButtonPressed));
		AddNewButton(specializedControls, new UnitControlButton("pillage", 2, 1, onButtonPressed));
		AddNewButton(specializedControls, new UnitControlButton("bombard", 3, 1, onButtonPressed));
		AddNewButton(specializedControls, new UnitControlButton("autobombard", 3, 5, onButtonPressed));
		AddNewButton(specializedControls, new UnitControlButton("paradrop", 4, 1, onButtonPressed));
		//superfortify?
		AddNewButton(specializedControls, new UnitControlButton("hurryBuilding", 6, 1, onButtonPressed));
		AddNewButton(specializedControls, new UnitControlButton("upgrade", 7, 1, onButtonPressed));

		//TODO: The first two buttons in row index 2, and validate science age/colony are correct
		AddNewButton(specializedControls, new UnitControlButton("sacrifice", 3, 2, onButtonPressed));
		AddNewButton(specializedControls, new UnitControlButton("scienceAge", 3, 2, onButtonPressed));	//validate
		AddNewButton(specializedControls, new UnitControlButton("buildColony", 4, 2, onButtonPressed));	//validate
		AddNewButton(specializedControls, new UnitControlButton("buildCity", Godot.Key.B, 5, 2, onButtonPressed));
		AddNewButton(specializedControls, new UnitControlButton("buildRoad", Godot.Key.R, 6, 2, onButtonPressed));
		AddNewButton(specializedControls, new UnitControlButton("buildRailroad", 7, 2, onButtonPressed));

		AddNewButton(specializedControls, new UnitControlButton("fortress", 0, 3, onButtonPressed));
		AddNewButton(specializedControls, new UnitControlButton("barricade", 4, 4, onButtonPressed));
		AddNewButton(specializedControls, new UnitControlButton("mine", 1, 3, onButtonPressed));
		AddNewButton(specializedControls, new UnitControlButton("irrigate", 2, 3, onButtonPressed));
		AddNewButton(specializedControls, new UnitControlButton("chopForest", 3, 3, onButtonPressed));
		AddNewButton(specializedControls, new UnitControlButton("chopJungle", 4, 3, onButtonPressed));
		AddNewButton(specializedControls, new UnitControlButton("plantForest", 5, 3, onButtonPressed));
		AddNewButton(specializedControls, new UnitControlButton("clearDamage", 6, 3, onButtonPressed));
		AddNewButton(specializedControls, new UnitControlButton("automate", 7, 3, onButtonPressed));

		//Row index 4 and later not yet added
	}

	private void AddNewButton(HBoxContainer row, UnitControlButton button)
	{
		row.AddChild(button);
		buttonMap[button.key] = button;
	}

	private void onButtonPressed(string buttonKey)
	{
		EmitSignal("UnitButtonPressed", buttonKey);
	}

	private void OnNoMoreAutoselectableUnits()
	{
		this.Visible = false;
	}

	private void OnNewUnitSelected(ParameterWrapper wrappedMapUnit)
	{
		MapUnit unit = wrappedMapUnit.GetValue<MapUnit>();


		foreach (UnitControlButton button in buttonMap.Values) {
			button.Visible = false;
		}

		//TODO: This is technically right, since the unit's available actions have been attached,
		//but is unintuitive since they typically are on the prototype.
		//Goal: Send the actions as a list.
		foreach (string buttonKey in unit.availableActions) {
			if (buttonMap.ContainsKey(buttonKey)) {
				buttonMap[buttonKey].Visible = true;
			}
			else {
				log.Warning("Could not find button " + buttonKey);
			}
		}

		this.Visible = true;
	}

	public override void _UnhandledInput(InputEvent @event) {
		if (this.Visible)
		{
			if (@event is InputEventKey eventKey && eventKey.Pressed)
			{
				foreach (UnitControlButton button in buttonMap.Values)
				{
					if (button.Visible == true) {
						if (eventKey.Keycode == button.shortcutKey && !eventKey.ShiftPressed && !eventKey.IsCommandOrControlPressed() && !eventKey.AltPressed) {
							this.onButtonPressed(button.key);
							GetViewport().SetInputAsHandled();
						}
					}
				}
			}
		}
	}
}
