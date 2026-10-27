extends Control
## Pointer gestures belong to the panorama only; cards and buttons consume their own input.
signal pan(delta_pixels: float)
signal coast(velocity_pixels: float)
signal tap(at: Vector2)
var pressed=false
var moved=false
var travel=0.0
var speed=0.0
var last_event=0

func _ready() -> void:
	mouse_filter=Control.MOUSE_FILTER_STOP
	mouse_default_cursor_shape=Control.CURSOR_DRAG

func _gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		if event.button_index in [MOUSE_BUTTON_LEFT,MOUSE_BUTTON_RIGHT]:
			if event.pressed:
				pressed=true
				moved=false
				travel=0
				speed=0
				last_event=Time.get_ticks_msec()
				coast.emit(0.0)
			else:
				if pressed:
					if moved: coast.emit(speed if Time.get_ticks_msec()-last_event<120 else 0.0)
					elif event.button_index==MOUSE_BUTTON_LEFT: tap.emit(event.position+global_position)
				pressed=false
		elif event.pressed:
			if event.button_index in [MOUSE_BUTTON_WHEEL_UP,MOUSE_BUTTON_WHEEL_LEFT]: pan.emit(150.0)
			if event.button_index in [MOUSE_BUTTON_WHEEL_DOWN,MOUSE_BUTTON_WHEEL_RIGHT]: pan.emit(-150.0)
		accept_event()
	elif event is InputEventMouseMotion and pressed:
		var now=Time.get_ticks_msec()
		var dt=maxf((now-last_event)/1000.0,.008)
		last_event=now
		travel+=event.relative.length()
		moved=travel>6
		if moved:
			speed=lerpf(speed,event.relative.x/dt,.45)
			pan.emit(event.relative.x)
		accept_event()
