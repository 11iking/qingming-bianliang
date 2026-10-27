extends CanvasLayer
## Fixed design canvas scales with the Godot canvas_items stretch mode.
signal action(id: String)
const INK=Color("343e39")
const MUTED=Color("7a806f")
const RED=Color("9c4838")
const JADE=Color("466d61")
const PAPER=Color("ede5d1")
const LINE=Color("c5bda7")
var root: Control
var content: Control
var game
var serif: FontVariation
var sans: FontVariation
var toast_label: Label
var toast_timer:=0.0
var progress: ProgressBar
var focus_label: Label
var dynamic_tip: Label
var mode_buttons: Array=[]
var screen:="menu"
var painting_view
var transition_cover: ColorRect
const PaintingView=preload("res://scripts/painting_view.gd")
const ScrollSurface=preload("res://scripts/scroll_surface.gd")
const ScrollMap=preload("res://scripts/scroll_map.gd")
var scroll_surface
var scroll_map
var site_markers: Array=[]
var last_narrative=""
var text_tween: Tween
var card_tween: Tween
var cue_tween: Tween
var chapter_banner: Control
var fx_root: Control

func _ready() -> void:
	serif=FontVariation.new()
	serif.base_font=preload("res://assets/fonts/NotoSerifSC.ttf")
	serif.variation_opentype={"wght":500.0}
	sans=FontVariation.new()
	sans.base_font=preload("res://assets/fonts/NotoSansSC.ttf")
	sans.variation_opentype={"wght":430.0}
	root=Control.new()
	root.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	root.mouse_filter=Control.MOUSE_FILTER_IGNORE
	add_child(root)
	var theme=Theme.new()
	theme.default_font=sans
	theme.default_font_size=18
	root.theme=theme
	var overlay=ColorRect.new()
	overlay.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	overlay.mouse_filter=Control.MOUSE_FILTER_IGNORE
	var mat=ShaderMaterial.new()
	mat.shader=preload("res://scripts/paper.gdshader")
	overlay.material=mat
	root.add_child(overlay)
	content=Control.new()
	content.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	content.mouse_filter=Control.MOUSE_FILTER_IGNORE
	root.add_child(content)
	fx_root=Control.new()
	fx_root.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	fx_root.mouse_filter=Control.MOUSE_FILTER_IGNORE
	root.add_child(fx_root)
	toast_label=label(root,"",Rect2(425,124,650,45),18,PAPER)
	toast_label.horizontal_alignment=HORIZONTAL_ALIGNMENT_CENTER
	toast_label.add_theme_stylebox_override("normal",style(Color("344d43"),Color.TRANSPARENT,8))
	toast_label.visible=false
	transition_cover=ColorRect.new()
	transition_cover.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	transition_cover.color=PAPER
	transition_cover.modulate.a=0
	transition_cover.visible=false
	root.add_child(transition_cover)
	var entry_text=label(transition_cover,"从画中，走入人间。",Rect2(0,390,1440,90),40,INK,true)
	entry_text.horizontal_alignment=HORIZONTAL_ALIGNMENT_CENTER

func style(bg: Color,border: Color=Color.TRANSPARENT,pad: int=12) -> StyleBoxFlat:
	var s=StyleBoxFlat.new()
	s.bg_color=bg
	s.border_color=border
	s.set_border_width_all(1 if border.a>0 else 0)
	s.set_content_margin_all(pad)
	s.corner_radius_top_left=3
	s.corner_radius_top_right=3
	s.corner_radius_bottom_left=3
	s.corner_radius_bottom_right=3
	return s

func panel(parent: Control,rect: Rect2,color: Color=PAPER,border: Color=LINE) -> Panel:
	var p=Panel.new()
	p.position=rect.position
	p.size=rect.size
	p.add_theme_stylebox_override("panel",style(color,border))
	parent.add_child(p)
	return p

func label(parent: Control,text: String,rect: Rect2,size: int=18,color: Color=INK,poetic: bool=false) -> Label:
	var l=Label.new()
	l.text=text
	l.position=rect.position
	l.size=rect.size
	l.add_theme_font_override("font",serif if poetic else sans)
	l.add_theme_font_size_override("font_size",size)
	l.add_theme_color_override("font_color",color)
	l.mouse_filter=Control.MOUSE_FILTER_IGNORE
	parent.add_child(l)
	return l

