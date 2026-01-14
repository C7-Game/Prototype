using Godot;

/// <summary>
/// A class for detecting double-clicks.
///
/// To differentiate between single and double clicks, this class uses
/// a timer-based detection mechanism. When a click occurs, it waits
/// for a short period (DOUBLE_CLICK_DELAY) to check if another click
/// follows. If a second click happens, it emits a double-click
/// signal; otherwise, it emits a single-click signal.
///
/// Although Godot provides built-in double-click detection, it does
/// not allow handling single clicks separately. For example, with
/// Godot’s built-in detection, clicking on a city that contains a
/// unit would both zoom to the city and select the unit.
/// </summary>
[GlobalClass]
public partial class DoubleClickHandler : Node {
	int leftMouseButtonClickCount = 0;
	InputEventMouseButton lastEventMouseButton;
	const double DOUBLE_CLICK_DELAY = 0.2;

	Timer timer = new();

	[Signal] public delegate void SingleClickEventHandler(InputEventMouseButton eventMouseButton);
	[Signal] public delegate void DoubleClickEventHandler(InputEventMouseButton eventMouseButton);

	public override void _Ready() {
		AddChild(timer);
		timer.OneShot = true;
		timer.Timeout += OnTimeout;
	}

	public void Accept(InputEventMouseButton eventMouseButton) {
		lastEventMouseButton = eventMouseButton;
		++leftMouseButtonClickCount;

		timer.Stop();

		if (leftMouseButtonClickCount >= 2) {
			EmitSignal(SignalName.DoubleClick, lastEventMouseButton);
			leftMouseButtonClickCount = 0;
		} else {
			timer.Start(DOUBLE_CLICK_DELAY);
		}
	}

	private void OnTimeout() {
		EmitSignal(SignalName.SingleClick, lastEventMouseButton);
		leftMouseButtonClickCount = 0;
	}
}
