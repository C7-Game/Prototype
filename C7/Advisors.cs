using Godot;
using System;

/**
 * Handles managing the advisor screens.
 * Showing them, hiding them... maybe some other things eventually.
 * This is part of the effort to de-centralize from Game.cs and be more event driven.
 */
public class Advisors : CenterContainer
{
	

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		
	}

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }


	private void ShowLatestAdvisor()
	{
		GD.Print("Received request to show latest advisor");
		GD.Print("Creating and showing advisor");

		//Center the advisor container.  Following directions at https://docs.godotengine.org/en/stable/tutorials/gui/size_and_anchors.html?highlight=anchor
		//Also taking advantage of it being 1024x768, as the directions didn't really work.  This is not 100% ideal (would be great for a general-purpose solution to work),
		//but does work with the current graphics independent of resolution.
		this.MarginLeft = -512;
		this.MarginRight = -512;
		this.MarginTop = -384;
		this.MarginBottom = 384;

		if (this.GetChildCount() == 0) {
			DomesticAdvisor advisor = new DomesticAdvisor();
			AddChild(advisor);
		}
		this.Show();
	}
	
	
	private void _on_Advisor_hide()
	{
		this.Hide();
	}
}
