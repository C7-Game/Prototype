using System.Numerics;
using C7Engine;
using C7GameData;
using Godot;

[GlobalClass]
[Tool]
public partial class ForeignAdvisor : Control  {

	[Export] public TextureRect background;

	private TextureButton _close;
	private TextureRect _advisorHead;
	private TextureButton _dialogBox;
	private Label _dialogBoxLabel;

	public override void _Ready() {
		this.CreateUI();
	}

	private void CreateUI() {
		background.Texture = TextureLoader.Load("advisors.foreign.background");

		_advisorHead = AdvisorUtils.CreateAdvisorHead(background, AdvisorHead.Advisor.Foreign);
		_close = AdvisorUtils.CreateExitButton(background);
		_close.Pressed += () => {  this.GetParent<Advisors>().Hide(); };
		(_dialogBox, _dialogBoxLabel) = AdvisorUtils.CreateAdvisorDialogBox(background);
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
