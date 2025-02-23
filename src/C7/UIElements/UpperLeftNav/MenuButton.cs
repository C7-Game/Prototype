using Godot;
using ConvertCiv3Media;

public partial class MenuButton : TextureButton {

	[Export]
	private PopupOverlay popupOverlay;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		Pcx buttonPcx = Util.LoadPCX("Art/interface/menuButtons.pcx");
		Pcx buttonPcxAlpha = Util.LoadPCX("Art/interface/menuButtonsAlpha.pcx");
		//TODO: Caching for these textures
		ImageTexture menuTexture = PCXToGodot.getImageFromPCXWithAlphaBlend(buttonPcx, buttonPcxAlpha, 0, 1, 35, 29);
		this.TextureNormal = menuTexture;
	}

	public override void _Pressed() {
		popupOverlay.ShowPopup(new GameMenu(), PopupOverlay.PopupCategory.Info);
	}

}
