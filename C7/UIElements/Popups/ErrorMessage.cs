using Godot;

public class ErrorMessage : TextureRect
{
	private string message = "";

	public ErrorMessage(string message) {
		this.message = message;
	}

    public override void _Ready()
    {
        base._Ready();

        //Dimensions in-game are 715x325
		//The top 100px are empty

		//Create a transparent texture background of the appropriate size.
		//This is super important as if we just add the children, the parent won't be able to figure
		//out the size of this TextureRect, and it won't be able to align it properly.
		ImageTexture thisTexture = new ImageTexture();
		Image image = new Image();
		image.Create(715, 325, false, Image.Format.Rgba8);
		image.Fill(Color.Color8(0, 0, 0, 0));
		thisTexture.CreateFromImage(image);
		this.Texture = thisTexture;

        TextureRect background = PopupOverlay.GetPopupBackground(715, 225);
		background.SetPosition(new Vector2(0, 100));
		AddChild(background);

		PopupOverlay.AddHeaderToPopup(this, "Load Error", 110);

        Label errorDescription = new Label();
		//TODO: General-purpose text breaking up util.  Instead of \n
		//This appears to be the way to do multi line labels, see: https://godotengine.org/qa/11126/how-to-break-line-on-the-label-using-gdscript
		//Maybe there's an awesomer control we can user instead
		errorDescription.Text = message;

		errorDescription.SetPosition(new Vector2(25, 162));
		AddChild(errorDescription);
        
		PopupOverlay.AddButton(this, "Return to Menu", 188, "quit");
    }
    private void quit()
	{
		GetTree().ChangeScene("res://MainMenu.tscn");
	}
}