func paragraph(parent: Control,text: String,rect: Rect2,size: int=18,color: Color=INK) -> Label:
	var l=label(parent,text,rect,size,color)
	l.autowrap_mode=TextServer.AUTOWRAP_WORD_SMART
	return l

func line(parent: Control,rect: Rect2,color: Color=LINE) -> void:
	var c=ColorRect.new()
	c.color=color
	c.position=rect.position
	c.size=rect.size
	c.mouse_filter=Control.MOUSE_FILTER_IGNORE
	parent.add_child(c)

func button(parent: Control,text: String,rect: Rect2,id: String,primary: bool=false,disabled: bool=false) -> Button:
	var b=Button.new()
	b.text=text
	b.position=rect.position
	b.size=rect.size
	b.mouse_default_cursor_shape=Control.CURSOR_POINTING_HAND
	b.add_theme_stylebox_override("normal",style(JADE if primary else Color("e7deca"),JADE if primary else LINE,14))
	b.add_theme_stylebox_override("hover",style(Color("5b7c6e") if primary else Color("f8f0de"),JADE,14))
	b.add_theme_stylebox_override("pressed",style(Color("314e42"),JADE,14))
	b.add_theme_stylebox_override("focus",style(Color(0,0,0,0),RED,14))
	b.add_theme_stylebox_override("disabled",style(Color("d5cfbe"),LINE,14))
	b.add_theme_color_override("font_color",PAPER if primary else INK)
	b.add_theme_color_override("font_hover_color",PAPER if primary else INK)
	b.add_theme_color_override("font_pressed_color",PAPER)
	b.add_theme_color_override("font_disabled_color",MUTED)
	b.add_theme_font_size_override("font_size",17)
	b.disabled=disabled
	b.pressed.connect(func(): action.emit(id))
	b.pivot_offset=rect.size*.5
	b.mouse_entered.connect(func(): b.create_tween().tween_property(b,"scale",Vector2.ONE*1.025,.12))
	b.mouse_exited.connect(func(): b.create_tween().tween_property(b,"scale",Vector2.ONE,.16))
	parent.add_child(b)
	return b

func clear() -> void:
	if text_tween and text_tween.is_valid(): text_tween.kill()
	if card_tween and card_tween.is_valid(): card_tween.kill()
	site_markers.clear()
	scroll_surface=null
	scroll_map=null
	painting_view=null
	for c in content.get_children():
		content.remove_child(c)
		c.queue_free()
	progress=null
	focus_label=null
	dynamic_tip=null
	mode_buttons.clear()

func clear_toast() -> void:
	toast_label.visible=false
	toast_timer=0

func show_menu(has_save: bool) -> void:
	clear_toast()
	clear()
	screen="menu"
	panel(content,Rect2(0,0,1440,900),PAPER,Color.TRANSPARENT)
	label(content,"汴 梁 一 日  /  走 进 一 幅 画",Rect2(50,28,680,35),18,JADE)
	label(content,"01  观画     —     02  入画     —     03  过一日",Rect2(920,33,490,30),14,MUTED)
	line(content,Rect2(49,81,1342,1))
	label(content,"清明上河图",Rect2(45,98,700,69),50,INK,true)
	label(content,"北宋 · 张择端     /     故宫博物院藏     /     绢本淡设色",Rect2(51,177,960,31),17,MUTED)
	var stamp=panel(content,Rect2(1333,107,58,88),RED,Color.TRANSPARENT)
	label(stamp,"观\n画",Rect2(13,3,35,81),27,PAPER,true)
	painting_view=PaintingView.new()
	painting_view.position=Vector2(48,220)
	painting_view.size=Vector2(1344,510)
	content.add_child(painting_view)
	button(content,"全卷",Rect2(48,745,104,38),"paint_full")
	button(content,"卷首郊野",Rect2(162,745,138,38),"paint_rural")
	button(content,"虹桥市集",Rect2(310,745,138,38),"paint_bridge")
	button(content,"城门街巷",Rect2(458,745,138,38),"paint_city")
	button(content,"−",Rect2(986,745,52,38),"paint_out")
	button(content,"＋",Rect2(1048,745,52,38),"paint_in")
	button(content,"1:1 原始像素",Rect2(1110,745,184,38),"paint_native")
	button(content,"图源",Rect2(1304,745,88,38),"paint_source")
	label(content,"画卷中，借一位卖梨人的眼睛。",Rect2(49,809,602,38),24,INK,true)
	button(content,"继续上次",Rect2(664,807,142,49),"continue",false,not has_save)
	button(content,"独立章节",Rect2(818,807,142,49),"chapters")
	button(content,"点击入画 · 阿安的一天   →",Rect2(976,807,416,49),"new",true)
	label(content,"真实原作数字图像  ·  38,414 × 1,800 像素  ·  全卷与高清细节离线可看",Rect2(49,872,1070,21),12,MUTED)
	label(content,"M 声音  /  F11 全屏",Rect2(1205,872,215,21),12,MUTED)

