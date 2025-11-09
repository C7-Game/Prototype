using Godot;
using System;
using C7Engine;
using Serilog;

public partial class MainMenu : Node {
	private ILogger log;

	[Export]
	Civ3FileDialog LoadDialog;
	[Export]
	Control NoCiv3Options;
	[Export]
	FileDialog SetCiv3HomeDialog;
	[Export]
	Civ3FileDialog LoadScenarioDialog;
	[Export]
	MenuButtonContainer ButtonContainer;
	[Export]
	AudioStreamPlayer player;

	GlobalSingleton Global;

	public override void _Ready() {
		log = LogManager.ForContext<MainMenu>();
		log.Debug("enter MainMenu._Ready");

		DisplayServer.WindowSetTitle("C7 - Godot 4");

		try {
			DisplayTitleScreen();
		} catch (Exception ex) {
			log.Error(ex, "Could not set up the main menu");
		}
	}

	private void DisplayTitleScreen() {
		// To pass data between scenes, putting path string in a global singleton and reading it later in createGame
		Global = GetNode<GlobalSingleton>("/root/GlobalSingleton");
		Global.ResetLoadGamePath();

		LoadDialog.SetDirectoryForLoading(@"Conquests/Saves");
		LoadScenarioDialog.SetDirectoryForLoading(@"Conquests/Scenarios");
		LoadScenarioDialog.GoToScenarioSetupAfterLoading = true;

		// If couldn't find the button textures when ButtonContainer._Ready, then
		// these buttons will be null, so be sure to recreate them if we call
		// DisplayTitleScreen after picking standalone mode or getting the civ3
		// home path set.
		if (ButtonContainer.NewGame == null) {
			ButtonContainer.CreateButtons();
		}
		ButtonContainer.NewGame.Pressed += GoToWorldSetup;
		ButtonContainer.QuickStart.Pressed += GoToWorldSetup;
		ButtonContainer.Tutorial.Pressed += StartGame;
		ButtonContainer.LoadGame.Pressed += LoadGame;
		ButtonContainer.LoadScenario.Pressed += LoadScenario;
		ButtonContainer.HallOfFame.Pressed += HallOfFame;
		ButtonContainer.Preferences.Pressed += Preferences;
		ButtonContainer.AudioPreferences.Pressed += Preferences;
		ButtonContainer.Credits.Pressed += showCredits;
		ButtonContainer.Exit.Pressed += _on_Exit_pressed;

		ButtonContainer.ToggleGraphics.Pressed += () => {
			Global.ToggleModernGraphics();
			SetToggleGraphicsText();
		};
		SetToggleGraphicsText();

		// We can't toggle to using civ3 graphics in standalone mode.
		if (C7Settings.UseStandaloneMode()) {
			ButtonContainer.ToggleGraphics.Visible = false;
		}

		// Hide if valid path is present as proven by reaching this point in code
		NoCiv3Options.Visible = false;
	}

	private void SetToggleGraphicsText() {
		if (Global.ModernGraphicsActive) {
			ButtonContainer.ToggleGraphics.Text = "Turn on Civ3 Graphics";
		} else {
			ButtonContainer.ToggleGraphics.Text = "Turn on C7 Graphics";
		}
	}

	public void GoToWorldSetup() {
		PlayButtonPressedSound();
		GetTree().ChangeSceneToFile("res://UIElements/NewGame/world_setup.tscn");
	}

	public void StartGame() {
		log.Information("start game button pressed");
		PlayButtonPressedSound();
		GetTree().ChangeSceneToFile("res://C7Game.tscn");
	}

	public void LoadGame() {
		log.Information("load game button pressed");
		PlayButtonPressedSound();
		LoadDialog.Popup();
	}

	public void LoadScenario() {
		log.Information("load scenario button pressed");
		PlayButtonPressedSound();
		LoadScenarioDialog.Popup();
	}

	public void showCredits() {
		log.Information("credits button pressed");
		GetTree().ChangeSceneToFile("res://Credits.tscn");
	}

	public void HallOfFame() {
		PlayButtonPressedSound();
	}

	public void Preferences() {
		PlayButtonPressedSound();
	}

	public void _on_Exit_pressed() {
		GetTree().Quit(); // no need to notify the scene tree
	}

	private void PlayButtonPressedSound() {
		AudioStreamWav wav = Util.LoadCiv3WAVFromDisk("Sounds/Button1.wav");
		if (wav == null) {
			return;
		}
		player.Stream = wav;
		player.Play();
	}

	private void _on_SetCiv3Home_pressed() {
		SetCiv3HomeDialog.Popup();
	}

	private void _on_SetCiv3HomeDialog_dir_selected(string path) {
		Util.Civ3Root = path;
		C7Settings.SetValue("locations", "civ3InstallDir", path);
		C7Settings.SaveSettings();
		// This function should only be reachable if DisplayTitleScreen failed on previous runs, so should be OK to run here
		DisplayTitleScreen();
	}

	private void UseStandaloneModePressed() {
		Global.ToggleModernGraphics();
		C7Settings.SetValue("locations", "useStandaloneMode", "true");
		C7Settings.SaveSettings();
		DisplayTitleScreen();
	}
}
