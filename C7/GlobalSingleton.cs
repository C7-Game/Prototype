using Godot;
using C7Engine;
using C7GameData.Save;

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

	// Generated game data used when starting a new game
	public SaveGame SaveGame;

	public bool ModernGraphicsActive { get; private set; }

	public GlobalSingleton() {
		if (C7Settings.UseStandaloneMode()) {
			ToggleModernGraphics();
		}
	}

	// The characteristics of the world to generate. This exists in the singleton
	// to allow the world setup screen to pass the information to the player
	// setup screen, which is what actually kicks off the world generation.
	public WorldCharacteristics WorldCharacteristics;

	public void ResetLoadGameFields() {
		LoadGamePath = null;
		SaveGame = null;
	}

	public void ToggleModernGraphics() {
		string newConfig = ModernGraphicsActive ? GamePaths.ClassicGraphicsConfig : GamePaths.ModernGraphicsConfig;
		TextureLoader.SetConfig(GamePaths.TextureConfigsDir, newConfig);
		ModernGraphicsActive = !ModernGraphicsActive;
	}
}
