
// The purpose of the AnimationTracker is to store the state of all ongoing animations in a module separate from the rest of the UI. It doesn't do
// anything with the animations, it simply keeps record of them while they're playing then calls a callback function when they're done. So it's
// basically just a stopwatch. Its update function must be called regularly so it can follow the passage of time, right now this is done in the Game
// class's _Process method. There is one instance of AnimationTracker and it is located in Game. TODO: Consider moving it to MapView.

// The callbacks are hopefully temporary. I don't like using them since they obscure control flow as they get called at some later time potentially by
// a different thread. The threading issue doesn't matter at the moment since everything important runs on one thread but this could change if we want
// to have separate UI and engine threads (as I believe we should).

using System;
using System.Collections.Generic;
using System.Linq;
using C7GameData;
using C7Engine; // for IAnimationControl, OnAnimationCompleted

public class AnimationTracker : IAnimationControl {
	public static readonly OnAnimationCompleted doNothing = (unitGUID, action) => { return true; };

	private Civ3UnitAnim civ3UnitAnim;

	public AnimationTracker(Civ3UnitAnim civ3UnitAnim)
	{
		this.civ3UnitAnim = civ3UnitAnim;
	}

	public struct ActiveAnimation {
		public long startTimeMS, endTimeMS;
		public MapUnit.AnimatedAction action;
		public OnAnimationCompleted callback;
	}

	private Dictionary<string, ActiveAnimation> activeAnims    = new Dictionary<string, ActiveAnimation>();
	private Dictionary<string, ActiveAnimation> completedAnims = new Dictionary<string, ActiveAnimation>();

	public long getCurrentTimeMS()
	{
		return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
	}

	public void startAnimation(MapUnit unit, MapUnit.AnimatedAction action, OnAnimationCompleted callback)
	{
		long currentTimeMS = getCurrentTimeMS();
		long animDurationMS = (long)(1000.0 * civ3UnitAnim.getDuration(unit.unitType.name, action));

		ActiveAnimation aa;
		if (activeAnims.TryGetValue(unit.guid, out aa)) {
			// If there's already an animation playing for this unit, end it first before replacing it
			aa.callback(unit.guid, aa.action);
		}
		aa = new ActiveAnimation { startTimeMS = currentTimeMS, endTimeMS = currentTimeMS + animDurationMS, action = action, callback = callback ?? doNothing };

		civ3UnitAnim.playSound(unit.unitType.name, action);

		activeAnims[unit.guid] = aa;
		completedAnims.Remove(unit.guid);
	}

	public void endAnimation(MapUnit unit, bool triggerCallback = true)
	{
		ActiveAnimation aa;
		if (triggerCallback && activeAnims.TryGetValue(unit.guid, out aa)) {
			var forget = aa.callback(unit.guid, aa.action);
			if (! forget)
				completedAnims[unit.guid] = aa;
			activeAnims.Remove(unit.guid);
		} else {
			activeAnims   .Remove(unit.guid);
			completedAnims.Remove(unit.guid);
		}
	}

	public bool hasCurrentAction(MapUnit unit)
	{
		return activeAnims.ContainsKey(unit.guid) || completedAnims.ContainsKey(unit.guid);
	}

	public (MapUnit.AnimatedAction, double) getCurrentActionAndRepetitionCount(MapUnit unit)
	{
		ActiveAnimation aa;
		if (! activeAnims.TryGetValue(unit.guid, out aa))
			aa = completedAnims[unit.guid];

		var durationMS = (double)(aa.endTimeMS - aa.startTimeMS);
		if (durationMS <= 0.0)
			durationMS = 1.0;
		var repCount = (double)(getCurrentTimeMS() - aa.startTimeMS) / durationMS;
		return (aa.action, repCount);
	}

	public void update()
	{
		long currentTimeMS = getCurrentTimeMS();
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

	public MapUnit.ActiveAnimation getActiveAnimation(MapUnit unit)
	{
		if (hasCurrentAction(unit)) {
			var (action, repCount) = getCurrentActionAndRepetitionCount(unit);

			var isNonRepeatingAction =
				(action == MapUnit.AnimatedAction.RUN) ||
				(action == MapUnit.AnimatedAction.DEATH) ||
				(action == MapUnit.AnimatedAction.FORTIFY) ||
				(action == MapUnit.AnimatedAction.VICTORY) ||
				(action == MapUnit.AnimatedAction.BUILD);

			float progress;
			if (isNonRepeatingAction)
				progress = (repCount <= 1.0) ? (float)repCount : 1f;
			else
				progress = (float)(repCount - Math.Floor(repCount));

			float offsetX = 0, offsetY = 0;
			if (action == MapUnit.AnimatedAction.RUN) {
				(int dX, int dY) = unit.facingDirection.toCoordDiff();
				offsetX = -1 * dX * (1f - progress);
				offsetY = -1 * dY * (1f - progress);
			}

			return new MapUnit.ActiveAnimation {
				action = action,
					direction = unit.facingDirection,
					progress = progress,
					offsetX = offsetX,
					offsetY = offsetY
					};
		} else
			return new MapUnit.ActiveAnimation {
				action = unit.isFortified ? MapUnit.AnimatedAction.FORTIFY : MapUnit.AnimatedAction.DEFAULT,
					direction = unit.facingDirection,
					progress = 1f,
					offsetX = 0f,
					offsetY = 0f
					};
	}
}
