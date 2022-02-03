using Godot;

public class ErrorMessage : Popup
{
	private string message = "";

	public ErrorMessage(string message) {
		this.message = message;
		alignment = BoxContainer.AlignMode.Center;
		margins = new Margins(top: 100);
	}

	public override void _Ready()
	{
		base._Ready();

		AddTexture(615, 325);
		AddBackground(615, 325);

		AddHeader("Load Error", 10);

		Label errorDescription = new Label();
		//TODO: General-purpose text breaking up util.  Instead of \n
		//This appears to be the way to do multi line labels, see: https://godotengine.org/qa/11126/how-to-break-line-on-the-label-using-gdscript
		//Maybe there's an awesomer control we can user instead
		errorDescription.Text = "Not a valid save file\n" + message;
		errorDescription.SetPosition(new Vector2(25, 162));
		AddChild(errorDescription);
		
		AddButton("Return to Menu", 290, "quit");
	}

	private void quit()
	{
		GetTree().ChangeScene("res://MainMenu.tscn");
	}
}
