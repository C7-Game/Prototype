using Godot;
using System;

public class PopupOverlay : HBoxContainer
{
	[Signal] public delegate void UnitDisbanded();
	
	private void HidePopup()
	{
		this.Hide();
	}

	public void PlaySound(AudioStreamSample wav)
	{
		AudioStreamPlayer player = GetNode<AudioStreamPlayer>("PopupSound");
		player.Stream = wav;
		player.Play();
	}
}


