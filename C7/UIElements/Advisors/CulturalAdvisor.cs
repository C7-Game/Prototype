using System.Numerics;
using C7Engine;
using C7GameData;
using Godot;

[GlobalClass]
[Tool]
public partial class CulturalAdvisor : Control {

	[Export] public TextureRect background;

	private TextureButton _close;
	private TextureRect _advisorHead;
	private TextureButton _dialogBox;
	private Label _dialogBoxLabel;

	public CulturalAdvisor() {
		MouseFilter = MouseFilterEnum.Stop;
	}

	public override void _Ready() {
		this.CreateUI();
	}

	private void CreateUI() {
		background.Texture = TextureLoader.Load("advisors.culture.background");

		_advisorHead = AdvisorUtils.CreateAdvisorHead(background, AdvisorHead.Advisor.Culture);
		_close = AdvisorUtils.CreateExitButton(background);
		_close.Pressed += () => { this.GetParent<Advisors>().Hide(); };
		(_dialogBox, _dialogBoxLabel) = AdvisorUtils.CreateAdvisorDialogBox(background);

		AdvisorUtils.CreateAdvisorTitle(background, background.Texture.GetWidth(), "CULTURAL ADVISOR");
	}

	public void ShowAdvisor() {
		Show();

		EngineStorage.ReadGameData((GameData gameData) => {
			Player player = gameData.GetFirstHumanPlayer();

			// TODO: Choose advisor head
			_advisorHead.Texture = AdvisorHead.GetPopupImage(AdvisorHead.Advisor.Culture, AdvisorHead.Mood.Happy, player.EraIndex());
		});
	}
}