func painting_action(id: String) -> void:
	if not is_instance_valid(painting_view): return
	match id:
		"paint_full": painting_view.show_full()
		"paint_rural": painting_view.show_place("rural")
		"paint_bridge": painting_view.show_place("bridge")
		"paint_city": painting_view.show_place("city")
		"paint_in": painting_view.zoom_by(1.5)
		"paint_out": painting_view.zoom_by(1.0/1.5)
		"paint_native": painting_view.original_pixels()
		"paint_source": show_painting_source()

func show_painting_source() -> void:
	var scrim=panel(content,Rect2(0,0,1440,900),Color(.15,.17,.14,.65),Color.TRANSPARENT)
	var p=panel(scrim,Rect2(320,235,800,416),PAPER)
	label(p,"原作与图源",Rect2(34,25,720,53),32,INK,true)
	paragraph(p,"张择端《清明上河图》 · 故宫博物院藏。\n本程序采用 Wikimedia Commons 的公共领域数字复制图，\n尺寸 38,414 × 1,800 像素，完整画心从卷首至卷末保留。\n高清细节直接使用原图像素；未进行 AI 补画、扩图或重绘。",Rect2(38,102,724,151),18,INK)
	label(p,"commons.wikimedia.org/wiki/File:Alongtheriver_QingMing.jpg",Rect2(38,273,736,28),14,MUTED)
	var b=button(p,"合上，继续观画",Rect2(498,336,264,47),"paint_source_close",true)
	b.pressed.connect(func(): scrim.queue_free())

func enter_transition_in() -> void:
	if is_instance_valid(painting_view):
		painting_view.mouse_filter=Control.MOUSE_FILTER_IGNORE
		var move=create_tween()
		move.tween_method(painting_view.focus_bridge_for_entry,0.0,1.0,.65).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_IN_OUT)
		await move.finished
	transition_cover.visible=true
	transition_cover.modulate.a=0
	var fade=create_tween()
	fade.tween_property(transition_cover,"modulate:a",1.0,.35)
	await fade.finished

func enter_transition_out() -> void:
	var fade=create_tween()
	fade.tween_property(transition_cover,"modulate:a",0.0,.6)
	await fade.finished
	transition_cover.visible=false

func show_chapters() -> void:
	clear()
	screen="chapters"
	panel(content,Rect2(130,85,1180,730),PAPER)
	label(content,"选一段人生",Rect2(180,121,800,60),42,INK,true)
	label(content,"独立体验不会覆盖主线存档。完整的一天，从晨起出摊开始。",Rect2(182,193,1000,35),18,MUTED)
	var desc=["拿起竹篮，摆好三筐鲜梨", "招呼客人，在价钱与人情间取舍", "站在虹桥，看大船穿桥而过", "左右交替划桨，顺流抵达码头", "收好竹篮，清点今天的账", "亲手斟酒，和老友碰一盏", "坐进勾栏，听一折市井故事"]
	for i in range(7):
		var x=180+(i%3)*362
		var y=259+(i/3)*149
		var card=panel(content,Rect2(x,y,338,130),Color("e4dcc7"))
		label(card,"0%d  /  %s" % [i+1,DayState.HOURS[i].split(" · ")[0]],Rect2(18,10,300,25),13,RED)
		button(card,DayState.CHAPTER_NAMES[i]+"   →",Rect2(12,39,314,40),"chapter_%d"%i)
		label(card,desc[i],Rect2(18,91,310,24),14,MUTED)
	button(content,"返回",Rect2(1125,735,135,44),"menu")

