using Godot;
using System;

public partial class PlayerCamera : Camera2D
{
	private readonly float maxZoom = 2.0f;
	private readonly float minZoom = 0.2f;
	public float zoomFactor {get; private set; } = 1.0f;

	public override void _Ready() {
		base._Ready();
		scaleZoom(zoomFactor);
	}

	public void scaleZoom(float factor) {
		zoomFactor = zoomFactor * factor;
		zoomFactor = Mathf.Clamp(zoomFactor, minZoom, maxZoom);
		Zoom = Vector2.One * zoomFactor;
	}

	public void setZoom(float factor) {
		zoomFactor = Mathf.Clamp(factor, minZoom, maxZoom);
		Zoom = Vector2.One * zoomFactor;
	}

	public override void _UnhandledInput(InputEvent @event) {
		if (@event is InputEventMouseMotion mm && mm.ButtonMask == MouseButtonMask.Left) {
			Position -= mm.Relative / Zoom;
		}
		if (@event is InputEventMagnifyGesture mg) {
			scaleZoom(mg.Factor);
		}
	}

	public Rect2 getVisibleWorld() {
		Transform2D vpToGlobal = (GetViewport().GlobalCanvasTransform * GetCanvasTransform()).AffineInverse();
		return vpToGlobal * GetViewportRect();
	}
}
