using Godot;
using System;

[Tool]
public partial class MenuButtonContainer : VBoxContainer {
	public Civ3MenuButton NewGame { get; private set; }
	public Civ3MenuButton QuickStart { get; private set; }
	public Civ3MenuButton Tutorial { get; private set; }
	public Civ3MenuButton LoadGame { get; private set; }
	public Civ3MenuButton LoadScenario { get; private set; }
	public Civ3MenuButton HallOfFame { get; private set; }
	public Civ3MenuButton ToggleGraphics { get; private set; }
	public Civ3MenuButton Preferences { get; private set; }
	public Civ3MenuButton AudioPreferences { get; private set; }
	public Civ3MenuButton Credits { get; private set; }
	public Civ3MenuButton Exit { get; private set; }

	public override void _Ready() {
		CreateButtons();
	}

	public void CreateButtons() {
		NewGame = new Civ3MenuButton() { Text = "New Game" };
		AddChild(NewGame);

		QuickStart = new Civ3MenuButton() { Text = "Quick Start" };
		AddChild(QuickStart);

		Tutorial = new Civ3MenuButton() { Text = "Tutorial" };
		AddChild(Tutorial);

		LoadGame = new Civ3MenuButton() { Text = "Load Game" };
		AddChild(LoadGame);

		LoadScenario = new Civ3MenuButton() { Text = "Load Scenario" };
		AddChild(LoadScenario);

		HallOfFame = new Civ3MenuButton() { Text = "Hall of Fame" };
		AddChild(HallOfFame);

		ToggleGraphics = new Civ3MenuButton() { Text = "Use OpenCiv3 Graphics" };
		AddChild(ToggleGraphics);

		Preferences = new Civ3MenuButton() { Text = "Preferences" };
		AddChild(Preferences);

		AudioPreferences = new Civ3MenuButton() { Text = "Audio Preferences" };
		AddChild(AudioPreferences);

		Credits = new Civ3MenuButton() { Text = "Credits" };
		AddChild(Credits);

		Exit = new Civ3MenuButton() { Text = "Exit" };
		AddChild(Exit);
	}
}
