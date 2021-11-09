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
		UnitControlButton waitButton = new UnitControlButton("waitButton", 32, 0, onButtonPressed);
		UnitControlButton fortifyButton = new UnitControlButton("fortify", 64, 0, onButtonPressed);
		UnitControlButton disbandButton = new UnitControlButton("disband", 96, 0, onButtonPressed);
		UnitControlButton goToButton = new UnitControlButton("goTo", 128, 0, onButtonPressed);
		UnitControlButton exploreButton = new UnitControlButton("explore", 160, 0, onButtonPressed);
		UnitControlButton sentryButton = new UnitControlButton("sentry", 192, 0, onButtonPressed);
		UnitControlButton sentryEnemyOnly = new UnitControlButton("sentryEnemyOnly", 224, 0, onButtonPressed);
		primaryControls.AddChild(holdButton);
		primaryControls.AddChild(waitButton);
		primaryControls.AddChild(fortifyButton);
		primaryControls.AddChild(disbandButton);
		primaryControls.AddChild(goToButton);
		primaryControls.AddChild(exploreButton);
		primaryControls.AddChild(sentryButton);
		primaryControls.AddChild(sentryEnemyOnly);
		
		//Button count should be calculated dynamically and be the max # of visible buttons in any row
		//For now I've set it to 8 because I'm making other things better first.
		int buttonCount = 8;
		this.MarginLeft = -1 * 16 * buttonCount;
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
