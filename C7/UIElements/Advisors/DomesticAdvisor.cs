using Godot;
using System;

public class DomesticAdvisor : TextureRect
{
	// Declare member variables here. Examples:
	// private int a = 2;
	// private string b = "text";

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		this.CreateUI();
	}

	private void CreateUI() {
		ImageTexture DomesticBackground = Util.LoadTextureFromPCX("Art/Advisors/domestic.pcx");
		this.Texture = DomesticBackground;

		//TODO: Age-based background.  Only use Ancient for now.
		ImageTexture AdvisorHappy = Util.LoadTextureFromPCX("Art/SmallHeads/popupDOMESTIC.pcx", 1, 40, 149, 110);
		ImageTexture AdvisorAngry = Util.LoadTextureFromPCX("Art/SmallHeads/popupDOMESTIC.pcx", 151, 40, 149, 110);
		ImageTexture AdvisorSad = Util.LoadTextureFromPCX("Art/SmallHeads/popupDOMESTIC.pcx", 301, 40, 149, 110);
		ImageTexture AdvisorSurprised = Util.LoadTextureFromPCX("Art/SmallHeads/popupDOMESTIC.pcx", 451, 40, 149, 110);

		TextureRect AdvisorHead = new TextureRect();
		//TODO: Randomize or set logically
		AdvisorHead.Texture = AdvisorSurprised;
		AdvisorHead.SetPosition(new Vector2(851, 0));
		AddChild(AdvisorHead);

		ImageTexture DialogBoxTexture = Util.LoadTextureFromPCX("Art/Advisors/dialogbox.pcx");
		TextureButton DialogBox = new TextureButton();
		DialogBox.TextureNormal = DialogBoxTexture;
		DialogBox.SetPosition(new Vector2(806, 110));
		AddChild(DialogBox);

		//TODO: Multi-line capabilities
		Label DialogBoxAdvise = new Label();
		DialogBoxAdvise.AddColorOverride("font_color", new Color(0, 0, 0));
		DialogBoxAdvise.Text = "You are running C7!";
		DialogBoxAdvise.SetPosition(new Vector2(815, 119));
		AddChild(DialogBoxAdvise);
		
		ImageTexture GoBackTexture = Util.LoadTextureFromPCX("Art/exitBox-backgroundStates.pcx", 0, 0, 72, 48);
		TextureButton GoBackButton = new TextureButton();
		GoBackButton.TextureNormal = GoBackTexture;
		GoBackButton.SetPosition(new Vector2(952, 720));
		AddChild(GoBackButton);
		GoBackButton.Connect("pressed", this, "ReturnToMenu");
	}

	private void ReturnToMenu()
	{
		GetParent().EmitSignal("hide");
	}
}
