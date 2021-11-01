using Godot;
using ConvertCiv3Media;

public class MainMenu : Node2D
{
	readonly int BUTTON_LABEL_OFFSET = 4;
	
	StyleBoxFlat TransparentBackgroundStyle = new StyleBoxFlat();
	StyleBoxFlat TransparentBackgroundHoverStyle = new StyleBoxFlat();
	ImageTexture InactiveButton;
	ImageTexture HoverButton;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("Hello world!");
		DisplayTitleScreen();
	}
	
	private void DisplayTitleScreen()
	{	
		SetMainMenuBackground();
		
		Pcx ButtonsTxtr = new Pcx(Util.Civ3MediaPath("Art/buttonsFINAL.pcx"));
		InactiveButton = PCXToGodot.getImageTextureFromPCX(ButtonsTxtr, 1, 1, 20, 20);
		HoverButton = PCXToGodot.getImageTextureFromPCX(ButtonsTxtr, 22, 1, 20, 20);
		
		TransparentBackgroundStyle.BgColor = new Color(0, 0, 0, 0);
		TransparentBackgroundHoverStyle.BgColor = new Color(0, 0, 0, 0);

		AddButton("New Game", 160, "_on_Button_pressed");
		AddButton("Quick Start", 195, "_on_Button_pressed");
		AddButton("Tutorial", 230, "_on_Button_pressed");
		AddButton("Load Game", 265, "_on_Button_pressed");
		AddButton("Load Scenario", 300, "_on_Button_pressed");
		AddButton("Hall of Fame", 335, "_on_Button_pressed");
		AddButton("Preferences", 370, "_on_Button_pressed");
		AddButton("Audio Preferences", 405, "_on_Button_pressed");
		AddButton("Credits", 440, "showCredits");
		AddButton("Exit", 475, "_on_Exit_pressed");
	}

	private void SetMainMenuBackground()
	{
		Pcx TitleScreenPCX = new Pcx(Util.Civ3MediaPath("Art/title.pcx"));
		ImageTexture TitleScreenTexture = PCXToGodot.getImageTextureFromPCX(TitleScreenPCX);
		
		TextureRect MainMenuBackground = new TextureRect();
		MainMenuBackground.Texture = TitleScreenTexture;
		AddChild(MainMenuBackground);
	}

	private void AddButton(string label, int verticalPosition, string actionName)
	{
		TextureButton newButton = new TextureButton();
		newButton.TextureNormal = InactiveButton;
		newButton.TextureHover = HoverButton;
		newButton.SetPosition(new Vector2(835, verticalPosition));
		AddChild(newButton);
		newButton.Connect("pressed", this, actionName);
				
		Button newButtonLabel = new Button();
		newButtonLabel.Text = label;

		newButtonLabel.AddColorOverride("font_color", new Color(0, 0, 0));
		//Only the Exit and part of the Credits button are getting the right color.  The rest are black.  Not sure why.
		newButtonLabel.AddColorOverride("font_color_hover", new Color(181.0f/255, 90.0f/255, 0, 255));
		//N.B. there are also font_color_pressed and font_color_disabled.

		newButtonLabel.AddStyleboxOverride("normal", TransparentBackgroundStyle);
		newButtonLabel.AddStyleboxOverride("hover", TransparentBackgroundHoverStyle);
		newButtonLabel.AddStyleboxOverride("pressed", TransparentBackgroundHoverStyle);

		newButtonLabel.SetPosition(new Vector2(860, verticalPosition + BUTTON_LABEL_OFFSET));
		AddChild(newButtonLabel);
		newButtonLabel.Connect("pressed", this, actionName);
	}

	public void _on_Button_pressed()
	{
		GD.Print("Load button pressed");
		GetTree().ChangeScene("res://C7Game.tscn");    
	}
	
	public void showCredits()
	{
		GD.Print("Credits button pressed");
		GetTree().ChangeScene("res://Credits.tscn");    
	}
	
	public void _on_Exit_pressed()
	{
		GetTree().Quit();
	}
}
