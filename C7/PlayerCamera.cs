using Godot;
using C7.Map;
using C7GameData;

public partial class PlayerCamera : Camera2D
{
	private readonly float maxZoom = 3.0f;
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
		switch (@event) {
		case InputEventMouseMotion mouseDrag when mouseDrag.ButtonMask == MouseButtonMask.Left:
			Position -= mouseDrag.Relative / Zoom;
			break;
		case InputEventMagnifyGesture magnifyGesture:
			scaleZoom(magnifyGesture.Factor);
			break;
		}
	}

	public Rect2 getVisibleWorld() {
		Transform2D vpToGlobal = (GetViewport().GlobalCanvasTransform * GetCanvasTransform()).AffineInverse();
		return vpToGlobal * GetViewportRect();
	}

	public void centerOnTile(Tile tile,  MapView map) {
		Vector2 target = map.tileToLocal(tile);
		Position = target;
	}
}
