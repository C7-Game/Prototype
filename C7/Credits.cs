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

		//Todo: Either import Text/credits.txt and scroll it as done in-game, or have our own custom credits.
		Label creditsLabel = new Label();
		creditsLabel.AddColorOverride("font_color", new Color(0, 0, 0));
		creditsLabel.Text = "Project Ringleader: WildWeazel";
		creditsLabel.SetPosition(new Vector2(360, 120));
		AddChild(creditsLabel);

		Pcx GoBackPCX = new Pcx(Util.Civ3MediaPath("Art/exitBox-backgroundStates.pcx"));
		ImageTexture GoBackTexture = PCXToGodot.getImageTextureFromPCX(GoBackPCX, 0, 0, 72, 48);

		TextureButton GoBackButton = new TextureButton();
		GoBackButton.TextureNormal = GoBackTexture;
		GoBackButton.SetPosition(new Vector2(952, 720));
		AddChild(GoBackButton);
		GoBackButton.Connect("pressed", this, "ReturnToMenu");
	}
	public void ReturnToMenu()
	{
		GD.Print("Returning to main menu");
		GetTree().ChangeScene("res://MainMenu.tscn");    
	}
}
