using C7Engine;
using C7GameData;
using Godot;

[GlobalClass]
[Tool]
public partial class DemographicsView : Control {

	[Export] public TextureRect background;

	private TextureButton _close;

	public DemographicsView() {
		MouseFilter = MouseFilterEnum.Stop;
	}

	public override void _Ready() {
		this.CreateUI();
	}

	private void CreateUI() {
		background.Texture = TextureLoader.Load("screens.demographics.background");

		_close = AdvisorUtils.CreateExitButton(background);
		_close.Pressed += () => { this.GetParent<GameViews>().Hide(); };

		var title = AdvisorUtils.CreateAdvisorTitle(background, background.Texture.GetWidth(),
			" TOP 5 CITIES    DEMOGRAPHICS");
		title.Position += new Vector2(0, 10);
	}

	public void ShowView() {
		Show();

		EngineStorage.ReadGameData((GameData gameData) => {
			Player player = gameData.GetFirstHumanPlayer();

		});
	}
}
