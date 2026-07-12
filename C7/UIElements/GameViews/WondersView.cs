using C7Engine;
using C7GameData;
using Godot;

[GlobalClass]
[Tool]
public partial class WondersView : Control {

	[Export] public TextureRect background;

	private TextureButton _close;

	public WondersView() {
		MouseFilter = MouseFilterEnum.Stop;
	}

	public override void _Ready() {
		this.CreateUI();
	}

	private void CreateUI() {
		background.Texture = TextureLoader.Load("screens.wonders.background");

		_close = AdvisorUtils.CreateExitButton(background);
		_close.Pressed += () => { this.GetParent<GameViews>().Hide(); };

		AdvisorUtils.CreateAdvisorTitle(background, background.Texture.GetWidth(), "WONDERS OF THE WORLD");
	}

	public void ShowView() {
		Show();

		EngineStorage.ReadGameData((GameData gameData) => {
			Player player = gameData.GetFirstHumanPlayer();

		});
	}
}
