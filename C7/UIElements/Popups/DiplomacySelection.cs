using Godot;
using C7GameData;
using System.Collections.Generic;

// The popup for selecting which other civilization to contact.
public partial class DiplomacySelection : Popup {
	private Player player;
	private List<Player> allPlayers;

	public DiplomacySelection(Player player, List<Player> allPlayers) {
		alignment = BoxContainer.AlignmentMode.Center;
		margins = new Margins(top: 200);
		this.player = player;
		this.allPlayers = allPlayers;
	}

	public override void _Ready() {
		base._Ready();

		int width = 530;
		int height = 115 + 25 * player.playerRelationships.Keys.Count;
		AddTexture(width, height);
		AddBackground(width, height);
		AddHeader("Pick the civilization...", 10);

		int vOffset = 65;
		foreach (KeyValuePair<ID, PlayerRelationship> kvp in player.playerRelationships) {
			string status = kvp.Value.AtWar() ? "War" : "Peace";

			var diplomacy = GetNode<Diplomacy>("../%DiplomacyOverlay");
			var popupOverlay = GetParent<PopupOverlay>();

			AddButton($"{allPlayers.Find(x => x.id == kvp.Key).civilization.noun} (at {status})", vOffset, () => {
				popupOverlay.Hide();
				diplomacy.ShowTalkScreenForPlayer(player.id, kvp.Key);
			});
			vOffset += 25;
		}

		// TODO: Do something when the confirm button is pressed.
		AddConfirmButton(new Vector2(width - 55, height - 47), () => { });
		AddCancelButton(new Vector2(width - 30, height - 47));
	}
}