func show_game(g) -> void:
	game=g
	clear()
	screen="game"
	preload("res://scripts/scroll_ui.gd").build(self,g)

func show_pause() -> void:
	clear()
	screen="pause"
	panel(content,Rect2(0,0,1440,900),Color(.12,.18,.16,.55),Color.TRANSPARENT)
	var p=panel(content,Rect2(443,130,554,632),PAPER)
	label(p,"画卷暂歇",Rect2(47,38,470,64),42,INK,true)
	paragraph(p,"进度已自动保存。\n右键环顾，数字键 1 / 2 / 3 切换身份视角。\n点击场景里的物件，或按 E 完成当前交互。",Rect2(49,127,460,115),18,MUTED)
	button(p,"回到画中",Rect2(48,276,459,52),"resume",true)
	button(p,"手卷 · 章节与见闻",Rect2(48,345,459,48),"journal")
	button(p,"声音："+("关闭" if game.sound.muted else "开启"),Rect2(48,408,459,48),"sound")
	button(p,"回到卷首",Rect2(48,471,459,48),"menu")
	button(p,"保存并退出",Rect2(48,534,459,48),"quit")

func show_journal(g) -> void:
	game=g
	clear()
	screen="journal"
	panel(content,Rect2(123,58,1194,780),PAPER)
	label(content,"一日手卷",Rect2(169,90,750,61),43,INK,true)
	label(content,"行过的路，遇见的人。",Rect2(172,162,720,30),18,MUTED)
	line(content,Rect2(173,218,1090,1))
	for i in range(8):
		label(content,("●  " if i==g.state.chapter else "○  ")+DayState.HOURS[i],Rect2(176,251+i*57,225,30),17,RED if i==g.state.chapter else MUTED)
		label(content,DayState.CHAPTER_NAMES[i],Rect2(420,246+i*57,230,39),22,INK,true)
	line(content,Rect2(682,250,1,442))
	label(content,"钱袋与人情",Rect2(722,244,500,38),25,JADE,true)
	paragraph(content,"起始盘缠  18 文\n售梨收入  %d 文\n今日花费  %d 文\n钱袋结余  %d 文\n人情往来  %+d" % [g.state.revenue,g.state.expenses,g.state.coins,g.state.reputation],Rect2(726,301,460,168),20)
	label(content,"途中见闻",Rect2(722,492,470,35),23,JADE,true)
	paragraph(content,"\n".join(g.state.memories) if not g.state.memories.is_empty() else "还没有记下见闻。去桥上、河面与灯火里走走吧。",Rect2(726,546,490,160),18,MUTED)
	button(content,"独立章节",Rect2(870,753,185,48),"chapters")
	button(content,"合上手卷",Rect2(1075,753,188,48),"resume",true)

func show_summary(g) -> void:
	game=g
	clear()
	screen="summary"
	var s=g.state
	panel(content,Rect2(203,69,1034,760),PAPER)
	label(content,"卯时入市，亥时归家。",Rect2(257,101,930,33),17,MUTED)
	label(content,"你的一日，也在画里。",Rect2(253,156,940,72),47,INK,true)
	paragraph(content,"鲜梨的清甜、桥头的一声吆喝、老友递来的酒盏。\n长卷没有画完的日常，今天由你走过。",Rect2(259,253,918,81),22,MUTED)
	line(content,Rect2(258,363,920,1))
	var titles=["今日收入","钱袋结余","人情往来"]
	var values=["%d 文"%s.revenue,"%d 文"%s.coins,"%+d"%s.reputation]
	for i in range(3):
		label(content,titles[i],Rect2(268+i*310,393,280,35),17,MUTED)
		label(content,values[i],Rect2(264+i*310,439,290,65),42,JADE,true)
	var ending="会做生意，也会过日子。" if s.reputation<4 else "铜钱有数，人情无价。"
	label(content,ending,Rect2(264,544,900,46),28,RED,true)
	paragraph(content," · ".join(s.memories),Rect2(268,600,900,68),17,MUTED)
	button(content,"再走一日",Rect2(264,715,290,53),"new",true)
	button(content,"重游某一段",Rect2(576,715,290,53),"chapters")
	button(content,"回到卷首",Rect2(888,715,288,53),"menu")

