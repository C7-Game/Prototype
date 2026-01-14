namespace C7GameData {
	// My intention is that eventually AnimatedEffect will be a class with some useful stuff in it. Right now it's just an enum that lets the engine
	// communicate to the UI which Civ 3 special effect it wants to trigger. The UI must provide the file names for the animations, unfortunately they
	// can't be guessed based on the enum names (like MapUnit.AnimatedAction) because the files aren't all stored in predictable folder locations.
	// These enum values match the names of animation files in civ3PTW/Art/Animations/Trajectory. The actual mapping from enum value to file name is
	// determined by a dictionary in the Civ3Anim class (in Civ3AnimData.cs, part of the UI). TODO: Ultimately we'll want a way for the engine to specify
	// effect animations that is extensible for modders and not limited to Civ 3's animations.
	public enum AnimatedEffect {
		Hit,
		Hit2,
		Hit3,
		Hit5,
		Miss,
		WaterMiss
	}

	// Controls what happens to an animation once its time is up
	public enum AnimationEnding {
		Stop, // Automatically end animation and return to default pose
		Pause, // Hold animation on last frame until it's ended or replaced by another
		Repeat // Restart animation from beginning
	}
}
