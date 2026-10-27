extends Control
## A spatial overview of the continuous 3D scroll, independent of the day's chronology.
signal seek(x: float)
var center=-3.0
var dragging=false
const TRACK=Rect2(0,11,824,10)

func _ready() -> void:
	mouse_default_cursor_shape=Control.CURSOR_POINTING_HAND
	mouse_filter=Control.MOUSE_FILTER_STOP

func set_center(x: float) -> void:
	center=x
	queue_redraw()

func _draw() -> void:
	draw_line(Vector2(0,16),Vector2(824,16),Color("b6ab91"),2)
	for x in [-21.0,-14.0,-3.0,8.0,23.0]:
		draw_circle(Vector2((x+23)/46*824,16),3,Color("9e947d"))
	var p=(center+23)/46*824
	draw_rect(Rect2(clampf(p-86,0,652),7,172,18),Color(.28,.43,.37,.16))
	draw_line(Vector2(p,5),Vector2(p,27),Color("9c4838"),3)

func _gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.button_index==MOUSE_BUTTON_LEFT:
		dragging=event.pressed
		if dragging: seek.emit(clampf(event.position.x/824,0,1)*46-23)
		accept_event()
	elif event is InputEventMouseMotion and dragging:
		seek.emit(clampf(event.position.x/824,0,1)*46-23)
		accept_event()
