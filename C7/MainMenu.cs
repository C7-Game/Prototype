using Godot;
using System;
using ConvertCiv3Media;

public class MainMenu : Node2D
{
	// Declare member variables here. Examples:
	// private int a = 2;
	// private string b = "text";
	
	// public string Civ3Path = Util.GetCiv3Path();

	
	StyleBoxFlat TransparentBackgroundStyle = new StyleBoxFlat();
	ImageTexture InactiveButton;

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
		
		TransparentBackgroundStyle.BgColor = new Color(0, 0, 0, 0);

		AddButton("New Game", 160, "_on_Button_pressed");
		AddButton("Quick Start", 195, "_on_Button_pressed");
		AddButton("Tutorial", 230, "_on_Button_pressed");
		AddButton("Load Game", 265, "_on_Button_pressed");
		AddButton("Load Scenario", 300, "_on_Button_pressed");
		AddButton("Hall of Fame", 335, "_on_Button_pressed");
		AddButton("Preferences", 370, "_on_Button_pressed");
		AddButton("Audio Preferences", 405, "_on_Button_pressed");
		AddButton("Credits", 440, "_on_Button_pressed");
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
		TextureButton startButton = new TextureButton();
		startButton.TextureNormal = InactiveButton;
		startButton.SetPosition(new Vector2(835, verticalPosition));
		AddChild(startButton);
		startButton.Connect("pressed", this, actionName);
				
		Button start = new Button();
		start.Text = label;
		start.AddColorOverride("font_color", new Color(0, 0, 0));
		start.AddStyleboxOverride("normal", TransparentBackgroundStyle);
		start.SetPosition(new Vector2(860, verticalPosition));
		AddChild(start);
		start.Connect("pressed", this, actionName);
	}

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }

	public void _on_Button_pressed()
	{
		GD.Print("Load button pressed");
		GetTree().ChangeScene("res://C7Game.tscn");    
	}
	
	public void _on_Exit_pressed()
	{
		GetTree().Quit();
	}
}
