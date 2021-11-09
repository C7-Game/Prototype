using Godot;
using System;

public class UnitButtons : VBoxContainer
{

	[Signal] public delegate void UnitButtonPressed(string button);

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//You can hide buttons like this.  This will come in handy later!
		//Remember to re-calc the margin after hiding/unhiding buttons, as that may affect the width.
		//this.GetNode<FortifyButton>("PrimaryUnitControls/FortifyButton").Hide();

		HBoxContainer primaryControls = GetNode<HBoxContainer>("PrimaryUnitControls");
		UnitControlButton holdButton = new UnitControlButton("hold", 0, 0, onButtonPressed);
		UnitControlButton waitButton = new UnitControlButton("wait", 1, 0, onButtonPressed);
		UnitControlButton fortifyButton = new UnitControlButton("fortify", 2, 0, onButtonPressed);
		UnitControlButton disbandButton = new UnitControlButton("disband", 3, 0, onButtonPressed);
		UnitControlButton goToButton = new UnitControlButton("goTo", 4, 0, onButtonPressed);
		UnitControlButton exploreButton = new UnitControlButton("explore", 5, 0, onButtonPressed);
		UnitControlButton sentryButton = new UnitControlButton("sentry", 6, 0, onButtonPressed);
		UnitControlButton sentryEnemyOnly = new UnitControlButton("sentryEnemyOnly", 2, 5, onButtonPressed);
		primaryControls.AddChild(holdButton);
		primaryControls.AddChild(waitButton);
		primaryControls.AddChild(fortifyButton);
		primaryControls.AddChild(disbandButton);
		primaryControls.AddChild(goToButton);
		primaryControls.AddChild(exploreButton);
		primaryControls.AddChild(sentryButton);
		primaryControls.AddChild(sentryEnemyOnly);

		HBoxContainer specializedControls = GetNode<HBoxContainer>("SpecializedUnitControls");
		UnitControlButton load = new UnitControlButton("load", 7, 0, onButtonPressed);
		UnitControlButton unload = new UnitControlButton("unload", 0, 1, onButtonPressed);
		UnitControlButton pillage = new UnitControlButton("pillage", 2, 1, onButtonPressed);
		UnitControlButton bombard = new UnitControlButton("bombard", 3, 1, onButtonPressed);
		UnitControlButton autobombard = new UnitControlButton("autobombard", 3, 5, onButtonPressed);
		UnitControlButton fortress = new UnitControlButton("fortress", 0, 3, onButtonPressed);
		UnitControlButton barricade = new UnitControlButton("barricade", 4, 4, onButtonPressed);
		UnitControlButton mine = new UnitControlButton("mine", 1, 3, onButtonPressed);
		UnitControlButton irrigate = new UnitControlButton("irrigate", 2, 3, onButtonPressed);
		UnitControlButton chopForest = new UnitControlButton("chopForest", 3, 3, onButtonPressed);
		UnitControlButton chopJungle = new UnitControlButton("chopJungle", 4, 3, onButtonPressed);
		UnitControlButton plantForest = new UnitControlButton("plantForest", 5, 3, onButtonPressed);
		UnitControlButton clearDamage = new UnitControlButton("clearDamage", 6, 3, onButtonPressed);
		UnitControlButton automate = new UnitControlButton("automate", 7, 3, onButtonPressed);
		specializedControls.AddChild(load);
		specializedControls.AddChild(unload);
		specializedControls.AddChild(pillage);
		specializedControls.AddChild(bombard);
		specializedControls.AddChild(autobombard);
		specializedControls.AddChild(fortress);
		specializedControls.AddChild(barricade);
		specializedControls.AddChild(mine);
		specializedControls.AddChild(irrigate);
		specializedControls.AddChild(chopForest);
		specializedControls.AddChild(chopJungle);
		specializedControls.AddChild(plantForest);
		specializedControls.AddChild(clearDamage);
		specializedControls.AddChild(automate);
	}

	private void onButtonPressed(string buttonName)
	{
		EmitSignal(nameof(UnitButtonPressed), buttonName);
	}
	
	private void OnNoMoreAutoselectableUnits()
	{
		this.Visible = false;
	}
	
	private void OnNewUnitSelected(ParameterWrapper wrappedMapUnit)
	{
		this.Visible = true;
	}
}
