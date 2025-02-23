using Godot;
using ConvertCiv3Media;

public partial class CivilopediaButton : TextureButton {

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		Pcx buttonPcx = new Pcx(Util.Civ3MediaPath("Art/interface/menuButtons.pcx"));
		Pcx buttonPcxAlpha = new Pcx(Util.Civ3MediaPath("Art/interface/menuButtonsAlpha.pcx"));
		ImageTexture menuTexture = PCXToGodot.getImageFromPCXWithAlphaBlend(buttonPcx, buttonPcxAlpha, 36, 1, 35, 29);
		this.TextureNormal = menuTexture;
	}
}
