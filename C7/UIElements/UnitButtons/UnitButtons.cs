using Godot;
using System.Collections.Generic;
using C7GameData;
using Serilog;

/*
 UnitButtons contains the buttons at the bottom of the game UI when viewing the
 map, and control unit actions. Clicking buttons triggers Input.ActionPress
 calls which are checked and handled in Game.processActions.
*/

public partial class UnitButtons : VBoxContainer {

	private ILogger log = LogManager.ForContext<UnitButtons>();

	private Dictionary<string, UnitControlButton> buttonMap = new Dictionary<string, UnitControlButton>();
	HBoxContainer primaryControls;
	HBoxContainer specializedControls;
	HBoxContainer advancedControls;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		// You can hide buttons like this.  This will come in handy later!
		// Remember to re-calc the margin after hiding/unhiding buttons, as that may affect the width.
		// this.GetNode<FortifyButton>("PrimaryUnitControls/FortifyButton").Hide();

		primaryControls = GetNode<HBoxContainer>("PrimaryUnitControls");
		specializedControls = GetNode<HBoxContainer>("SpecializedUnitControls");

		AddNewButton(primaryControls, new UnitControlButton(C7Action.UnitHold, 0, 0, onButtonPressed));
		AddNewButton(primaryControls, new UnitControlButton(C7Action.UnitWait, 1, 0, onButtonPressed));
		AddNewButton(primaryControls, new UnitControlButton(C7Action.UnitFortify, 2, 0, onButtonPressed));
		AddNewButton(primaryControls, new UnitControlButton(C7Action.UnitDisband, 3, 0, onButtonPressed));
		AddNewButton(primaryControls, new UnitControlButton(C7Action.UnitGoto, 4, 0, onButtonPressed));
		AddNewButton(primaryControls, new UnitControlButton(C7Action.UnitExplore, 5, 0, onButtonPressed));
		AddNewButton(primaryControls, new UnitControlButton(C7Action.UnitSentry, 6, 0, onButtonPressed));
		AddNewButton(primaryControls, new UnitControlButton(C7Action.UnitSentryEnemyOnly, 2, 5, onButtonPressed));

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
		AddNewButton(specializedControls, new UnitControlButton("scienceAge", 3, 2, onButtonPressed));  //validate
		AddNewButton(specializedControls, new UnitControlButton("buildColony", 4, 2, onButtonPressed)); //validate
		AddNewButton(specializedControls, new UnitControlButton(C7Action.UnitBuildCity, 5, 2, onButtonPressed));
		AddNewButton(specializedControls, new UnitControlButton(C7Action.UnitBuildRoad, 6, 2, onButtonPressed));
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

		// Row index 4 and later not yet added
	}

	private void AddNewButton(HBoxContainer row, UnitControlButton button) {
		row.AddChild(button);
		buttonMap[button.action] = button;
	}

	private void onButtonPressed(string action) {
		Input.ActionPress(action);
	}

	private void OnNoMoreAutoselectableUnits() {
		this.Visible = false;
	}

	private void OnNewUnitSelected(ParameterWrapper<MapUnit> wrappedMapUnit) {
		MapUnit unit = wrappedMapUnit.Value;
		foreach (UnitControlButton button in buttonMap.Values) {
			button.Visible = false;
		}

		// pcen (may 2023) - switched this to use the prototype's actions, not
		// sure I understand the commented goal. Before, was commented:
		//     TODO: This is technically right, since the unit's available actions have been attached,
		//     but is unintuitive since they typically are on the prototype.
		//     Goal: Send the actions as a list.
		foreach (string action in unit.unitType.actions) {
			if (buttonMap.ContainsKey(action)) {
				buttonMap[action].Visible = true;
			} else {
				log.Warning("Could not find button " + action);
			}
		}

		this.Visible = true;
	}
}
