using Godot;
using System;
using Serilog;
using System.Collections.Generic;

/**
 * Handles managing the advisor screens.
 * Showing them, hiding them... maybe some other things eventually.
 * This is part of the effort to de-centralize from Game.cs and be more event driven.
 */
public partial class Advisors : CenterContainer {
	private ILogger log = LogManager.ForContext<Advisors>();

	private DomesticAdvisor domesticAdvisor;
	private ScienceAdvisor scienceAdvisor;

	// A list of all the non-null advisors, so we can hide them whenever we
	// draw a different advisor.
	private List<TextureRect> advisors = new();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		//Center the advisor container.  Following directions at https://docs.godotengine.org/en/stable/tutorials/gui/size_and_anchors.html?highlight=anchor
		//Also taking advantage of it being 1024x768, as the directions didn't really work.  This is not 100% ideal (would be great for a general-purpose solution to work),
		//but does work with the current graphics independent of resolution.
		this.Hide();
	}

	private void ShowLatestAdvisor() {
		log.Debug("Received request to show latest advisor");

		OnShowSpecificAdvisor("F1");
		this.Show();
	}

	private void _on_Advisor_hide() {
		this.Hide();
	}

	private void OnShowSpecificAdvisor(string advisorType) {
		// Hide any existing advisors so we can draw the requested one.
		foreach (TextureRect tr in advisors) {
			tr.Hide();
		}

		if (advisorType.Equals("F1")) {
			if (domesticAdvisor != null) {
				RemoveChild(domesticAdvisor);
				domesticAdvisor = null;
			}

			domesticAdvisor = new DomesticAdvisor();
			advisors.Add(domesticAdvisor);
			AddChild(domesticAdvisor);
			this.Show();
		}
		if (advisorType.Equals("F6")) {
			// TODO: What's the best way to refresh the tech tree UI without
			// adding too many children?
			if (scienceAdvisor != null) {
				RemoveChild(scienceAdvisor);
				scienceAdvisor = null;
			}

			scienceAdvisor = new ScienceAdvisor();
			advisors.Add(scienceAdvisor);
			AddChild(scienceAdvisor);
			this.Show();
		}
	}
}
