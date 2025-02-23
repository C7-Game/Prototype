using Godot;
using QueryCiv3;

/****
	Need to pass values from one scene to another, particularly when loading
	a game in main menu. This script is set to auto load in project settings.
	See https://docs.godotengine.org/en/stable/getting_started/step_by_step/singletons_autoload.html
****/
public partial class GlobalSingleton : Node {
	// Will have main menu file picker set this and Game.cs pass it to C7Engine.createGame
	// which then should blank it again to prevent reloading same if going back to main menu
	// and back to game
	public string LoadGamePath;
	// For now this needs to get passed to QueryCiv3 when importing.
	public string DefaultBicPath { get => Civ3Location.GetCiv3Path() + @"/Conquests/conquests.biq"; }
	// This is the 'static map' used in lieu of terrain generation
	public string DefaultGamePath { get => @"./Text/c7-static-map-save.json"; }
	public void ResetLoadGamePath() {
		LoadGamePath = DefaultGamePath;
	}
}
