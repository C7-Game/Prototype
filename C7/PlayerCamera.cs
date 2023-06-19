using Godot;
using System;

public partial class PlayerCamera : Camera2D
{
	private readonly float maxZoom = 2.0f;
	private readonly float minZoom = 0.2f;
	private float zoomFactor = 1.0f;

	public override void _Ready() {
		base._Ready();
		Zoom = Vector2.One * zoomFactor;
	}

	public override void _UnhandledInput(InputEvent @event) {
		if (@event is InputEventMouseMotion mm && mm.ButtonMask == MouseButtonMask.Left) {
			Position -= mm.Relative / Zoom;
		}
		if (@event is InputEventMagnifyGesture mg) {
			zoomFactor = Mathf.Clamp(zoomFactor * mg.Factor, minZoom, maxZoom);
			Zoom = Vector2.One * zoomFactor;
		}
	}

	public Rect2 getVisibleWorld() {
		Transform2D vpToGlobal = (GetViewport().GlobalCanvasTransform * this.GetCanvasTransform()).AffineInverse();
		return vpToGlobal * GetViewportRect();
	}
}
