using System.Linq;
using C7Engine;
using Godot;

public partial class UIOverlayController : Node {
	[Export]
	private PopupOverlay popupOverlay;
	[Export]
	private CityScreen cityScreen;
	[Export]
	private Advisors advisor;
	[Export]
	private Diplomacy diplomacy;
	[Export]
	private Control palaceScene;
	[Export]
	private MessageConsumer messageConsumer;

	public override void _Ready() {
		messageConsumer.messageConsumed += HandleEngineMessage;
	}

	public bool IsOverlayVisible() {
		foreach (Node child in GetChildren()) {
			if (child is CanvasItem canvasItem && canvasItem.Visible)
				return true;
		}

		return false;
	}

	public override void _UnhandledInput(InputEvent @event) {
		if (@event is InputEventKey eventKeyDown && eventKeyDown.Pressed) {
			if (eventKeyDown.Keycode == Godot.Key.F1) {
				advisor.ShowAdvisor(Advisors.Type.Domestic);
			}
			if (eventKeyDown.Keycode == Godot.Key.F3) {
				advisor.ShowAdvisor(Advisors.Type.Military);
			}
			if (eventKeyDown.Keycode == Godot.Key.F6) {
				advisor.ShowAdvisor(Advisors.Type.Scientific);
			}
			if (eventKeyDown.Keycode == Godot.Key.F9) {
				palaceScene.Show();
			}
			if (eventKeyDown.Keycode == Godot.Key.Escape && IsOverlayVisible()) {
				HideOverlays();
				GetViewport().SetInputAsHandled();
			}
		}
	}

	private void HideOverlays() {
		foreach (var child in GetChildren().OfType<CanvasItem>()) {
			child.Hide();
		}
	}

	private void HandleEngineMessage(MessageToUI msg) {
		switch (msg) {
			case MsgCivilizationDestroyed mCivD:
				popupOverlay.ShowPopup(new CivilizationDestroyed(mCivD.civilization), PopupOverlay.PopupCategory.Advisor);
				break;
			case MsgShowMilitaryAdvisorPopup mSMAP:
				if (!popupOverlay.Visible) {
					popupOverlay.ShowPopup(
						new InformationalPopup(mSMAP.message, AdvisorHead.Advisor.Military, mSMAP.happy ? AdvisorHead.Mood.Happy : AdvisorHead.Mood.Angry),
						PopupOverlay.PopupCategory.Advisor);
				}
				break;
			case MsgShowScienceAdvisor mSSA:
				advisor.ShowAdvisor(Advisors.Type.Scientific);
				break;
			case MsgUpdateUiAfterDomesticChange mUUASC:
				advisor.ShowAdvisor(Advisors.Type.Domestic);
				break;
			case MsgShowTradeOffer mSTO:
				diplomacy.ShowDealScreenForPlayer(
					mSTO.humanPlayer.id, mSTO.aiPlayer.id,
					humanGives: mSTO.aiWant,
					humanWants: mSTO.aiGive);
				break;
			case MsgDisplayHurryProductionPopup mDHPP:
				if (mDHPP.details.errorMessage != null) {
					popupOverlay.ShowPopup(
						new InformationalPopup(mDHPP.details.errorMessage),
						PopupOverlay.PopupCategory.Advisor);
				} else {
					popupOverlay.ShowPopup(
						new ConfirmationPopup(message: mDHPP.details.costMessage,
												yesText: "Yes I'm sure!",
												noText: "Maybe you're right. Nevermind.",
												yesAction: () => {
													new MsgDoHurryProduction(mDHPP.city).send();
												}),
						PopupOverlay.PopupCategory.Advisor);
				}
				break;
			case MsgWarDeclaration mWD:
				popupOverlay.ShowPopup(
					new InformationalPopup($"The {mWD.aggressor.civilization.noun} declared war on the {mWD.opponent.civilization.noun}"),
					PopupOverlay.PopupCategory.Advisor);
				break;
		}
	}
}
