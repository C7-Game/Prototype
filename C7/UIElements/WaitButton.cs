using Godot;
using ConvertCiv3Media;

public class WaitButton : TextureButton
{

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Pcx buttonPcx = new Pcx(Util.Civ3MediaPath("Art/interface/NormButtons.pcx"));
		Pcx buttonPcxAlpha = new Pcx(Util.Civ3MediaPath("Art/interface/ButtonAlpha.pcx"));
		ImageTexture menuTexture = PCXToGodot.getImageFromPCXWithAlphaBlend(buttonPcx, buttonPcxAlpha, 32, 0, 32, 32);
		this.TextureNormal = menuTexture;		
	}

}
