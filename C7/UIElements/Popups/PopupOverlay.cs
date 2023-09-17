using Godot;
using Serilog;

public partial class PopupOverlay : HBoxContainer
{

	private ILogger log = LogManager.ForContext<PopupOverlay>();

	[Signal] public delegate void UnitDisbandedEventHandler();
	[Signal] public delegate void QuitEventHandler();
	[Signal] public delegate void BuildCityEventHandler(string name);
	[Signal] public delegate void HidePopupEventHandler();

	Control currentChild = null;

	public static readonly string NodePath = "/root/C7Game/CanvasLayer/PopupOverlay";

	public enum PopupCategory {
		Advisor,
		Console,
		Info	//Sounds similar to the above, but lower-pitched in the second half
	}

	public void OnHidePopup()
	{
		RemoveChild(currentChild);
		currentChild = null;
		Hide();
	}

	public bool ShowingPopup => currentChild is not null;

	public void PlaySound(AudioStreamWav wav)
	{
		AudioStreamPlayer player = GetNode<AudioStreamPlayer>("PopupSound");
		player.Stream = wav;
		player.Play();
	}

	public void ShowPopup(Popup child, PopupCategory category)
	{
		if (child is null) {
			// not necessary if we don't pass null?
			log.Error("Received request to show null popup");
			return;
		}

		Alignment = child.alignment;
		OffsetTop = child.margins.top;
		OffsetBottom = child.margins.bottom;
		OffsetLeft = child.margins.left;
		OffsetRight = child.margins.right;

		AddChild(child);
		currentChild = child;

		string soundFile = category switch {
			PopupCategory.Advisor => "Sounds/PopupAdvisor.wav",
			PopupCategory.Console => "Sounds/PopupConsole.wav",
			PopupCategory.Info => "Sounds/PopupInfo.wav",
			_ => "",
		};
		if (soundFile == "") {
			log.Error("Invalid popup category");
		}
		AudioStreamWav wav = Util.LoadWAVFromDisk(Util.Civ3MediaPath(soundFile));
		Show();
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
