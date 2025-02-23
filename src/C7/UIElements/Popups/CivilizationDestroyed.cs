using Godot;
using System;
using System.Diagnostics;
using C7GameData;
using Serilog;

public partial class CivilizationDestroyed : Popup {
	string civNoun = "";

	public CivilizationDestroyed(Civilization civ) {
		alignment = BoxContainer.AlignmentMode.End;
		margins = new Margins(right: 10);
		civNoun = civ.noun;
	}

	public override void _Ready() {
		base._Ready();

		//Dimensions are 530x260 (roughly).
		//The top 110 px are for the advisor.
		AddTexture(530, 260);

		ImageTexture AdvisorHappy = Util.LoadTextureFromPCX("Art/SmallHeads/popupMILITARY.pcx", 1, 40, 149, 110, false);
		TextureRect AdvisorHead = new() {
			Texture = AdvisorHappy
		};
		//Appears at 400, 110 in game, but leftmost 25px are transparent with default graphics
		AdvisorHead.SetPosition(new Vector2(375, 0));
		AddChild(AdvisorHead);

		AddBackground(530, 150, 110);
		AddHeader("Military Advisor", 120);

		Label message = new() {
			Text = "The " + civNoun + " have been destroyed."
		};
		message.SetPosition(new Vector2(25, 170));
		AddChild(message);

		AddButton("Very well.", 215, ContinueAction);
	}

	private void ContinueAction() {
		GetParent().EmitSignal("HidePopup");
	}
}
