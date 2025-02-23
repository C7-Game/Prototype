using C7GameData;
using C7GameData.AIData;

namespace C7Engine {
	//Not-fully-fleshed out player unit AI.
	//Right now it's kind of random to just add some appearance
	//of stuff being done.  I.e. it kinda sucks.  But that's okay.
	//It has to start somewhere, right?
	public interface UnitAI {
		bool PlayTurn(Player player, MapUnit unit);
	}
}
