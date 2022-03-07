using System;
using System.Collections.Generic;

namespace C7Engine
{
	using C7GameData;

	/**
	 * A simple AI for choosing what to produce next.
	 * We probably will have a few variants of this, an interface, etc.
	 * eventually.  For now, I just want to separate it out from the main
	 * interaction events and make it clear that it's an AI component.
	 */
	public class CityProductionAI
	{
		
		/**
		 * Gets the next item to be produced in a given city.
		 * Not a final API; it probably has the wrong parameters.  The last item in this city shouldn't
		 * matter, unless the "always build previously build unit" option is enabled, in which case it isn't necessary
		 * to call this method, just build the same thing.
		 *
		 * But what are the right parameters?  That's a tougher question.  We might want to consider a bunch of things.
		 * If there's a war going on.  What victory condition we're going for.  If we're broke and need more marketplaces.
		 * Maybe we'll wind up with some sort of collection of AI parameters to pass someday?  For now I'm not going to
		 * get hung up on knowing exactly how it should be done the road.
		 */
		public static IProducible GetNextItemToBeProduced(City city, IProducible lastProduced)
		{
			Dictionary<string, UnitPrototype> unitPrototypes = EngineStorage.gameData.unitPrototypes;
			if (city.size >= 3) {
				return unitPrototypes["Settler"];
			}
			if (lastProduced == unitPrototypes["Warrior"]) {
				if (city.location.NeighborsWater()) {
					Random rng = new Random();
					if (rng.Next(3) == 0) {
						return unitPrototypes["Galley"];
					}
					else {
						return unitPrototypes["Chariot"];
					}
				}
				else {
					return unitPrototypes["Chariot"];
				}
			}
			else  {
				return unitPrototypes["Warrior"];
			}
		}
	}
}
