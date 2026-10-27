extends Control
## Full original is always visible in the overview. Details use the original pixels.
signal view_changed
const IMAGE_SIZE=Vector2(38414,1800)
const TILE_WIDTH=2048
const TILE_COUNT=19
const OVERVIEW=Rect2(322,838,796,796.0*1800.0/38414.0)
const DETAIL=Rect2(54,104,1332,700)
const BRIDGE=Vector2(19550,700)
const ORIGINAL=preload("res://assets/painting/overview.jpg")
var image_focus=Vector2(19550,900)
var image_zoom=700.0/1800.0
var draw_overview=false
var tile_cache: Dictionary={}
var dragging=false
var overview_drag=false
var min_zoom=DETAIL.size.x/IMAGE_SIZE.x
var max_zoom=1.5
var section="虹桥市集"

func _ready() -> void:
	mouse_filter=Control.MOUSE_FILTER_STOP
	mouse_default_cursor_shape=Control.CURSOR_DRAG
	texture_filter=CanvasItem.TEXTURE_FILTER_LINEAR_WITH_MIPMAPS
	refresh()

func image_origin() -> Vector2:
	return DETAIL.get_center()-image_focus*image_zoom

func visible_source_rect() -> Rect2:
	var visible_rect=Rect2((DETAIL.position-image_origin())/image_zoom,DETAIL.size/image_zoom)
	return visible_rect.intersection(Rect2(Vector2.ZERO,IMAGE_SIZE))

func refresh() -> void:
	image_zoom=clampf(image_zoom,min_zoom,max_zoom)
	var half=DETAIL.size/(2.0*image_zoom)
	image_focus.x=IMAGE_SIZE.x*.5 if half.x>=IMAGE_SIZE.x*.5 else clampf(image_focus.x,half.x,IMAGE_SIZE.x-half.x)
	image_focus.y=IMAGE_SIZE.y*.5 if half.y>=IMAGE_SIZE.y*.5 else clampf(image_focus.y,half.y,IMAGE_SIZE.y-half.y)
	if image_zoom>.07:
		var visible_rect=visible_source_rect()
		var first=clampi(floori(visible_rect.position.x/TILE_WIDTH),0,TILE_COUNT-1)
		var last=clampi(floori(visible_rect.end.x/TILE_WIDTH),0,TILE_COUNT-1)
		for i in range(first,last+1):
			if not tile_cache.has(i): tile_cache[i]=load("res://assets/painting/tile_%02d.png"%i)
		for i in tile_cache.keys():
			if i<first-1 or i>last+1: tile_cache.erase(i)
	queue_redraw()
	view_changed.emit()

func set_zoom_at(value: float,pivot: Vector2) -> void:
	var pixel=(pivot-image_origin())/image_zoom
	image_zoom=clampf(value,min_zoom,max_zoom)
	image_focus=pixel-(pivot-DETAIL.get_center())/image_zoom
	section="自由细读"
	refresh()

func zoom_by(factor: float) -> void:
	set_zoom_at(image_zoom*factor,DETAIL.get_center())

func show_full() -> void:
	image_focus=IMAGE_SIZE*.5
	image_zoom=min_zoom
	section="完整画心 · 无裁切"
	refresh()

func show_place(place: String) -> void:
	image_zoom=700.0/1800.0
	image_focus.y=900
	match place:
		"rural": image_focus.x=IMAGE_SIZE.x*.89; section="卷首郊野"
		"bridge": image_focus.x=BRIDGE.x; section="虹桥市集"
		"city": image_focus.x=IMAGE_SIZE.x*.21; section="城门街巷"
	refresh()

func original_pixels() -> void:
	set_zoom_at(1.0,DETAIL.get_center())

func _draw() -> void:
	draw_rect(Rect2(0,0,1440,900),Color("e9e0ca"))
	draw_rect(DETAIL,Color("c7b794"))
	if image_zoom<=.07:
		draw_crop(ORIGINAL,0,IMAGE_SIZE)
	else:
		for i in tile_cache:
			var native_size=Vector2(mini(TILE_WIDTH,int(IMAGE_SIZE.x)-i*TILE_WIDTH),IMAGE_SIZE.y)
			draw_crop(tile_cache[i],i*TILE_WIDTH,native_size)
	draw_rect(DETAIL,Color("867353"),false,1)

func draw_crop(texture: Texture2D,native_x: int,native_size: Vector2) -> void:
	if texture==null: return
	var dest=Rect2(image_origin()+Vector2(native_x*image_zoom,0),native_size*image_zoom)
	var clipped=dest.intersection(DETAIL)
	if not clipped.has_area(): return
	var src=Rect2((clipped.position-dest.position)/dest.size*texture.get_size(),clipped.size/dest.size*texture.get_size())
	draw_texture_rect_region(texture,clipped,src)

func _gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		if event.button_index in [MOUSE_BUTTON_LEFT,MOUSE_BUTTON_RIGHT]:
			if event.pressed:
				overview_drag=OVERVIEW.grow(7).has_point(event.position)
				dragging=DETAIL.has_point(event.position)
				if overview_drag: move_overview(event.position)
				if event.double_click and dragging: set_zoom_at(1.0 if image_zoom<.8 else 700.0/1800.0,event.position)
			else:
				dragging=false
				overview_drag=false
		if event.pressed and event.button_index in [MOUSE_BUTTON_WHEEL_UP,MOUSE_BUTTON_WHEEL_DOWN]:
			var pivot=event.position if DETAIL.has_point(event.position) else DETAIL.get_center()
			set_zoom_at(image_zoom*(1.24 if event.button_index==MOUSE_BUTTON_WHEEL_UP else 1.0/1.24),pivot)
		accept_event()
	elif event is InputEventMouseMotion:
		if dragging:
			image_focus-=event.relative/image_zoom
			section="自由细读"
			refresh()
		elif overview_drag: move_overview(event.position)
		accept_event()

func move_overview(pos: Vector2) -> void:
	image_focus.x=clampf((pos.x-OVERVIEW.position.x)/OVERVIEW.size.x,0,1)*IMAGE_SIZE.x
	section="自由细读"
	refresh()

func focus_bridge_for_entry(amount: float) -> void:
	image_focus=Vector2(19550,900).lerp(BRIDGE,amount)
	image_zoom=lerpf(700.0/1800.0,.72,amount)
	section="虹桥 · 即将入画"
	refresh()
