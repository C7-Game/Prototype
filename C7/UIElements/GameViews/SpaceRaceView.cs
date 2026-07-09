using C7Engine;
using C7GameData;
using Godot;

[GlobalClass]
[Tool]
public partial class SpaceRaceView : Control {

	[Export] public TextureRect background;

	private TextureButton _close;

	public SpaceRaceView() {
		MouseFilter = MouseFilterEnum.Stop;
	}

	public override void _Ready() {
		this.CreateUI();
	}

	private void CreateUI() {
		background.Texture = TextureLoader.Load("screens.space_race.background");

		_close = AdvisorUtils.CreateExitButton(background);
		_close.Pressed += () => { this.GetParent<GameViews>().Hide(); };
	}

	public void ShowView() {
		Show();

		EngineStorage.ReadGameData((GameData gameData) => {
			Player player = gameData.GetFirstHumanPlayer();

		});
	}
}
