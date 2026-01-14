using Godot;
using System;
using System.Diagnostics;
using C7GameData;
using C7Engine;
using C7GameData.Save;
using System.Collections.Generic;
using Serilog;

// The popup for selecting which other civilization to contact.
public partial class GovernmentSelection : Popup {
	private Player player;
	private List<Government> governments;

	public GovernmentSelection(Player player, List<Government> governments) {
		alignment = BoxContainer.AlignmentMode.Center;
		margins = new Margins(top: 200);
		this.player = player;
		this.governments = governments;
	}

	public override void _Ready() {
		base._Ready();

		int width = 530;
		int height = 115 + 25 * governments.Count;
		AddTexture(width, height);
		AddBackground(width, height);
		AddHeader("Select a new government type", 10);

		int vOffset = 65;
		foreach (Government g in governments) {
			AddButton($"{g.name}", vOffset, () => {
				Node parent = GetParent();
				new SelectGovernmentMsg(player, g).send();
				parent.EmitSignal(PopupOverlay.SignalName.HidePopup);
			});
			vOffset += 25;
		}
	}

	public override void _ExitTree() {
		// Restart the turn once a selection has been made.
		new MsgStartTurn().send();
	}

}
