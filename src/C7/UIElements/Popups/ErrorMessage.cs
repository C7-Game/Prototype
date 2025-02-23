using Godot;

public partial class ErrorMessage : Popup {
	private string message = "";

	public ErrorMessage(string message) {
		this.message = message;
		alignment = BoxContainer.AlignmentMode.Center;
		margins = new Margins(top: 100);
	}

	public override void _Ready() {
		base._Ready();

		AddTexture(615, 325);
		AddBackground(615, 325);

		AddHeader("Load Error", 10);

		Label errorDescription = new Label();
		//TODO: General-purpose text breaking up util.  Instead of \n
		//This appears to be the way to do multi line labels, see: https://godotengine.org/qa/30459/label-or-richtextlabel-auto-width
		//Maybe there's an awesomer control we can user instead.
		//But it should also be general-purpose, not just coded one-off here.
		errorDescription.Text = "Not a valid save file\n" + message;
		errorDescription.SetPosition(new Vector2(25, 162));
		AddChild(errorDescription);

		AddButton("Return to Menu", 290, quit);
	}

	private void quit() {
		GetTree().ChangeSceneToFile("res://MainMenu.tscn");
	}
}
