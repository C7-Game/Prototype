using Godot;
using Serilog;

public class Credits : Node2D
{
	private string creditsText = "Could not load credits file";

	private static ILogger log = Log.ForContext<Credits>();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		log.Information("Now rolling the credits!");
		try
		{
			creditsText = System.IO.File.ReadAllText("./Text/credits.txt");
		}
		catch(System.Exception ex)
		{
			log.Error(ex, "Failed to read from credits.txt!");
		}
		ShowCredits();
	}

	private void ShowCredits()
	{
		ImageTexture CreditsTexture = Util.LoadTextureFromPCX("Art/Credits/credits_background.pcx");
		ImageTexture GoBackTexture = Util.LoadTextureFromPCX("Art/exitBox-backgroundStates.pcx", 0, 0, 72, 48);

		TextureRect CreditsBackground = new TextureRect();
		CreditsBackground.Texture = CreditsTexture;
		AddChild(CreditsBackground);

		RichTextLabel creditsLabel = new RichTextLabel();
		creditsLabel.RectPosition = new Vector2(80, 120);
		creditsLabel.RectSize = new Vector2(864, 528);
		creditsLabel.BbcodeEnabled = true;

		DynamicFont smallFont = new DynamicFont();
		smallFont.FontData = ResourceLoader.Load<DynamicFontData>("res://Fonts/NotoSans-Regular.ttf");
		smallFont.Size = 12;

		creditsLabel.AddFontOverride("normal_font", smallFont);
		// creditsLabel.PushFont(ResourceLoader.Load<DynamicFontData>("res://Fonts/NotoSans-Regular.ttf"));
		creditsLabel.BbcodeText = creditsText;
		creditsLabel.BbcodeText = "[color=black]<b>Bold Text</b>[b]Bold Text 2[/b]Not Bold Text";
		AddChild(creditsLabel);

		TextureButton GoBackButton = new TextureButton();
		GoBackButton.TextureNormal = GoBackTexture;
		GoBackButton.SetPosition(new Vector2(952, 720));
		AddChild(GoBackButton);
		GoBackButton.Connect("pressed", this, "ReturnToMenu");
	}
	public void ReturnToMenu()
	{
		log.Information("Returning to main menu");
		GetTree().ChangeScene("res://MainMenu.tscn");
	}
}
