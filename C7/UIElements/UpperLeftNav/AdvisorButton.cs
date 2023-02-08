using Godot;
using ConvertCiv3Media;

public partial class AdvisorButton : TextureButton
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Pcx buttonPcx = new Pcx(Util.Civ3MediaPath("Art/interface/menuButtons.pcx"));
		Pcx buttonPcxAlpha = new Pcx(Util.Civ3MediaPath("Art/interface/menuButtonsAlpha.pcx"));
		ImageTexture texture = PCXToGodot.getImageFromPCXWithAlphaBlend(buttonPcx, buttonPcxAlpha, 73, 1, 35, 29);
		ImageTexture textureHover = PCXToGodot.getImageFromPCXWithAlphaBlend(buttonPcx, buttonPcxAlpha, 73, 61, 35, 29, 60);
		ImageTexture texturePressed = PCXToGodot.getImageFromPCXWithAlphaBlend(buttonPcx, buttonPcxAlpha, 73, 121, 35, 29, 120);
		this.TextureNormal = texture;
		this.TextureHover = textureHover;
		this.TexturePressed = texturePressed;
	}
}
