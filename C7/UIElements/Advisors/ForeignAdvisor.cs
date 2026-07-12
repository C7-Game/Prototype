using System.Numerics;
using C7Engine;
using C7GameData;
using Godot;
using Vector2 = Godot.Vector2;

[GlobalClass]
[Tool]
public partial class ForeignAdvisor : Control {

	[Export] public TextureRect background;

	private TextureRect treaties = new();
	private TextureButton treatiesButton = new();
	private TextureRect trades = new();
	private TextureButton tradesButton = new();
	private TextureRect details = new();
	private TextureButton detailsButton = new();

	private TextureButton _close;
	private TextureRect _advisorHead;
	private TextureButton _dialogBox;
	private Label _dialogBoxLabel;

	public ForeignAdvisor() {
		MouseFilter = MouseFilterEnum.Stop;
	}

	public override void _Ready() {
		this.CreateUI();
	}

	private void CreateUI() {
		background.Texture = TextureLoader.Load("advisors.foreign.background");

		_advisorHead = AdvisorUtils.CreateAdvisorHead(background, AdvisorHead.Advisor.Foreign);
		_close = AdvisorUtils.CreateExitButton(background);
		_close.Pressed += () => { this.GetParent<Advisors>().Hide(); };
		(_dialogBox, _dialogBoxLabel) = AdvisorUtils.CreateAdvisorDialogBox(background);

		// Tabs
		Vector2 tabsPosition = new(740, 300);
		CreateTabs(tabsPosition);
		CreateTabHeaders(tabsPosition);

		AdvisorUtils.CreateAdvisorTitle(background, background.Texture.GetWidth(), "FOREIGN ADVISOR");
	}

	private void CreateTabs(Vector2 tabsPosition) {
		treaties.Texture = TextureLoader.Load("advisors.foreign.navigation.treaties");
		treaties.SetPosition(tabsPosition);
		background.AddChild(treaties);

		trades.Texture = TextureLoader.Load("advisors.foreign.navigation.trades");
		trades.SetPosition(tabsPosition);
		background.AddChild(trades);

		details.Texture = TextureLoader.Load("advisors.foreign.navigation.details");
		details.SetPosition(tabsPosition);
		background.AddChild(details);

		treaties.Show();
		trades.Hide();
		details.Hide();
	}

	private void CreateTabHeaders(Vector2 tabsPosition) {
		Vector2 buttonSize = new(74, 18);
		Vector2 xShift = new(buttonSize.X + 3, 0);

		treatiesButton.SetSize(buttonSize);
		treatiesButton.Pressed += () => { treaties.Show(); trades.Hide(); details.Hide(); };
		treatiesButton.SetPosition(tabsPosition);
		background.AddChild(treatiesButton);

		tradesButton.SetSize(buttonSize);
		tradesButton.Pressed += () => { treaties.Hide(); trades.Show(); details.Hide(); };
		tradesButton.SetPosition(tabsPosition + xShift);
		background.AddChild(tradesButton);

		detailsButton.SetSize(buttonSize);
		detailsButton.Pressed += () => { treaties.Hide(); trades.Hide(); details.Show(); };
		detailsButton.SetPosition(tabsPosition + xShift + xShift);
		background.AddChild(detailsButton);
	}

	public void ShowAdvisor() {
		Show();

		EngineStorage.ReadGameData((GameData gameData) => {
			Player player = gameData.GetFirstHumanPlayer();

			// TODO: Choose advisor head
			_advisorHead.Texture = AdvisorHead.GetPopupImage(AdvisorHead.Advisor.Foreign, AdvisorHead.Mood.Happy, player.EraIndex());
		});
	}
}
