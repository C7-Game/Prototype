
using System;
using C7GameData;
using Godot;

public partial class TechBox : TextureButton {
	private Tech tech;
	private TechState techState;

	public enum TechState {
		// This tech is known to the player.
		kKnown,
		// The player is actively researching this tech.
		kInProgress,
		// The player could research this tech.
		kPossible,
		// The player needs to research the prerequisites before this tech can
		// be researched.
		kBlocked,
	}

	public TechBox(Tech tech, TechState techState) {
		this.tech = tech;
		this.techState = techState;
	}

	public override void _Ready() {
		// TODO: Figure out how to pick which of the different sized tech boxes
		// we should use for a given tech.
		//
		// NOTE: this pcx has 16 rows, 4 per era, with different sizes.
		//
		// NOTE: the X coordinates of each column were found via guess+check.
		ImageTexture knownTechBox = Util.LoadTextureFromPCX("Art/Advisors/techboxes.pcx",
			1, 1, 180, 80);
		ImageTexture inProgressTechBox = Util.LoadTextureFromPCX("Art/Advisors/techboxes.pcx",
			192, 1, 180, 80);
		ImageTexture possibleTechBox = Util.LoadTextureFromPCX("Art/Advisors/techboxes.pcx",
			381, 1, 180, 80);
		ImageTexture blockedTechBox = Util.LoadTextureFromPCX("Art/Advisors/techboxes.pcx",
			568, 1, 180, 80);

		TextureNormal = techState switch {
			TechState.kKnown => knownTechBox,
			TechState.kInProgress => inProgressTechBox,
			TechState.kPossible => possibleTechBox,
			TechState.kBlocked => blockedTechBox,
			_ => throw new ArgumentOutOfRangeException("Invalid tech state")
		};

		ImageTexture iconTexture = Util.LoadTextureFromPCX(tech.SmallIconPath);
		TextureRect icon = new() {
			Texture = iconTexture
		};
		icon.SetPosition(new Vector2(12, 32));
		AddChild(icon);

		// TODO: Have the name trail off if it is too large for the box.
		Label techName = new() {
			Text = tech.Name,
			OffsetLeft = 12,
			OffsetTop = 12
		};
		AddChild(techName);
	}
}
