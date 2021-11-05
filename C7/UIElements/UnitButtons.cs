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
		
		this.MarginLeft = -1 * (this.RectSize.x/2.0f);
	}
}
