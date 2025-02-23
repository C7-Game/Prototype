using Godot;
using Serilog;

[GlobalClass]
public partial class PopupOverlay : HBoxContainer {

	private ILogger log = LogManager.ForContext<PopupOverlay>();

	[Signal] public delegate void UnitDisbandedEventHandler();
	[Signal] public delegate void QuitEventHandler();
	[Signal] public delegate void RetireEventHandler();
	[Signal] public delegate void BuildCityEventHandler(string name);
	[Signal] public delegate void HidePopupEventHandler();

	Control currentChild = null;

	[Export]
	private Control control;

	public enum PopupCategory {
		Advisor,
		Console,
		Info    //Sounds similar to the above, but lower-pitched in the second half
	}

	public void OnHidePopup() {
		// 1. enable mouse interaction with non-UI nodes
		MouseFilter = MouseFilterEnum.Pass;
		RemoveChild(currentChild);
		currentChild = null;
		Hide();

		// 2. enable mouse interactions with other UI elements
		setMouseFilter(control, MouseFilterEnum.Pass);

		// 3. enable clicking other UI elements
		control.ProcessMode = ProcessModeEnum.Inherit;
	}

	public bool ShowingPopup => currentChild is not null;

	public void PlaySound(AudioStreamWav wav) {
		AudioStreamPlayer player = GetNode<AudioStreamPlayer>("PopupSound");
		player.Stream = wav;
		player.Play();
	}

	private void setMouseFilter(Node n, MouseFilterEnum filter) {
		foreach (Node child in n?.GetChildren()) {
			setMouseFilter(child, filter);
		}
		if (n is Control control) {
			control.MouseFilter = filter;
		}
	}

	public void ShowPopup(Popup child, PopupCategory category) {
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

		// 1. prevent mouse interaction with non-UI elements (ie. the map)
		MouseFilter = MouseFilterEnum.Stop;

		// 2. prevent clicking other UI elements
		control.ProcessMode = ProcessModeEnum.Disabled;

		// 3. ignore all mouse input on other UI elements (ie. button color changes on hover)
		setMouseFilter(control, MouseFilterEnum.Ignore);

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
	public override void _UnhandledInput(InputEvent @event) {
		if (Visible && @event is InputEventKey eventKey && eventKey.Pressed) {
			// As I've added more shortcuts, I've realized checking all of them here could be irksome.
			// For now, I'm thinking it would make more sense to process or allow through the ones that should go through,
			// as most of the global ones should *not* go through here.
			GetViewport().SetInputAsHandled();
		}
	}
}
