using Godot;
using C7GameData;

namespace C7.Map {

	// MapViewCamera position and zoom should only be modified through the
	// following methods:
	// - scaleZoom
	// - setZoom
	// - setPosition
	// This is because these methods will ensure that MapViewCamera handles
	// world wrapping on the MapView automatically.
	public partial class MapViewCamera : Camera2D {
		private readonly float maxZoom = 3.0f;
		private readonly float minZoom = 0.2f;
		public float zoomFactor { get; private set; } = 1.0f;
		private MapView map;
		private HorizontalWrapState hwrap = HorizontalWrapState.None;

		public void attachToMapView(MapView map) {
			this.map = map;
		}

		public override void _Ready() {
			base._Ready();
			scaleZoom(zoomFactor);
		}

		public void scaleZoom(float factor) {
			zoomFactor = zoomFactor * factor;
			zoomFactor = Mathf.Clamp(zoomFactor, minZoom, maxZoom);
			Zoom = Vector2.One * zoomFactor;
			checkWorldWrap();
		}

		public void setZoom(float factor) {
			zoomFactor = Mathf.Clamp(factor, minZoom, maxZoom);
			Zoom = Vector2.One * zoomFactor;
			checkWorldWrap();
		}

		public void setPosition(Vector2 position) {
			Position = position;
			checkWorldWrap();
		}

		private void checkWorldWrap() {
			if (map is null || !map.wrapHorizontally) {
				return;
			}
			Rect2 visible = getVisibleWorld();
			float rhs = visible.End.X;
			float lhs = visible.Position.X;
			if (hwrap != HorizontalWrapState.Right && rhs >= map.worldEdgeRight) {
				hwrap = HorizontalWrapState.Right;
				map.setHorizontalWrap(hwrap); // move wrapping map
			} else if (hwrap == HorizontalWrapState.Right && rhs < map.worldEdgeRight) {
				hwrap = HorizontalWrapState.None;
			}
			if (hwrap != HorizontalWrapState.Left && lhs <= map.worldEdgeLeft) {
				hwrap = HorizontalWrapState.Left;
				map.setHorizontalWrap(hwrap); // move wrapping map
			} else if (hwrap == HorizontalWrapState.Left && lhs > map.worldEdgeLeft) {
				hwrap = HorizontalWrapState.None;
			}

			// jump back into original map
			if (hwrap == HorizontalWrapState.Right && rhs >= map.worldEdgeRight + map.pixelWidth) {
				Translate(Vector2.Left * map.pixelWidth); // at rhs of wrapping map
			} else if (hwrap == HorizontalWrapState.Left && lhs <= map.worldEdgeLeft - map.pixelWidth) {
				Translate(Vector2.Right * map.pixelWidth); // at lhs of wrapping map
			}
		}

		public override void _UnhandledInput(InputEvent @event) {
			switch (@event) {
				case InputEventMouseMotion mouseDrag when mouseDrag.ButtonMask == MouseButtonMask.Left:
					setPosition(Position - mouseDrag.Relative / Zoom);
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

		public void centerOnTile(Tile tile, MapView map) {
			Vector2 target = map.tileToLocal(tile);
			setPosition(target);
		}

		public bool isTileInView(Tile tile, MapView map) {
			Rect2 visible = getVisibleWorld();
			Vector2 target = map.tileToLocal(tile);
			float size = 30;
			target -= Vector2.One * (size / 2);
			Rect2 boundingBox = new Rect2(target, size, size);
			return visible.Encloses(boundingBox);
		}
	}
}
