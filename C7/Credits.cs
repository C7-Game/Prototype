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

		DynamicFont regularFont = new DynamicFont();
		DynamicFont boldFont = new DynamicFont();
		DynamicFont italicFont = new DynamicFont();
		DynamicFont boldItalicFont = new DynamicFont();
		regularFont.FontData = ResourceLoader.Load<DynamicFontData>("res://Fonts/NotoSans-Regular.ttf");
		boldFont.FontData = ResourceLoader.Load<DynamicFontData>("res://Fonts/NotoSans-Bold.ttf");
		italicFont.FontData = ResourceLoader.Load<DynamicFontData>("res://Fonts/NotoSans-Italic.ttf");
		boldItalicFont.FontData = ResourceLoader.Load<DynamicFontData>("res://Fonts/NotoSans-BoldItalic.ttf");
		regularFont.Size = 14;
		boldFont.Size = 14;
		italicFont.Size = 14;
		boldItalicFont.Size = 14;
		Theme theme = new Theme();
		theme.SetFont("normal_font", "RichTextLabel", regularFont);
		theme.SetFont("bold_font", "RichTextLabel", boldFont);
		theme.SetFont("italics_font", "RichTextLabel", italicFont);
		theme.SetFont("bold_italics_font", "RichTextLabel", boldItalicFont);

		creditsLabel.Theme = theme;

		creditsLabel.AddFontOverride("normal_font", regularFont);
		creditsLabel.BbcodeText = creditsText;
		creditsLabel.BbcodeText = "[color=black]Regular Text[b]Bold Text 2[/b][i]Italic Text[/i][b][i]Bold Italic Text[/i][/b]";
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
