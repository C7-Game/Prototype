using C7Engine;
using C7GameData;
using Godot;
using System;

[GlobalClass]
[Tool]
public partial class MilitaryAdvisor : Control {

	[Export] public TextureRect background;

	private TextureButton _close;
	private TextureRect _advisorHead;
	private TextureButton _dialogBox;
	private Label _dialogBoxLabel;

	private Label _totalUnitsLabel = new();
	private Label _allowedUnitsLabel = new();
	private Label _unitSupportCostLabel = new();

	public MilitaryAdvisor() {
		MouseFilter = MouseFilterEnum.Stop;
	}

	public override void _Ready() {
		this.CreateUI();
	}

	private void CreateUI() {
		background.Texture = TextureLoader.Load("advisors.military.background");

		_advisorHead = AdvisorUtils.CreateAdvisorHead(background, AdvisorHead.Advisor.Military);
		_close = AdvisorUtils.CreateExitButton(background);
		_close.Pressed += () => { this.GetParent<Advisors>().Hide(); };
		(_dialogBox, _dialogBoxLabel) = AdvisorUtils.CreateAdvisorDialogBox(background);

		AdvisorUtils.CreateAdvisorTitle(background, background.Texture.GetWidth(), "MILITARY ADVISOR");

		background.AddChild(_totalUnitsLabel);
		_totalUnitsLabel.SetPosition(new Vector2(0, 90));
		background.AddChild(_allowedUnitsLabel);
		_allowedUnitsLabel.SetPosition(new Vector2(0, 139));
		background.AddChild(_unitSupportCostLabel);
		_unitSupportCostLabel.SetPosition(new Vector2(0, 188));
	}

	public void ShowAdvisor() {
		Show();

		EngineStorage.ReadGameData((GameData gameData) => {
			Player player = gameData.GetFirstHumanPlayer();
			var (totalUnits, allowedUnits, unitSupportCost) = player.TotalUnitsAllowedUnitsAndSupportCost();

			_totalUnitsLabel.SetTextAndCenterLabel($"Total Units\n{totalUnits}");
			_totalUnitsLabel.Position += new Vector2(-50, 0);
			_allowedUnitsLabel.SetTextAndCenterLabel($"Allowed Units\n{allowedUnits}");
			_allowedUnitsLabel.Position += new Vector2(-50, 0);
			_unitSupportCostLabel.SetTextAndCenterLabel($"Unit Support Cost\n{unitSupportCost} gold/turn");
			_unitSupportCostLabel.Position += new Vector2(-50, 0);

			_advisorHead.Texture =
				AdvisorHead.GetPopupImage(AdvisorHead.Advisor.Military, AdvisorHead.Mood.Happy, player.EraIndex());
		});
	}
}
