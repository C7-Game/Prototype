using Godot;

public partial class GameMenu : Popup
{

	private static GameMenu instance = null;

	private GameMenu() {
		alignment = BoxContainer.AlignmentMode.Center;
		margins = new Margins(top: 100);

		AddTexture(370, 300);
		AddBackground(370, 300);

		AddHeader("Main Menu", 10);

		AddButton("Map", 60, "map");
		AddButton("Load Game (Ctrl-L)", 85, "load");
		AddButton("New Game (Ctrl-Shift-Q)", 110, "newGame");
		AddButton("Preferences (Ctrl-P)", 135, "preferences");
		AddButton("Retire (Ctrl-Q)", 160, "retire");
		AddButton("Save Game (Ctrl-S)", 185, "save");
		AddButton("Quit Game (ESC)", 210, "quit");
	}

	public static GameMenu Get() {
		if (GameMenu.instance == null) {
			GameMenu.instance = new GameMenu();
		}
		return GameMenu.instance;
	}

	public override void _Ready()
	{
		base._Ready();
	}

	private void quit()
	{
		GetParent().EmitSignal("Quit");
	}

	private void map()
	{
		GetParent().EmitSignal("HidePopup");
	}

}
