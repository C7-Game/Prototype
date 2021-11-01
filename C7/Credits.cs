using Godot;
using ConvertCiv3Media;

public class Credits : Node2D
{

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("Now rolling the credits!");
		ShowCredits();
	}

	private void ShowCredits()
	{
		Pcx CreditsBackgroundPCX = new Pcx(Util.Civ3MediaPath("Art/Credits/credits_background.pcx"));
		ImageTexture CreditsTexture = PCXToGodot.getImageTextureFromPCX(CreditsBackgroundPCX);
		
		TextureRect CreditsBackground = new TextureRect();
		CreditsBackground.Texture = CreditsTexture;
		AddChild(CreditsBackground);
	}
}
