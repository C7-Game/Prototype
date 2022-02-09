// My intention is that eventually AnimatedEffect will be a class with some useful stuff in it. Right now it's just an enum that lets the engine
// communicate to the UI which Civ 3 special effect it wants to trigger. The UI must provide the file names for the animations, unfortunately they
// can't be guessed based on the enum names (like MapUnit.AnimatedAction) because the files aren't all stored in predictable folder locations.
namespace C7GameData
{
public enum AnimatedEffect {
	HIT,
	HIT2,
	HIT3,
	HIT5,
	MISS,
	WATER_MISS
}
}
