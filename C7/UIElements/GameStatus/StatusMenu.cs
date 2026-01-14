using Godot;
using System;
using C7GameData;
using C7Engine;

[GlobalClass]
public partial class StatusMenu : Control {
	[Export] ConsoleButton openDiplomacy;
	[Export] ConsoleButton openPalaceScreen;

	[Export] PopupOverlay popupOverlay;
	[Export] Control palaceScene;

	public override void _Ready() {
		openDiplomacy.Pressed += OpenDiplomacyPopup;
		openPalaceScreen.Pressed += palaceScene.Show;
	}

	public override void _Process(double delta) {
		EngineStorage.ReadGameData((GameData gD) => {
			if (gD.observerMode) {
				return;
			}

			Player player = gD.GetFirstHumanPlayer();

			// Only show the diplomacy button if we have civs to talk to.
			if (player.playerRelationships.Count > 0) {
				openDiplomacy.ShowButton();
			}

			// TODO: Don't show the palace button if the player can't start building the palace
			openPalaceScreen.ShowButton();
		});
	}

	private void OpenDiplomacyPopup() {
		EngineStorage.ReadGameData((GameData gD) => {
			Player player = gD.GetFirstHumanPlayer();

			popupOverlay.ShowPopup(new DiplomacySelection(player, gD.players), PopupOverlay.PopupCategory.Info);
		});
	}
}
