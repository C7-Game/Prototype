using System.Collections.Generic;
using C7Engine;
using Godot;
using C7Engine.PalaceMinigame;

[Tool]
public partial class PalaceView : Control {
	[Export] public TextureRect background;
	[Export] public PalaceBuildingsLayer buildingsLayer;

	[Export] HBoxContainer switchButtonContainer;
	ButtonGroup switchButtonGroup = new();

	private TextureButton _close;

	public override void _Ready() {
		base._Ready();

		if (C7Settings.UseStandaloneMode()) {
			return;
		}

		background.Texture = TextureLoader.Load("screens.palace.background");

		Dictionary<string, Culture> cultures = ParsePalaceView();
		buildingsLayer.SetCultures(cultures);

		MouseFilter = MouseFilterEnum.Stop;

		_close = AdvisorUtils.CreateExitButton(background);
		_close.Pressed += () => { this.GetParent<GameViews>().Hide(); };

		foreach (Culture culture in cultures.Values) {
			AddSwitchButton(culture);
		}

		switchButtonContainer.GetChild<TextureButton>(0).ButtonPressed = true;
	}

	private Dictionary<string, Culture> ParsePalaceView() {
		string configPath = Util.Civ3MediaPath("Text/PalaceView.txt");
		ConfigParser parser = new();

		return parser.Parse(configPath);
	}

	private void AddSwitchButton(Culture culture) {
		var bt = culture.ButtonTextures;

		TextureButton button = new() {
			TextureNormal = TextureLoader.LoadByPath(bt.Normal),
			TexturePressed = TextureLoader.LoadByPath(bt.Pressed),
			TextureHover = TextureLoader.LoadByPath(bt.Hover),
			ButtonGroup = switchButtonGroup,
			ToggleMode = true,
		};
		button.Pressed += () => {
			buildingsLayer.ActivateCulture(culture);
		};

		switchButtonContainer.AddChild(button);
	}

	public override void _Process(double delta) {
		if (Engine.IsEditorHint()) return;
		QueueRedraw();
	}

	public void ShowView() {
		Show();
	}
}
