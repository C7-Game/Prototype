using Godot;
using System;
using System.Diagnostics;
using C7GameData;
using Serilog;

public partial class WarConfirmation : Popup {
	Player opponent;
	Action action;

	public WarConfirmation(Player opponent, Action action) {
		alignment = BoxContainer.AlignmentMode.End;
		margins = new Margins(right: 10);
		this.opponent = opponent;
		this.action = action;
	}

	public override void _Ready() {
		base._Ready();

		AddTexture(530, 320);

		TextureRect advisorHead = new();
		advisorHead.Texture = AdvisorHead.GetPopupImage(AdvisorHead.Advisor.Foreign, AdvisorHead.Mood.Surprised, eraIndex: 0);
		//Appears at 400, 110 in game, but leftmost 25px are transparent with default graphics
		advisorHead.SetPosition(new Vector2(375, 0));
		AddChild(advisorHead);

		AddBackground(530, 210, 110);
		AddHeader("Foreign Advisor", 120);

		Label warningMessage = new Label();
		warningMessage.Text = $"This will cause war with the {opponent.civilization.noun}.\nAre you sure?";
		warningMessage.SetPosition(new Vector2(25, 170));
		AddChild(warningMessage);

		AddButton("I said DO IT!", 215, () => {
			action();
			GetParent().EmitSignal(PopupOverlay.SignalName.HidePopup);
		});
		AddButton("No. You're right, perhaps we should reconsider.", 245, cancel);
	}

	private void cancel() {
		GetParent().EmitSignal(PopupOverlay.SignalName.HidePopup);
	}
}
