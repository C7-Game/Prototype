using Godot;
using Serilog;

[GlobalClass]
public partial class Civ3FileDialog : FileDialog {
	// An object for passing information (like save file paths) between scenes.
	GlobalSingleton Global;
	private ILogger log;

	public override void _Ready() {
		base._Ready();
		log = LogManager.ForContext<Civ3FileDialog>();

		FileMode = FileDialog.FileModeEnum.OpenFile;
		Access = AccessEnum.Filesystem;

		Global = GetNode<GlobalSingleton>("/root/GlobalSingleton");
		FileSelected += OnFileSelected;
	}

	public void SetDirectory(string RelPath) {
		CurrentDir = Util.Civ3Root + "/" + RelPath;
	}

	private void OnFileSelected(string path) {
		log.Information($"loading {path}");
		Global.LoadGamePath = path;
		GetTree().ChangeSceneToFile("res://C7Game.tscn");
	}
}
