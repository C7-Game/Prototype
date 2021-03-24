using Godot;
using System;
using ConvertCiv3Media;

public class MainMenu : Node2D
{
	// Declare member variables here. Examples:
	// private int a = 2;
	// private string b = "text";
	
	public string Civ3Path = Util.GetCiv3Path();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("Hello world!");
		DisplayTitleScreen();
	}
	
	private void DisplayTitleScreen()
	{	
		Pcx PcxTxtr = new Pcx(Civ3Path + "/Art/title.pcx");
		ImageTexture Txtr = PCXToGodot.getImageTextureFromPCX(PcxTxtr);
		
		TextureRect texture = new TextureRect();
		texture.Texture = Txtr;
		AddChild(texture);
 
		Button newButton = new Button();
		newButton.Text = "Testing 123";
		AddChild(newButton);
		newButton.Connect("pressed", this, "_on_Button_pressed");
	}

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }

	public void _on_Button_pressed()
	{
		GD.Print("Load button pressed");
		GetTree().ChangeScene("res://C7Game.tscn");    
	}
}
