
using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using C7GameData;
using C7Engine;

public class AnimationTracker {
	private Civ3AnimData civ3AnimData;

	public AnimationTracker(Civ3AnimData civ3AnimData)
	{
		this.civ3AnimData = civ3AnimData;
	}

	public struct ActiveAnimation {
		public long startTimeMS, endTimeMS;
		public MapUnit.AnimatedAction action;
		public AutoResetEvent completionEvent;
	}

	private Dictionary<string, ActiveAnimation> activeAnims = new Dictionary<string, ActiveAnimation>();

	public long getCurrentTimeMS()
	{
		return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
	}

	private string getTileID(Tile tile)
	{
		// Generate a string to ID this tile that won't conflict with the unit GUIDs. TODO: Eventually we'll implement a common way of ID'ing
		// all game objects. Use that here instead.
		return String.Format("Tile.{0}.{1}", tile.xCoordinate, tile.yCoordinate);
	}

	private void startAnimation(string id, Civ3Anim anim, AutoResetEvent completionEvent)
	{
		long currentTimeMS = getCurrentTimeMS();
		long animDurationMS = (long)(1000.0 * anim.getDuration());

		ActiveAnimation aa;
		if (activeAnims.TryGetValue(id, out aa)) {
			// If there's already an animation playing for this unit, end it first before replacing it
			// TODO: Consider instead queueing up the new animation until after the first one is completed
			if (aa.completionEvent != null)
				aa.completionEvent.Set();
		}
		aa = new ActiveAnimation { startTimeMS = currentTimeMS, endTimeMS = currentTimeMS + animDurationMS, action = anim.action, completionEvent = completionEvent };

		anim.playSound();

		activeAnims[id] = aa;
	}

	public void startAnimation(MapUnit unit, MapUnit.AnimatedAction action, AutoResetEvent completionEvent)
	{
		startAnimation(unit.guid, civ3AnimData.forUnit(unit.unitType.name, action), completionEvent);
	}

	public void startAnimation(Tile tile, AnimatedEffect effect, AutoResetEvent completionEvent)
	{
		startAnimation(getTileID(tile), civ3AnimData.forEffect(effect), completionEvent);
	}

	public void endAnimation(MapUnit unit)
	{
		ActiveAnimation aa;
		if (activeAnims.TryGetValue(unit.guid, out aa)) {
			if (aa.completionEvent != null)
				aa.completionEvent.Set();
			activeAnims.Remove(unit.guid);
		}
	}

	public bool hasCurrentAction(MapUnit unit)
	{
		return activeAnims.ContainsKey(unit.guid);
	}

	public (MapUnit.AnimatedAction, double) getCurrentActionAndRepetitionCount(MapUnit unit)
	{
		ActiveAnimation aa = activeAnims[unit.guid];
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
			var (id, aa) = (guidAAPair.Key, guidAAPair.Value);
			if (aa.completionEvent != null)
				aa.completionEvent.Set();
			keysToRemove.Add(id);
		}
		foreach (var key in keysToRemove)
			activeAnims.Remove(key);
	}

	public MapUnit.Appearance getUnitAppearance(MapUnit unit)
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

			return new MapUnit.Appearance {
				action = action,
				direction = unit.facingDirection,
				progress = progress,
				offsetX = offsetX,
				offsetY = offsetY
				};
		} else
			return new MapUnit.Appearance {
				action = unit.isFortified ? MapUnit.AnimatedAction.FORTIFY : MapUnit.AnimatedAction.DEFAULT,
				direction = unit.facingDirection,
				progress = 1f,
				offsetX = 0f,
				offsetY = 0f
				};
	}
}
