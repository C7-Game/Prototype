using Godot;

public partial class GameMenu : Popup {

	public Util.Civ3FileDialog SaveDialog;

	public GameMenu() {
		alignment = BoxContainer.AlignmentMode.Center;
		margins = new Margins(top: 100);
	}

	public override void _Ready() {
		base._Ready();

		this.SaveDialog = new Util.Civ3FileDialog(FileDialog.FileModeEnum.SaveFile);
		this.SaveDialog.Connect("file_selected", Callable.From((string path) => GetParent().EmitSignal("SaveGame", path)));
		GetNode<CanvasLayer>("/root/C7Game/CanvasLayer").AddChild(this.SaveDialog);

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

	private void quit() {
		GetParent().EmitSignal("Quit");
	}

	private void map() {
		GetParent().EmitSignal("HidePopup");
	}

	private void save() {
		SaveDialog.Popup();
	}
}
