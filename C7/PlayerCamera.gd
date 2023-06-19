extends Camera2D

const MAX_ZOOM: float = 2.0
const MIN_ZOOM: float = 0.1

var zoom_factor: float = 1.0

func _unhandled_input(event: InputEvent):
	if event is InputEventMouseMotion:
		if event.button_mask == MOUSE_BUTTON_MASK_LEFT:
			self.position -= (event.relative / self.zoom)

	if event is InputEventMagnifyGesture:
		zoom_factor = clampf(zoom_factor * event.factor, MIN_ZOOM, MAX_ZOOM)
		self.zoom = Vector2.ONE * zoom_factor
