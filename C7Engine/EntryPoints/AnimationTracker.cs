namespace C7Engine
{
using System.Collections.Generic;
using System.Linq;
using C7GameData;

public class AnimationTracker {
	public delegate void OnAnimationCompleted(string unitGUID, MapUnit.AnimatedAction action);

	public static readonly OnAnimationCompleted doNothing = (unitGUID, action) => {};

	public struct ActiveAnimation {
		public ulong startTimeMS, endTimeMS;
		public MapUnit.AnimatedAction action;
		public OnAnimationCompleted callback;
	}

	private Dictionary<string, ActiveAnimation> activeAnims = new Dictionary<string, ActiveAnimation>();

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
	}

	public bool hasCurrentAction(string unitGUID)
	{
		return activeAnims.ContainsKey(unitGUID);
	}

	public (MapUnit.AnimatedAction, double) getCurrentActionAndPeriod(string unitGUID, ulong currentTimeMS)
	{
		ActiveAnimation aa = activeAnims[unitGUID];
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
			aa.callback(unitGUID, aa.action);
			keysToRemove.Add(unitGUID);
		}
		foreach (var key in keysToRemove)
			activeAnims.Remove(key);
	}
}

}
