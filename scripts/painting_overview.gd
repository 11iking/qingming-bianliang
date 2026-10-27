extends Control
## A complete, uncropped navigation strip of the actual painting.
var viewer
var held=false

func _ready() -> void:
	mouse_filter=Control.MOUSE_FILTER_STOP
	mouse_default_cursor_shape=Control.CURSOR_POINTING_HAND
	viewer.view_changed.connect(queue_redraw)
	tooltip_text="完整画心 · 点击定位 · 卷首在右"

func _draw() -> void:
	draw_texture_rect(viewer.ORIGINAL,Rect2(Vector2.ZERO,size),false)
	var visible_rect=viewer.visible_source_rect()
	var marker=Rect2(visible_rect.position/viewer.IMAGE_SIZE*size,visible_rect.size/viewer.IMAGE_SIZE*size)
	draw_rect(marker,Color("983f2e"),false,1.5)
	draw_line(Vector2(marker.position.x,size.y+3),Vector2(marker.end.x,size.y+3),Color("983f2e"),1.5)

func _gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.button_index==MOUSE_BUTTON_LEFT:
		held=event.pressed
		if held: viewer.move_overview(viewer.OVERVIEW.position+event.position)
		accept_event()
	elif event is InputEventMouseMotion and held:
		viewer.move_overview(viewer.OVERVIEW.position+event.position)
		accept_event()
