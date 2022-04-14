using Godot;

public class GameMenu : Popup {

	public GameMenu() {
		alignment = BoxContainer.AlignMode.Center;
		margins = new Margins(top: 100);
	}

	public override void _Ready() {
		base._Ready();

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

	private void map() {
		GetParent().EmitSignal("HidePopup");
	}

	private void save() {
		GetParent().EmitSignal("SaveGame");
	}

	private void quit() {
		GetParent().EmitSignal("Quit");
	}

}
