using Godot;
using System;

public class PopupOverlay : HBoxContainer
{
	[Signal] public delegate void UnitDisbanded();
	Control currentChild = null;

	public enum PopupCategory {
		Advisor,
		Console,
		Info
	}
	
	private void HidePopup()
	{
		this.RemoveChild(currentChild);
		this.Hide();
	}

	public void PlaySound(AudioStreamSample wav)
	{
		AudioStreamPlayer player = GetNode<AudioStreamPlayer>("PopupSound");
		player.Stream = wav;
		player.Play();
	}

	public void ShowPopup(string dialogType, PopupCategory popupCategory)
	{
		if (dialogType.Equals("disband")) {
			DisbandConfirmation dbc = new DisbandConfirmation();
			AddChild(dbc);
			currentChild = dbc;
		}
		
		if (currentChild != null) {
			string soundFile = "";
			switch(popupCategory) {
				case PopupCategory.Advisor:
					soundFile = "Sounds/PopupAdvisor.wav";
					break;
				case PopupCategory.Console:
					soundFile = "Sounds/PopupConsole.wav";
					break;
				case PopupCategory.Info:
					soundFile = "Sounds/PopupInfo.wav";
					break;
				default:
					GD.PrintErr("Invalid popup category");
					break;
			}
			AudioStreamSample wav = Util.LoadWAVFromDisk(Util.Civ3MediaPath(soundFile));
			this.Visible = true;
			PlaySound(wav);
		}
		else {
			GD.PrintErr("Received request to show invalid dialog type " + dialogType);
		}
	}
}


