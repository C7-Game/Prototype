using Godot;
using ConvertCiv3Media;
using C7GameData;
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
	Util.Civ3FileDialog LoadScenarioDialog;
	GlobalSingleton Global;

	readonly int MENU_OFFSET_FROM_TOP = 180;
	readonly int MENU_OFFSET_FROM_LEFT = 180;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("Hello world!");
		// To pass data between scenes, putting path string in a global singleton and reading it later in createGame
		Global = GetNode<GlobalSingleton>("/root/GlobalSingleton");
		Global.ResetLoadGamePath();
		LoadDialog = new Util.Civ3FileDialog();
		LoadDialog.RelPath = @"Conquests/Saves";
		LoadDialog.Connect("file_selected", this, nameof(_on_FileDialog_file_selected));
		LoadScenarioDialog = new Util.Civ3FileDialog();
		LoadScenarioDialog.RelPath = @"Conquests/Scenarios";
		LoadScenarioDialog.Connect("file_selected", this, nameof(_on_FileDialog_file_selected));
		GetNode<CanvasLayer>("CanvasLayer").AddChild(LoadDialog);
		SetCiv3Home = GetNode<Button>("CanvasLayer/SetCiv3Home");
		SetCiv3HomeDialog = GetNode<FileDialog>("CanvasLayer/SetCiv3HomeDialog");
		// For some reason this option isn't available in the scene UI
		SetCiv3HomeDialog.Mode = FileDialog.ModeEnum.OpenDir;
		GetNode<CanvasLayer>("CanvasLayer").AddChild(LoadScenarioDialog);
		DisplayTitleScreen();
	}

	private void DisplayTitleScreen()
	{
		try {
			SetMainMenuBackground();

			InactiveButton = Util.LoadTextureFromPCX("Art/buttonsFINAL.pcx", 1, 1, 20, 20);
			HoverButton = Util.LoadTextureFromPCX("Art/buttonsFINAL.pcx", 22, 1, 20, 20);

			AddButton("New Game", 0, "StartGame");
			AddButton("Quick Start", 35, "StartGame");
			AddButton("Tutorial", 70, "StartGame");
			AddButton("Load Game", 105, "LoadGame");
			AddButton("Load Scenario", 140, "LoadScenario");
			AddButton("Hall of Fame", 175, "HallOfFame");
			AddButton("Preferences", 210, "Preferences");
			AddButton("Audio Preferences", 245, "Preferences");
			AddButton("Credits", 280, "showCredits");
			AddButton("Exit", 315, "_on_Exit_pressed");

			// Hide select home folder if valid path is present as proven by reaching this point in code
			SetCiv3Home.Visible = false;
		}
		catch(Exception ex)
		{
			GD.Print("Could not set up the main menu", ex);
			GetNode<Label>("CanvasLayer/Label").Visible = true;
			GetNode<ColorRect>("CanvasLayer/ColorRect").Visible = true;
		}
	}

	private void SetMainMenuBackground()
	{
		ImageTexture TitleScreenTexture = Util.LoadTextureFromC7JPG("Title_Screen.jpg");
		MainMenuBackground = GetNode<TextureRect>("CanvasLayer/MainMenuBackground");
		MainMenuBackground.StretchMode = TextureRect.StretchModeEnum.Scale;
		MainMenuBackground.Texture = TitleScreenTexture;
	}

	private void AddButton(string label, int verticalPosition, string actionName)
	{
		TextureButton newButton = new TextureButton();
		newButton.TextureNormal = InactiveButton;
		newButton.TextureHover = HoverButton;
		newButton.SetPosition(new Vector2(MENU_OFFSET_FROM_LEFT, MENU_OFFSET_FROM_TOP + verticalPosition));
		MainMenuBackground.AddChild(newButton);
		newButton.Connect("pressed", this, actionName);

		Button newButtonLabel = new Button();
		newButtonLabel.Text = label;

		newButtonLabel.SetPosition(new Vector2(MENU_OFFSET_FROM_LEFT + 25, MENU_OFFSET_FROM_TOP + verticalPosition + BUTTON_LABEL_OFFSET));
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

	public void LoadScenario()
	{
		GD.Print("Load scenario button pressed");
		PlayButtonPressedSound();
		LoadScenarioDialog.Popup_();
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