func toast(text: String) -> void:
	toast_label.text=text
	toast_label.position.y=157 if screen=="game" else 124
	toast_label.visible=true
	toast_timer=3.8
	toast_label.modulate.a=0
	create_tween().tween_property(toast_label,"modulate:a",1.0,.2)

func floating_feedback(text: String) -> void:
	var fx=label(fx_root,text,Rect2(525,340,390,62),30,PAPER,true)
	fx.horizontal_alignment=HORIZONTAL_ALIGNMENT_CENTER
	fx.add_theme_stylebox_override("normal",style(Color(.25,.37,.29,.92),Color.TRANSPARENT,10))
	fx.modulate.a=0
	var tween=fx.create_tween()
	tween.tween_property(fx,"modulate:a",1.0,.15)
	tween.parallel().tween_property(fx,"position:y",300,.85).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	tween.tween_interval(.25)
	tween.tween_property(fx,"modulate:a",0.0,.35)
	tween.tween_callback(fx.queue_free)

func chapter_cue(title: String,hour: String) -> void:
	if cue_tween and cue_tween.is_valid(): cue_tween.kill()
	if is_instance_valid(chapter_banner): chapter_banner.queue_free()
	chapter_banner=Control.new()
	chapter_banner.position=Vector2(930,214)
	chapter_banner.mouse_filter=Control.MOUSE_FILTER_IGNORE
	fx_root.add_child(chapter_banner)
	var name_label=label(chapter_banner,title,Rect2(0,0,450,58),40,PAPER,true)
	name_label.horizontal_alignment=HORIZONTAL_ALIGNMENT_RIGHT
	name_label.add_theme_color_override("font_shadow_color",INK)
	name_label.add_theme_constant_override("shadow_offset_x",2)
	name_label.add_theme_constant_override("shadow_offset_y",2)
	var hours=label(chapter_banner,hour,Rect2(0,63,450,30),17,PAPER)
	hours.horizontal_alignment=HORIZONTAL_ALIGNMENT_RIGHT
	chapter_banner.modulate.a=0
	cue_tween=create_tween()
	cue_tween.tween_interval(.55)
	cue_tween.tween_property(chapter_banner,"modulate:a",1.0,.4)
	cue_tween.parallel().tween_property(chapter_banner,"position:x",900,.6).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
	cue_tween.tween_interval(1.0)
	cue_tween.tween_property(chapter_banner,"modulate:a",0.0,.5)

func _process(delta: float) -> void:
	if is_instance_valid(chapter_banner): chapter_banner.visible=screen=="game"
	if screen=="game" and game:
		if is_instance_valid(scroll_map): scroll_map.set_center(game.scroll_x)
		for marker in site_markers:
			var point=game.SITES[marker.index].point
			if marker.index==0 and game.state.chapter==3: point=game.world.row_boat.position+Vector3(0,2.6,0)
			var pos=game.camera.unproject_position(point)
			marker.node.visible=not game.camera.is_position_behind(point) and pos.x>102 and pos.x<1338 and pos.y>220 and pos.y<596
			marker.node.position=pos-Vector2(77,80)+Vector2(0,sin(game.t*2+marker.index)*3)
			marker.stem.modulate.a=.6+sin(game.t*2+marker.index)*.3
	if toast_timer>0:
		toast_timer-=delta
		toast_label.visible=toast_timer>0
	if is_instance_valid(progress) and game:
		progress.value=game.state.pour
		if is_instance_valid(dynamic_tip): dynamic_tip.text="斟酒  %d%%" % int(game.state.pour*100)

const DayState=preload("res://scripts/day_state.gd")
