
// The purpose of the AnimationTracker is to store the state of all ongoing animations in a module separate from the rest of the engine. It doesn't do
// anything with the animations, it simply keeps record of them while they're playing then calls a callback function when they're done. So it's
// basically just a stopwatch. Its update function must be called regularly so it can follow the passage of time, right now this is done in the Game
// class's _Process method.

// I pulled AnimationTracker out from the rest of the engine so it can more easily be moved into the UI module later if wanted. Still that would be
// awkward since the UI doesn't have much to do with animations, it only cares about drawing them, compared to the engine which has the much more
// complex task of synchronizing them with the rest of the game state. Though the problem with leaving the engine in control of animations is that it
// pulls in a lot of additional baggage. Specifically it means the engine must know about real world time, unit INI files, and the player's interface
// settings.

// The callbacks are hopefully temporary. I don't like using them since they obscure control flow as they get called at some later time potentially by
// a different thread. The threading issue doesn't matter at the moment since everything important runs on one thread but this could change if we want
// to have separate UI and engine threads (as I believe we should). A better design would be to use async & await.

namespace C7Engine
{
using System.Collections.Generic;
using System.Linq;
using C7GameData;

public class AnimationTracker {
	public delegate bool OnAnimationCompleted(string unitGUID, MapUnit.AnimatedAction action);

	public static readonly OnAnimationCompleted doNothing = (unitGUID, action) => { return true; };

	public struct ActiveAnimation {
		public ulong startTimeMS, endTimeMS;
		public MapUnit.AnimatedAction action;
		public OnAnimationCompleted callback;
	}

	private Dictionary<string, ActiveAnimation> activeAnims    = new Dictionary<string, ActiveAnimation>();
	private Dictionary<string, ActiveAnimation> completedAnims = new Dictionary<string, ActiveAnimation>();

	public void startAnimation(ulong currentTimeMS, string unitGUID, MapUnit.AnimatedAction action, OnAnimationCompleted callback)
	{
		ulong animDurationMS = 500; // Hard-code durations to 0.5 sec for now. Ultimately we'll want to figure this out based on the INI file.

		ActiveAnimation aa;
		if (activeAnims.TryGetValue(unitGUID, out aa)) {
			// If there's already an animation playing for this unit, end it first before replacing it
			aa.callback(unitGUID, aa.action);
		}
		aa = new ActiveAnimation { startTimeMS = currentTimeMS, endTimeMS = currentTimeMS + animDurationMS, action = action, callback = callback ?? doNothing };

		activeAnims[unitGUID] = aa;
		completedAnims.Remove(unitGUID);
	}

	public void endAnimation(string unitGUID, bool triggerCallback = true)
	{
		ActiveAnimation aa;
		if (triggerCallback && activeAnims.TryGetValue(unitGUID, out aa)) {
			var forget = aa.callback(unitGUID, aa.action);
			if (! forget)
				completedAnims[unitGUID] = aa;
			activeAnims.Remove(unitGUID);
		} else {
			activeAnims   .Remove(unitGUID);
			completedAnims.Remove(unitGUID);
		}
	}

	public bool hasCurrentAction(string unitGUID)
	{
		return activeAnims.ContainsKey(unitGUID) || completedAnims.ContainsKey(unitGUID);
	}

	public (MapUnit.AnimatedAction, double) getCurrentActionAndPeriod(string unitGUID, ulong currentTimeMS)
	{
		ActiveAnimation aa;
		if (! activeAnims.TryGetValue(unitGUID, out aa))
			aa = completedAnims[unitGUID];

		var durationMS = (double)(aa.endTimeMS - aa.startTimeMS);
		if (durationMS <= 0.0)
			durationMS = 1.0;
		var period = (double)(currentTimeMS - aa.startTimeMS) / durationMS;
		return (aa.action, period);
	}

	public void update(ulong currentTimeMS)
	{
		var keysToRemove = new List<string>();
		foreach (var guidAAPair in activeAnims.Where(guidAAPair => guidAAPair.Value.endTimeMS <= currentTimeMS)) {
			var (unitGUID, aa) = (guidAAPair.Key, guidAAPair.Value);
			var forget = aa.callback(unitGUID, aa.action);
			if (! forget)
				completedAnims[unitGUID] = aa;
			keysToRemove.Add(unitGUID);
		}
		foreach (var key in keysToRemove)
			activeAnims.Remove(key);
	}
}

}
