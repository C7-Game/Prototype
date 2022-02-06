using Godot;
using ConvertCiv3Media;
using System;

public class MainMenu : Node2D
{
	readonly int BUTTON_LABEL_OFFSET = 4;
	
	ImageTexture InactiveButton;
	ImageTexture HoverButton;
	TextureRect MainMenuBackground;
	Util.Civ3FileDialog LoadDialog;
	Button SetCiv3Home;
	FileDialog SetCiv3HomeDialog;
	GlobalSingleton Global;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("Hello world!");
		// To pass data between scenes, putting path string in a global singleton and reading it later in createGame
		Global = GetNode<GlobalSingleton>("/root/GlobalSingleton");
		Global.ResetLoadGamePath();
		DisplayTitleScreen();
		LoadDialog = new Util.Civ3FileDialog();
		LoadDialog.RelPath = @"Conquests/Saves";
		LoadDialog.Connect("file_selected", this, nameof(_on_FileDialog_file_selected));
		GetNode<CanvasLayer>("CanvasLayer").AddChild(LoadDialog);
		SetCiv3Home = GetNode<Button>("CanvasLayer/SetCiv3Home");
		SetCiv3HomeDialog = GetNode<FileDialog>("CanvasLayer/SetCiv3HomeDialog");
		// For some reason this option isn't available in the scene UI
		SetCiv3HomeDialog.Mode = FileDialog.ModeEnum.OpenDir;
	}
	
	private void DisplayTitleScreen()
	{	
		try {
			SetMainMenuBackground();
			
			InactiveButton = Util.LoadTextureFromPCX("Art/buttonsFINAL.pcx", 1, 1, 20, 20);
			HoverButton = Util.LoadTextureFromPCX("Art/buttonsFINAL.pcx", 22, 1, 20, 20);

			AddButton("New Game", 160, "StartGame");
			AddButton("Quick Start", 195, "StartGame");
			AddButton("Tutorial", 230, "StartGame");
			AddButton("Load Game", 265, "LoadGame");
			AddButton("Load Scenario", 300, "StartGame");
			AddButton("Hall of Fame", 335, "HallOfFame");
			AddButton("Preferences", 370, "Preferences");
			AddButton("Audio Preferences", 405, "Preferences");
			AddButton("Credits", 440, "showCredits");
			AddButton("Exit", 475, "_on_Exit_pressed");

			// Hide select home folder if valid path is present as proven by reaching this point in code
			SetCiv3Home.Visible = false;
		}
		catch(Exception ex)
		{
			GD.Print("Could not set up the main menu");
		}
	}

	private void SetMainMenuBackground()
	{
		ImageTexture TitleScreenTexture = Util.LoadTextureFromPCX("Art/title.pcx");
		MainMenuBackground = GetNode<TextureRect>("CanvasLayer/CenterContainer/MainMenuBackground");
		MainMenuBackground.Texture = TitleScreenTexture;
	}

	private void AddButton(string label, int verticalPosition, string actionName)
	{
		TextureButton newButton = new TextureButton();
		newButton.TextureNormal = InactiveButton;
		newButton.TextureHover = HoverButton;
		newButton.SetPosition(new Vector2(835, verticalPosition));
		MainMenuBackground.AddChild(newButton);
		newButton.Connect("pressed", this, actionName);
				
		Button newButtonLabel = new Button();
		newButtonLabel.Text = label;

		newButtonLabel.SetPosition(new Vector2(860, verticalPosition + BUTTON_LABEL_OFFSET));
		MainMenuBackground.AddChild(newButtonLabel);
		newButtonLabel.Connect("pressed", this, actionName);
	}

	public void StartGame()
	{
		GD.Print("Load button pressed");
		PlayButtonPressedSound();
		GetTree().ChangeScene("res://C7Game.tscn");
	}
	
	public void LoadGame()
	{
		GD.Print("Real Load button pressed");
		PlayButtonPressedSound();
		LoadDialog.Popup_();
	}
	
	public void showCredits()
	{
		GD.Print("Credits button pressed");
		GetTree().ChangeScene("res://Credits.tscn");
	}

	public void HallOfFame()
	{
		PlayButtonPressedSound();
	}

	public void Preferences()
	{

		PlayButtonPressedSound();
	}
	
	public void _on_Exit_pressed()
	{
		GetTree().Quit();
	}

	private void PlayButtonPressedSound()
	{
		AudioStreamSample wav = Util.LoadWAVFromDisk(Util.Civ3MediaPath("Sounds/Button1.wav"));
		AudioStreamPlayer player = GetNode<AudioStreamPlayer>("CanvasLayer/SoundEffectPlayer");
		player.Stream = wav;
		player.Play();
	}
	private void _on_FileDialog_file_selected(string path)
	{
		GD.Print("Loading " + path);
		Global.LoadGamePath = path;
		GetTree().ChangeScene("res://C7Game.tscn");

	}
	private void _on_SetCiv3Home_pressed()
	{
		SetCiv3HomeDialog.Popup_();
	}
	private void _on_SetCiv3HomeDialog_dir_selected(string path)
	{
		Util.Civ3Root = path;
		// This function should only be reachable if DisplayTitleScreen failed on previous runs, so should be OK to run here
		DisplayTitleScreen();
	}
}
