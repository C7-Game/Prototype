using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using C7GameData;
using C7GameData.AIData;

namespace C7Engine.AI.StrategicAI {
	/**
	 * This is a prototype interface.  The idea is that there may be many strategic priorities, and the AI should decide on a few.
	 * I could see this going a lot of different ways.  Some general thoughts/ideas:
	 *
	 *  - "Victory conditions" are an obvious candidate.  "Space race", "20K culture", etc. could, and at some point should, be strategic priorities.
	 *  - There should always be at least one priority.  E.g. early game it is very likely to be "Expansion" (unless it's a scenario...)
	 *  - Smaller-scope items will also play a role.  "Acquire iron", "defeat the Zulu", "build more science buildings" could all be examples.
	 *  - At some point there will need to be a ranking of priorities.  Probably a limited number of priorities (three?), since if everything's a priority,
	 *    you are working at a big company, err, I mean, nothing is.
	 *     - I like the idea of this being weighted, e.g. 0.6, 0.3, 0.1 for primary, secondary, tertiary priority.  Obviously we'll have to see how things go.
	 *  - The strategic priorities will be factored in to other AI decisions.  "What do I build?" being an obvious one.  If the goal is expansion, Settlers will
	 *    be a high priority.  But also decisions such as where to position the army.
	 *  - I expect we'll have some need for lower-level priorities as well, especially militarily.  Let's suppose our strategic priorities are "defeat the Zulu"
	 *    and "acquire iron".  Some military-focused AI component will figure out how to prioritize tiles, terrain, and forces to try to maximize the chances of that
	 *    happening.
	 *  - There should probably be a way to set a generic "no priority" so we can compare a targeted AI to one that is indifferent or random.  An AI that is making
	 *    decisions based on game state should be noticeable better, at least over the long term or in aggregate.
	 *  - Part of the goal is to have the AI "think like a human".  Obviously we won't be programming organic neural matter, but the theory is a high-level view will help.
	 *    We also may have to teach it to react to what the human is doing.  On the other hand, ideally the AI won't cheat, e.g. it won't have any additional info that it
	 *    is acting on beyond what a human would.  It may be ambitious for it to be competent this way, but some companies are decent at this, relatively at least.
	 *  - There also will need to be a factor for how often strategic priorities change.  They should not change every turn, or they aren't very strategic.  My thoughts:
	 *      - A semi-random interval should trigger re-evaluations.  Likely 20-30 turns.  Semi-randomized b/c it's too predictable if it happens e.g. exactly every 20 turns
	 *        for each AI.
	 *      - Priorities should be replaced if a different one scores considerably higher.  E.g. if diplomatic victory is only marginally more attractive than our old
	 *        cultural 100K priority, we shouldn't be flip-floppy.  Maybe make diplo a secondary/tertiary priority to start with.
	 *      - Certain events should cause an immediate re-evaluation.  Most notably, if another player declares war, that should trigger a re-evaluation, and likely
	 *        making "defend ourselves" a top priority.  The end of available land for settlers could be another.  But my expectation is these would only be major events
	 *        that would invalidate priorities/require new ones immediately.  The type of events where decades happen in weeks, to paraphrase Lenin.
	 *  - I expect there to be some sub-classes (sub-interfaces? whatever makes the most sense in C#).  E.g. VictoryTypePriority.  It may be desirable to always have at
	 *    least one of these as a priority, even if the chosen one is "generic score, we'll decide later" or "we aren't going to win, but we want to play spoiler and
	 *    capture the 20K city before it reaches 20K".
	 */
	public abstract class StrategicPriority {
		[JsonIgnore]
		protected float calculatedWeight;

		protected string key;
		protected Dictionary<string, string> properties = new Dictionary<string, string>();

		/**
		 * Returns a weight for how important this strategic priority is for the player.
		 * The implementation will examine the player's data, and figure out the weight based on that.
		 * The values should eventually be normalized somehow, but likely in relation to each other.
		 * Example game mechanic: Diplomacy in EU4.  Various game factors apply weights to the likelihood of diplomatic acceptance.  Some factors are intentionally
		 * extreme to guarantee or near-guarantee behavior; others are more subtle.  What's an appropriate weight is only really known relative to other weights.
		 *
		 * As a starting convention, I'll say that 100 should be considered "highly probable" and 1000 should be "guaranteed unless something else contraindicates it even more".
		 *
		 * Once returned, this value will be combined with the values for other StrategicPriority's, and the AI will choose priorities either straight-up based
		 * on the highest score, or potentially with a weighted/randomized factor thrown in.  This is one of the aspects that will require tuning.  I don't want it
		 * to be super-predictable that a Scientific AI always chooses Space Race, but a Scientific AI should choose Space Race more often than a non-Scientific AI.
		 */
		public abstract void CalculateWeightAndMetadata(Player player);

		public float GetCalculatedWeight() {
			return calculatedWeight;
		}

		/// <summary>
		/// Allows the priority to add a flat adjuster to the likelihood of building this item.
		/// This can be used to offset weights in the base adjuster, or simply as an alternative to the weight-based method.
		/// </summary>
		/// <param name="producible"></param>
		/// <returns></returns>
		public virtual float GetProductionItemFlatAdjuster(IProducible producible) {
			return 0.0f;
		}

		/// <summary>
		/// How much more or less likely a unit is to be produced based on this strategy.
		/// This will be weighted by the priority's rank (top priority = full effect)
		/// A value of 0.0 indicates no change to the prioritization.
		/// A value of 1.0 means a 100% bonus (twice as likely to build); 4.0 means five times as likely to build.
		/// A value of -1.0 means 100% less likely to build (will never build).
		/// </summary>
		/// <param name="producible"></param>
		/// <returns></returns>
		public virtual float GetProductionItemPreferenceWeight(IProducible producible) {
			if (producible is UnitPrototype prototype) {
				return 0.0f;
			}
			return 0.0f;
		}
	}
}
