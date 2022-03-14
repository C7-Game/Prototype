using Godot;

public class Credits : Node2D
{
	private string creditsText;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("Now rolling the credits!");
		creditsText = System.IO.File.ReadAllText("./credits.txt");
		ShowCredits();
	}

	private void ShowCredits()
	{
		ImageTexture CreditsTexture = Util.LoadTextureFromPCX("Art/Credits/credits_background.pcx");
		ImageTexture GoBackTexture = Util.LoadTextureFromPCX("Art/exitBox-backgroundStates.pcx", 0, 0, 72, 48);
		
		TextureRect CreditsBackground = new TextureRect();
		CreditsBackground.Texture = CreditsTexture;
		AddChild(CreditsBackground);

		Label creditsLabel = new Label();
		creditsLabel.Text = creditsText;
		creditsLabel.Align = Label.AlignEnum.Center;
		creditsLabel.RectPosition = new Vector2(80, 120);
		creditsLabel.RectSize = new Vector2(864, 528);
		AddChild(creditsLabel);
		
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
