using System.Collections.Generic;
using C7Engine;
using Godot;
using C7Engine.PalaceMinigame;

[Tool]
public partial class PalaceView : Control {
	[Export] public TextureRect background;
	[Export] public TextureRect buildingsLayer;

	private TextureButton _close;

	public override void _Ready() {
		base._Ready();

		if (C7Settings.UseStandaloneMode()) {
			return;
		}

		background.Texture = TextureLoader.Load("screens.palace.background");

		MouseFilter = MouseFilterEnum.Stop;

		_close = AdvisorUtils.CreateExitButton(background);
		_close.Pressed += () => { this.GetParent<GameViews>().Hide(); };
	}

	public override void _Process(double delta) {
		if (Engine.IsEditorHint()) return;
		QueueRedraw();
	}

	public void ShowView() {
		Show();
	}
}
