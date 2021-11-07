using Godot;
using System;

public class UnitButtons : VBoxContainer
{

	[Signal] public delegate void UnitButtonPressed();

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

		//All right, what about actions?  Clicking on the buttons should do stuff, beyond changing the color of the icon.
		//This should probably be shoved into the UnitControlButton, but I want to write one out here to see what it looks like.
		//Also, where should the events go?  They need to call the engine, but should they go via Game.cs?  Probably not forever, but
		//I am not sure where they should go.  And it might differ per button, e.g. Disband will give a confirmation pop-up, bu
		//most would not.
		holdButton.Connect("pressed", this, "OnHoldButtonPressed");
		
		//Button count should be calculated dynamically and be the max # of visible buttons in any row
		//For now I've set it to 8 because I'm making other things better first.
		int buttonCount = 8;
		this.MarginLeft = -1 * 16 * buttonCount;
	}

	public void OnHoldButtonPressed()
	{
		//Okay, so as I'm looking at this, it seems kind clear that what should happen is it emits UnitButtonPressed with its button
		//name.  Maybe.  But it does make some sense.
		//It also avoids having to have signals for every button.  Downside, listeners will have to do a check for whether the button
		//pressed is the one they are looking for.  But it seems like an acceptable trade-off on the surface.
		EmitSignal("UnitButtonPressed", "hold");
	}
	
	private void OnNoMoreAutoselectableUnits()
	{
		this.Visible = false;
	}
}
