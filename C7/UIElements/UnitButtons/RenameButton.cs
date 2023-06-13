using Godot;
using ConvertCiv3Media;

public partial class RenameButton : TextureButton
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//TODO: The "Conquests" shouldn't be necessary, but is currently.
		Pcx buttonPcx = new Pcx(Util.Civ3MediaPath("Conquests/Art/interface/NormButtons.PCX"));
		Pcx buttonPcxAlpha = new Pcx(Util.Civ3MediaPath("Conquests/Art/interface/ButtonAlpha.pcx"));
		ImageTexture menuTexture = PCXToGodot.getImageFromPCXWithAlphaBlend(buttonPcx, buttonPcxAlpha, 64, 224, 32, 32);
		this.TextureNormal = menuTexture;
	}
}
