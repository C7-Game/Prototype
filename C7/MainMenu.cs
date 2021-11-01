using Godot;
using System;
using ConvertCiv3Media;

public class MainMenu : Node2D
{
	// Declare member variables here. Examples:
	// private int a = 2;
	// private string b = "text";
	
	// public string Civ3Path = Util.GetCiv3Path();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("Hello world!");
		DisplayTitleScreen();
	}
	
	private void DisplayTitleScreen()
	{	
		Pcx PcxTxtr = new Pcx(Util.Civ3MediaPath("Art/title.pcx"));
		ImageTexture Txtr = PCXToGodot.getImageTextureFromPCX(PcxTxtr);
		
		TextureRect texture = new TextureRect();
		texture.Texture = Txtr;
		AddChild(texture);
 
		Button newButton = new Button();
		newButton.Text = "New Game";
		newButton.SetPosition(new Vector2(860, 160));
		AddChild(newButton);
		newButton.Connect("pressed", this, "_on_Button_pressed");
		
		Button quickStart = new Button();
		quickStart.Text = "Quick Start";
		quickStart.SetPosition(new Vector2(860, 195));
		AddChild(quickStart);
		quickStart.Connect("pressed", this, "_on_Button_pressed");
		
		Button tutorial = new Button();
		tutorial.Text = "Tutorial";
		tutorial.SetPosition(new Vector2(860, 230));
		AddChild(tutorial);
		tutorial.Connect("pressed", this, "_on_Button_pressed");
		
		Button loadGame = new Button();
		loadGame.Text = "Load Game";
		loadGame.SetPosition(new Vector2(860, 265));
		AddChild(loadGame);
		loadGame.Connect("pressed", this, "_on_Button_pressed");
		
		Button loadScenario = new Button();
		loadScenario.Text = "Load Scenario";
		loadScenario.SetPosition(new Vector2(860, 300));
		AddChild(loadScenario);
		loadScenario.Connect("pressed", this, "_on_Button_pressed");
		
		Button hallOfFame = new Button();
		hallOfFame.Text = "Hall of Fame";
		hallOfFame.SetPosition(new Vector2(860, 335));
		AddChild(hallOfFame);
		hallOfFame.Connect("pressed", this, "_on_Button_pressed");
		
		Button preferences = new Button();
		preferences.Text = "Preferences";
		preferences.SetPosition(new Vector2(860, 370));
		AddChild(preferences);
		preferences.Connect("pressed", this, "_on_Button_pressed");
		
		Button audioPreferences = new Button();
		audioPreferences.Text = "Audio Preferences";
		audioPreferences.SetPosition(new Vector2(860, 405));
		AddChild(audioPreferences);
		audioPreferences.Connect("pressed", this, "_on_Button_pressed");
		
		Button credits = new Button();
		credits.Text = "Credits";
		credits.SetPosition(new Vector2(860, 440));
		AddChild(credits);
		credits.Connect("pressed", this, "_on_Button_pressed");
		
		Button exit = new Button();
		exit.Text = "Exit";
		exit.SetPosition(new Vector2(860, 475));
		AddChild(exit);
		exit.Connect("pressed", this, "_on_Button_pressed");
		
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
}
