using Godot;

public class DisbandConfirmation : TextureRect
{

	public DisbandConfirmation() 
	{

	}

	public override void _Ready()
	{
		base._Ready();

		//Dimensions in-game are 530x320
		//The top 110px are for the advisor leaderhead, Domestic in this case.
		//For some reason it uses the Happy graphics.

		//Create a transparent texture background of the appropriate size.
		//This is super important as if we just add the children, the parent won't be able to figure
		//out the size of this TextureRect, and it won't be able to align it properly.
		//I added an extra 10 px on width for margin... maybe we should do margin another way, but 
		//this works reliably.
		ImageTexture thisTexture = new ImageTexture();
		Image image = new Image();
		image.Create(540, 320, false, Image.Format.Rgba8);
		image.Fill(Color.Color8(0, 0, 0, 0));
		thisTexture.CreateFromImage(image);
		this.Texture = thisTexture;


		ImageTexture AdvisorHappy = Util.LoadTextureFromPCX("Art/SmallHeads/popupDOMESTIC.pcx", 1, 40, 149, 110);
		TextureRect AdvisorHead = new TextureRect();
		AdvisorHead.Texture = AdvisorHappy;
		//Appears at 400, 110 in game, but leftmost 25px are transparent with default graphics
		AdvisorHead.SetPosition(new Vector2(375, 0));
		AddChild(AdvisorHead);

		//The pop-up part is the tricky part
		ImageTexture topLeftPopup = Util.LoadTextureFromPCX("Art/popupborders.pcx", 251, 1, 61, 44);
		ImageTexture topCenterPopup = Util.LoadTextureFromPCX("Art/popupborders.pcx", 313, 1, 61, 44);
		ImageTexture topRightPopup = Util.LoadTextureFromPCX("Art/popupborders.pcx", 375, 1, 61, 44);
		ImageTexture middleLeftPopup = Util.LoadTextureFromPCX("Art/popupborders.pcx", 251, 46, 61, 44);
		ImageTexture middleCenterPopup = Util.LoadTextureFromPCX("Art/popupborders.pcx", 313, 46, 61, 44);
		ImageTexture middleRightPopup = Util.LoadTextureFromPCX("Art/popupborders.pcx", 375, 46, 61, 44);
		ImageTexture bottomLeftPopup = Util.LoadTextureFromPCX("Art/popupborders.pcx", 251, 91, 61, 44);
		ImageTexture bottomCenterPopup = Util.LoadTextureFromPCX("Art/popupborders.pcx", 313, 91, 61, 44);
		ImageTexture bottomRightPopup = Util.LoadTextureFromPCX("Art/popupborders.pcx", 375, 91, 61, 44);

		//Dimensions are 530x320.  The leaderhead takes up 110.  So the popup is 530x210.
		//We have multiples of... 62? For the horizontal dimension, 45 for vertical.
		//45 does not fit into 210.  90, 135, 180, 215.  Well, 215 is sorta closeish.
		//62, we got 62, 124, 248, 496, 558.  Doesn't match up at all.
		//Which means that partial textures can be used.  Lovely.

		//Let's try adding some helper functions so this can be refactored later into a more general-purpose popup popper
		int vOffset = 110;
		int height = 320;
		drawRowOfPopup(vOffset, 530, topLeftPopup, topCenterPopup, topRightPopup);
		const int VTILE_SIZE = 44;
		vOffset+=VTILE_SIZE;
		for (;vOffset < height - VTILE_SIZE; vOffset += VTILE_SIZE) {
			drawRowOfPopup(vOffset, 530, middleLeftPopup, middleCenterPopup, middleRightPopup);
		}
		vOffset = height - VTILE_SIZE;
		drawRowOfPopup(vOffset, 530, bottomLeftPopup, bottomCenterPopup, bottomRightPopup);


		//Pop-up done.  Should refactor it someday so it's reusable.  But for now let's add the other things and stuff
		HBoxContainer header = new HBoxContainer();
		header.Alignment = BoxContainer.AlignMode.Center;
		Label advisorType = new Label();
		advisorType.AddColorOverride("font_color", new Color(0, 0, 0));
		advisorType.Set("custom_fonts/settings/size", 24);  //doesn't work
		// advisorType.Get("custom_fonts/font").size = 24;
		advisorType.GetFont("font").Set("size", 24);    //doesn't work
		//advisorType.GetFont("font").Size = 24;    //Internet says it works in GDScript, not a valid property in C#
		advisorType.Text = "Domestic Advisor";
		//Probably going to have to figure out themes for the font size.  Seems odd to add a theme for one Label,
		//but maybe it'll get reused, and I can't figure out how to one-off override the font size.

		header.AddChild(advisorType);
		header.SetPosition(new Vector2(0, 120));
		header.AnchorLeft = 0.0f;
		header.AnchorRight = 1.0f;
		// header.HintTooltip = "Help!";
		header.MarginRight = 10;    //For some reason this isn't causing it to be indented 10 pixels from the right.  Uncomment the line above and you'll see the tooltip goes all the way across
		AddChild(header);

	}

	private void drawRowOfPopup(int vOffset, int width, ImageTexture left, ImageTexture center, ImageTexture right)
	{
		//Okay, at least we only need one function for all three rows, it can be SIMD (single instruction, multiple data) by analogy
		TextureRect leftRectangle = new TextureRect();
		leftRectangle.SetPosition(new Vector2(0, vOffset));
		leftRectangle.Texture = left;
		AddChild(leftRectangle);

		const int TILE_SIZE = 61;   //yes, it will always be 61.  at least with Civ graphics.  so like WildWeazel, it will be hard coded
		int leftOffset = TILE_SIZE;
		for (;leftOffset < width - TILE_SIZE; leftOffset += TILE_SIZE)
		{
			TextureRect middleRectangle = new TextureRect();
			middleRectangle.SetPosition(new Vector2(leftOffset, vOffset));
			middleRectangle.Texture = center;
			AddChild(middleRectangle);
		}

		leftOffset = width - TILE_SIZE;
		TextureRect rightRectangle = new TextureRect();
		rightRectangle.SetPosition(new Vector2(leftOffset, vOffset));
		rightRectangle.Texture = right;
		AddChild(rightRectangle);
	}
}
