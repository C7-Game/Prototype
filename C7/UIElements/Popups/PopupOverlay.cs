using Godot;
using ConvertCiv3Media;
using System;
using System.Diagnostics;
using Serilog;

public partial class PopupOverlay : HBoxContainer
{

	private ILogger log = LogManager.ForContext<PopupOverlay>();

	[Signal] public delegate void UnitDisbandedEventHandler();
	[Signal] public delegate void QuitEventHandler();
	[Signal] public delegate void BuildCityEventHandler(string name);
	[Signal] public delegate void HidePopupEventHandler();

	Control currentChild = null;

	public const string NodePath = "/root/C7Game/CanvasLayer/PopupOverlay";

	public enum PopupCategory {
		Advisor,
		Console,
		Info	//Sounds similar to the above, but lower-pitched in the second half
	}

	public override void _Ready()
	{
		base._Ready();
	}

	private void OnHidePopup()
	{
		log.Debug("Hiding popup");
		RemoveChild(currentChild);
		Hide();
	}

	public void PlaySound(AudioStreamWav wav)
	{
		AudioStreamPlayer player = GetNode<AudioStreamPlayer>("PopupSound");
		player.Stream = wav;
		player.Play();
	}

	public void ShowPopup(Popup child, PopupCategory category)
	{
		if (child == null) // not necessary if we don't pass null?
		{
			log.Error("Received request to show null popup");
			return;
		}

		if (child == currentChild) {
			return; // already being displayed
		}

		Alignment = child.alignment;
		OffsetTop = child.margins.top;
		OffsetBottom = child.margins.bottom;
		OffsetLeft = child.margins.left;
		OffsetRight = child.margins.right;

		AddChild(child);
		currentChild = child;

		string soundFile = "";
		switch (category)
		{
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
				log.Error("Invalid popup category");
				break;
		}
		AudioStreamWav wav = Util.LoadWAVFromDisk(Util.Civ3MediaPath(soundFile));
		Visible = true;
		PlaySound(wav);
	}

	/**
	 * N.B. Some popups should react to certain keys, e.g. the Build City popup should close without building if you
	 * press escape.  Those popups will have to implement this functionality.
	 *
	 * If we find that the majority of popups should close on Escape, we may want to make that the default,
	 * but so far, 2 out of 3 popups do not close on escape.
	 **/
	public override void _UnhandledInput(InputEvent @event)
	{
		if (this.Visible) {
			if (@event is InputEventKey eventKey)
			{
				//As I've added more shortcuts, I've realized checking all of them here could be irksome.
				//For now, I'm thinking it would make more sense to process or allow through the ones that should go through,
				//as most of the global ones should *not* go through here.
				if (eventKey.Pressed)
				{
					GetViewport().SetInputAsHandled();
				}
			}
		}
	}
}
