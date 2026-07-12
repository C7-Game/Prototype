using C7Engine;
using C7GameData;
using Godot;

[GlobalClass]
[Tool]
public partial class VictoryStatusView : Control {

	[Export] public TextureRect background;

	private TextureButton _close;

	public VictoryStatusView() {
		MouseFilter = MouseFilterEnum.Stop;
	}

	public override void _Ready() {
		this.CreateUI();
	}

	private void CreateUI() {
		background.Texture = TextureLoader.Load("screens.standing.victory_status.background");
		var histogramTexture = TextureLoader.Load("screens.standing.histogram.background");

		_close = AdvisorUtils.CreateExitButton(background);
		_close.Pressed += () => { this.GetParent<GameViews>().Hide(); };

		AdvisorUtils.CreateAdvisorTitle(background, background.Texture.GetWidth(), "VICTORY STATUS");
	}

	public void ShowView() {
		Show();

		EngineStorage.ReadGameData((GameData gameData) => {
			Player player = gameData.GetFirstHumanPlayer();

		});
	}
}
