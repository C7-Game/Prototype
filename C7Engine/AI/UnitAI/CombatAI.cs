using System.Collections.Generic;
using C7GameData;
using C7GameData.AIData;
using Serilog;

namespace C7Engine.AI.UnitAI {
	/// <summary>
	/// A unit whose intended role is combat, other than purely point-defense combat.
	///
	/// This is likely to evolve significantly, and likely have sub-classes; this is the very first iteration.
	/// This first iteration is focused on defeating barbarians.
	/// </summary>
	public class CombatAI : C7Engine.UnitAI {
		private Tile target;

		private ILogger log = Log.ForContext<CombatAI>();

		public bool PlayTurn(Player player, MapUnit unit) {
			CombatAIData explorerData = (CombatAIData)unit.currentAIData;

			while (unit.movementPoints.remaining > 0) {
				Tile t = explorerData.path.Next();
				if (t == Tile.NONE) {
					break;
				}

				//Todo: Is there a less clunky way to do this?
				//Uhhhmm, actually yes, there's a move along path method.  The things is the path has
				//to be on the unit, rather than the unit AI.  Looks like we're only using that for
				//human-inputted paths (go to for tiles more than one tile away).
				foreach (KeyValuePair<TileDirection, Tile> neighbor in unit.location.neighbors) {
					if (neighbor.Value == t) {
						unit.move(neighbor.Key);
					}
				}
			}
			return true;
		}
	}
}
