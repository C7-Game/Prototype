using Godot;
using System;

public class PopupOverlay : HBoxContainer
{
	[Signal] public delegate void UnitDisbanded();
	
	private void HidePopup()
	{
		this.Hide();
	}
}


