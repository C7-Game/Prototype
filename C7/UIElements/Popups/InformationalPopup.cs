using Godot;
using System;
using System.Diagnostics;
using C7GameData;
using Serilog;

// A generic popup for some sort of information.
public partial class InformationalPopup : Popup {
	string message;
	AdvisorHead.Advisor advisor;
	AdvisorHead.Mood mood;

	public InformationalPopup(string message, AdvisorHead.Advisor advisor = AdvisorHead.Advisor.Foreign, AdvisorHead.Mood mood = AdvisorHead.Mood.Happy) {
		alignment = BoxContainer.AlignmentMode.End;
		margins = new Margins(right: 10);
		this.message = message;
		this.advisor = advisor;
		this.mood = mood;
	}

	public override void _Ready() {
		base._Ready();

		int width = 430;
		int height = 230;

		TextureRect advisorHead = new();
		advisorHead.Texture = AdvisorHead.GetPopupImage(advisor, mood, eraIndex: 0);
		advisorHead.SetPosition(new Vector2(275, 0));
		AddChild(advisorHead);

		AddTexture(width, height);
		AddBackground(width, height - 110, 110);
		AddHeader("Foreign Advisor", 120);

		Label messageLabel = new();
		messageLabel.Text = message;
		messageLabel.SetPosition(new Vector2(25, 160));
		AddChild(messageLabel);

		AddConfirmButton(new Vector2(width - 40, height - 40), () => {
			GetParent().EmitSignal(PopupOverlay.SignalName.HidePopup);
		});
	}
}
