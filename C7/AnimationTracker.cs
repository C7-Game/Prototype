
using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using C7GameData;
using C7Engine;

public partial class AnimationTracker {
	private AnimationManager civ3AnimData;
	public bool endAllImmediately = false; // If true, update() ends all running animations regardless of time remaining.

	public AnimationTracker(AnimationManager civ3AnimData)
	{
		this.civ3AnimData = civ3AnimData;
	}

	public struct ActiveAnimation {
		public long startTimeMS, endTimeMS;
		public AutoResetEvent completionEvent;
		public AnimationEnding ending;
		public C7Animation anim;
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

	private void startAnimation(string id, C7Animation anim, AutoResetEvent completionEvent, AnimationEnding ending)
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
		aa = new ActiveAnimation { startTimeMS = currentTimeMS, endTimeMS = currentTimeMS + animDurationMS, completionEvent = completionEvent,
			ending = ending, anim = anim };

		anim.playSound();

		activeAnims[id] = aa;
	}

	public void startAnimation(MapUnit unit, MapUnit.AnimatedAction action, AutoResetEvent completionEvent, AnimationEnding ending)
	{
		startAnimation(unit.id.ToString(), civ3AnimData.forUnit(unit.unitType, action), completionEvent, ending);
	}

	public void startAnimation(Tile tile, AnimatedEffect effect, AutoResetEvent completionEvent, AnimationEnding ending)
	{
		startAnimation(getTileID(tile), civ3AnimData.forEffect(effect), completionEvent, ending);
	}

	public void endAnimation(MapUnit unit)
	{
		ActiveAnimation aa;
		if (activeAnims.TryGetValue(unit.id.ToString(), out aa)) {
			if (aa.completionEvent != null)
				aa.completionEvent.Set();
			activeAnims.Remove(unit.id.ToString());
		}
	}

	public bool hasCurrentAction(MapUnit unit)
	{
		return activeAnims.ContainsKey(unit.id.ToString());
	}

	public (MapUnit.AnimatedAction, float) getCurrentActionAndProgress(string id)
	{
		ActiveAnimation aa = activeAnims[id];

		var durationMS = (double)(aa.endTimeMS - aa.startTimeMS);
		if (durationMS <= 0.0)
			durationMS = 1.0;

		var progress = (double)(getCurrentTimeMS() - aa.startTimeMS) / durationMS;
		if (aa.ending == AnimationEnding.Repeat)
			progress = progress - Math.Floor(progress);
		else if (progress > 1.0)
			progress = 1.0;

		return (aa.anim.action, (float)progress);
	}

	public (MapUnit.AnimatedAction, float) getCurrentActionAndProgress(MapUnit unit)
	{
		return getCurrentActionAndProgress(unit.id.ToString());
	}

	public (MapUnit.AnimatedAction, float) getCurrentActionAndProgress(Tile tile)
	{
		return getCurrentActionAndProgress(getTileID(tile));
	}

	public void update()
	{
		long currentTimeMS = (! endAllImmediately) ? getCurrentTimeMS() : long.MaxValue;
		var keysToRemove = new List<string>();
		foreach (var guidAAPair in activeAnims.Where(guidAAPair => guidAAPair.Value.endTimeMS <= currentTimeMS)) {
			var (id, aa) = (guidAAPair.Key, guidAAPair.Value);
			if (aa.completionEvent != null) {
				aa.completionEvent.Set();
				aa.completionEvent = null; // So event is only triggered once
			}
			if (aa.ending == AnimationEnding.Stop)
				keysToRemove.Add(id);
		}
		foreach (var key in keysToRemove)
			activeAnims.Remove(key);
	}

	public MapUnit.Appearance getUnitAppearance(MapUnit unit)
	{
		if (hasCurrentAction(unit)) {
			var (action, progress) = getCurrentActionAndProgress(unit);

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
		} else {
			return new MapUnit.Appearance {
				action = unit.isFortified ? MapUnit.AnimatedAction.FORTIFY : MapUnit.AnimatedAction.DEFAULT,
				direction = unit.facingDirection,
				progress = 1f,
				offsetX = 0f,
				offsetY = 0f
			};
		}
	}

	public C7Animation getTileEffect(Tile tile)
	{
		ActiveAnimation aa;
		if (activeAnims.TryGetValue(getTileID(tile), out aa))
			return aa.anim;
		else
			return null;
	}
}
