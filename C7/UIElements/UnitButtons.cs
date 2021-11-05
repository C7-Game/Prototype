using Godot;
using System;

public class UnitButtons : VBoxContainer
{

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//You can hide buttons like this.  This will come in handy later!
		//Remember to re-calc the margin after hiding/unhiding buttons, as that may affect the width.
		//this.GetNode<FortifyButton>("PrimaryUnitControls/FortifyButton").Hide();

		HBoxContainer primaryControls = GetNode<HBoxContainer>("PrimaryUnitControls");
		UnitControlButton holdButton = new UnitControlButton("hold", 0, 0);
		UnitControlButton waitButton = new UnitControlButton("waitButton", 32, 0);
		UnitControlButton fortifyButton = new UnitControlButton("disband", 64, 0);
		UnitControlButton disbandButton = new UnitControlButton("disband", 96, 0);
		UnitControlButton goToButton = new UnitControlButton("goTo", 128, 0);
		UnitControlButton exploreButton = new UnitControlButton("explore", 160, 0);
		UnitControlButton sentryButton = new UnitControlButton("sentry", 192, 0);
		UnitControlButton sentryEnemyOnly = new UnitControlButton("sentryEnemyOnly", 224, 0);
		primaryControls.AddChild(holdButton);
		primaryControls.AddChild(waitButton);
		primaryControls.AddChild(fortifyButton);
		primaryControls.AddChild(disbandButton);
		primaryControls.AddChild(goToButton);
		primaryControls.AddChild(exploreButton);
		primaryControls.AddChild(sentryButton);
		primaryControls.AddChild(sentryEnemyOnly);

		GD.Print("RectSize: " + this.RectSize);
		
		//Button count should be calculated dynamically and be the max # of visible buttons in any row
		//For now I've set it to 8 because I'm making other things better first.
		int buttonCount = 8;
		this.MarginLeft = -1 * 16 * buttonCount;
	}
}
