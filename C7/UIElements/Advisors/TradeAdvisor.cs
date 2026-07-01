using C7Engine;
using C7GameData;
using Godot;

public interface IAdvisor {
	void ShowAdvisor();
}

[GlobalClass]
[Tool]
public partial class TradeAdvisor : Control, IAdvisor  {

	[Export] public TextureRect background;
	[Export] public TextureButton close;

	private TextureRect advisorHead = new();
	private Label DialogBoxAdvise = new();

	public override void _Ready() {
		this.CreateUI();
	}

	protected void CreateUI() {
		background.Texture = TextureLoader.Load("advisors.trade.background");

		advisorHead.Texture = AdvisorHead.GetPopupImage(AdvisorHead.Advisor.Domestic, AdvisorHead.Mood.Happy, eraIndex: 0);
		advisorHead.SetPosition(new Vector2(851, 0));
		background.AddChild(advisorHead);

		ImageTexture DialogBoxTexture = TextureLoader.Load("advisors.dialog_box");
		TextureButton DialogBox = new TextureButton();
		DialogBox.TextureNormal = DialogBoxTexture;
		DialogBox.SetPosition(new Vector2(806, 110));
		background.AddChild(DialogBox);

		//TODO: Multi-line capabilities
		DialogBoxAdvise.Text = "You are running OpenCiv3!";
		DialogBoxAdvise.SetPosition(new Vector2(815, 119));
		background.AddChild(DialogBoxAdvise);

		TextureLoader.SetButtonTextures(close, "ui.exit");
		close.Pressed += () => {
			GetParent<Advisors>().Hide();
		};
	}

	public void ShowAdvisor() {
		Show();

		EngineStorage.ReadGameData((GameData gameData) => {
			Player player = gameData.GetFirstHumanPlayer();

			// TODO: Choose advisor head
			advisorHead.Texture = AdvisorHead.GetPopupImage(AdvisorHead.Advisor.Trade, AdvisorHead.Mood.Happy, player.EraIndex());
		});
	}
}
