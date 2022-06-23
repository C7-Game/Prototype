namespace C7Engine {
	/// <summary>
	/// For weighted actions, this class is used to help determine how the AI makes its decision.
	/// The idea is that for some things, the AI should always choose what it considers the best option.  But for other
	/// things, it should make a weighted decision so that there's some degree of randomness.
	///
	/// This both reflects human decision making, and makes things more interesting than if the AI always did the same
	/// thing.
	///
	/// In theory, with a good AI, ALWAYS_CHOOSE_HIGHEST_SCORE should give the strongest AI results.  Quadratic weighting
	/// squares the weights, and linear doesn't.  So e.g. if the weights for two options are 5 and 4, with always-highest,
	/// it will always choose the 5 option.  With linear, it will choose the 5 option 55.5% of the time.  With quadratic,
	/// it will choose the 5 option 25/41sts of the time, or 61% of the time.  Which is less of a boost than I expected,
	/// but is still a boost.
	///
	/// If the results are less close, the gap grows.  If it's 7 vs 3, linear gives 70/30, quadratic gives 49/58, or 84%.
	/// At 9 vs 1, it's 90/10 versus 98.8% for quadratic.  So it does work as intended - if the AI is pretty sure, it
	/// is much more likely to go with what it thinks is best; if it's only sort of sure, it's may second guess itself.
	/// </summary>
	public enum PrioritizationType {
		ALWAYS_CHOOSE_HIGHEST_SCORE,
		WEIGHTED_QUADRATIC,
		WEIGHTED_LINEAR,
	}
}
