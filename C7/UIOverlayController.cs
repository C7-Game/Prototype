using Godot;

class UIOverlayController : Node {
	[Export]
	private PopupOverlay popupOverlay;
	[Export]
	private CityScreen cityScreen;
	[Export]
	private Advisors advisor;
	[Export]
	private Diplomacy diplomacy;
	[Export]
	private Control palaceScene;

	public bool IsOverlayVisible() {
		return popupOverlay.Visible || cityScreen.Visible || advisor.Visible || diplomacy.Visible || palaceScene.Visible;
	}

	public override void _UnhandledInput(InputEvent @event) {
		if (@event is InputEventKey eventKeyDown && eventKeyDown.Pressed) {
			if (eventKeyDown.Keycode == Godot.Key.F1) {
				advisor.ShowAdvisor(Advisors.Type.Domestic);
			}
			if (eventKeyDown.Keycode == Godot.Key.F3) {
				advisor.ShowAdvisor(Advisors.Type.Military);
			}
			if (eventKeyDown.Keycode == Godot.Key.F6) {
				advisor.ShowAdvisor(Advisors.Type.Scientific);
			}
			if (eventKeyDown.Keycode == Godot.Key.F9) {
				palaceScene.Show();
			}
			if (eventKeyDown.Keycode == Godot.Key.Escape) {
				GetViewport().SetInputAsHandled();
			}
		}
	}
}
