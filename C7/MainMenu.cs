using Godot;
using System;

public class MainMenu : Node2D
{
	// Declare member variables here. Examples:
	// private int a = 2;
	// private string b = "text";

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("Hello world!");
	}

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }

	private void _on_Button_pressed()
	{
		GD.Print("Load button pressed");
		GetTree().ChangeScene("res://C7Game.tscn");    
	}
}
