extends Control
## A quiet seal, with its caption revealed on hover. Dragging still pans the scroll.
signal chosen
signal pan(amount: float)
var caption=""
var active=false
var hover=0.0
var clock=0.0
var held=false
var distance=0.0
var font: Font

func _ready() -> void:
	size=Vector2(48,48)
	mouse_filter=Control.MOUSE_FILTER_STOP
	mouse_default_cursor_shape=Control.CURSOR_POINTING_HAND
	font=preload("res://assets/fonts/NotoSerifSC.ttf")

func _process(delta: float) -> void:
	clock+=delta
	var over=Rect2(Vector2.ZERO,size).has_point(get_local_mouse_position())
	hover=move_toward(hover,1.0 if over else 0.0,delta*6.0)
	queue_redraw()

func _draw() -> void:
	var center=Vector2(24,24)
	var ink=Color("954d39") if active else Color("ede5d1")
	draw_circle(center,15+sin(clock*1.6)*2,Color(ink,.12),false,1.0,true)
	draw_circle(center,9,Color("473e2d"),true,-1,true)
	if active:
		draw_rect(Rect2(17,17,14,14),ink)
		draw_rect(Rect2(20,20,8,8),Color("eedbc1"),false,.8)
	else:
		draw_circle(center,4.5,ink,true,-1,true)
	var opacity=maxf(hover,.88 if active else 0.0)
	if opacity>.01:
		var text_width=font.get_string_size(caption,HORIZONTAL_ALIGNMENT_LEFT,-1,18).x
		var rect=Rect2(24-text_width*.5-13,-37,text_width+26,35)
		draw_style_box(_caption_style(opacity),rect)
		draw_string(font,Vector2(24-text_width*.5,-12),caption,HORIZONTAL_ALIGNMENT_LEFT,-1,18,Color("473e2d")*Color(1,1,1,opacity))

func _caption_style(opacity: float) -> StyleBoxFlat:
	var s=StyleBoxFlat.new()
	s.bg_color=Color(.95,.92,.84,.88*opacity)
	s.border_color=Color(.55,.43,.28,.45*opacity)
	s.border_width_bottom=1
	return s

func _gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.pressed and event.button_index in [MOUSE_BUTTON_WHEEL_UP,MOUSE_BUTTON_WHEEL_DOWN]:
		pan.emit(150 if event.button_index==MOUSE_BUTTON_WHEEL_UP else -150)
		accept_event()
	elif event is InputEventMouseButton and event.button_index==MOUSE_BUTTON_LEFT:
		if event.pressed:
			held=true
			distance=0
		else:
			if held and distance<6: chosen.emit()
			held=false
		accept_event()
	elif event is InputEventMouseMotion and held:
		distance+=event.relative.length()
		if distance>=6: pan.emit(event.relative.x)
		accept_event()
