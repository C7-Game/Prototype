using System;
using Godot;
using Serilog;

public partial class GameMenu : Popup {
	public GameMenu() {
		alignment = BoxContainer.AlignmentMode.Center;
		margins = new Margins(top: 100);
	}

	public override void _Ready() {
		base._Ready();

		AddTexture(370, 300);
		AddBackground(370, 300);

		AddHeader("Main Menu", 10);

		// TODO: enable buttons as the features are implemented
		AddButton("Map", 60, map);
		AddButton("Load Game", 85, load);
		//AddButton("New Game (Ctrl-Shift-Q)", 110, newGame);
		//AddButton("Preferences (Ctrl-P)", 135, preferences);
		AddButton("Retire", 110, retire);
		AddButton("Save Game", 135, save);
		AddButton("Quit Game (ESC)", 160, quit);

	}

	private void save() {
		var loadDialog = GetNode<Civ3FileDialog>("../%LoadDialog");
		// TODO: this should go to our own saves directory.
		loadDialog.SetDirectoryForSaving(@"Conquests/Saves");

		// TODO: The main menu does sound playing but we don't know our path in
		// the scene, which makes this hard.
		// PlayButtonPressedSound();
		GetParent().EmitSignal(PopupOverlay.SignalName.HidePopup);

		loadDialog.Popup();
	}

	private void preferences() {
		throw new NotImplementedException();
	}

	private void newGame() {
		throw new NotImplementedException();
	}

	private void load() {
		var loadDialog = GetNode<Civ3FileDialog>("../%LoadDialog");
		loadDialog.SetDirectoryForLoading(@"Conquests/Saves");

		// TODO: The main menu does sound playing but we don't know our path in
		// the scene, which makes this hard.
		// PlayButtonPressedSound();
		GetParent().EmitSignal(PopupOverlay.SignalName.HidePopup);

		loadDialog.Popup();
	}

	private void quit() {
		GetParent().EmitSignal(PopupOverlay.SignalName.Quit);
	}

	private void retire() {
		GetParent().EmitSignal(PopupOverlay.SignalName.Retire);
	}

	private void map() {
		GetParent().EmitSignal(PopupOverlay.SignalName.HidePopup);
	}
}
