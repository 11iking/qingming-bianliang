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
	toast_label=label(root,"",Rect2(425,124,650,45),18,PAPER)
	toast_label.horizontal_alignment=HORIZONTAL_ALIGNMENT_CENTER
	toast_label.add_theme_stylebox_override("normal",style(Color("344d43"),Color.TRANSPARENT,8))
	toast_label.visible=false

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
	parent.add_child(b)
	return b

func clear() -> void:
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
	var p=panel(content,Rect2(0,0,480,900),Color("eee6d2"),Color.TRANSPARENT)
	label(p,"一 幅 画  ·  一 个 人  ·  一 日 人 间",Rect2(56,66,360,30),16,MUTED)
	line(p,Rect2(56,118,54,3),RED)
	label(p,"汴梁\n一日",Rect2(52,157,350,231),86,INK,true)
	label(p,"走进《清明上河图》",Rect2(58,414,352,50),25,INK,true)
	paragraph(p,"长卷里，有千百种人生。\n今天，借一位卖梨人的眼睛，\n从晨雾走到灯火。",Rect2(60,487,340,110),20,MUTED)
	button(p,"入画  ·  开始这一天   →",Rect2(58,641,360,58),"new",true)
	button(p,"继续上次",Rect2(58,716,173,45),"continue",false,not has_save)
	button(p,"选一段人生",Rect2(244,716,174,45),"chapters")
	label(p,"桌面交互原型  /  GODOT 4",Rect2(60,831,340,28),13,MUTED)
	var stamp=panel(content,Rect2(1344,50,48,92),RED,Color.TRANSPARENT)
	label(stamp,"入\n画",Rect2(9,3,34,85),27,PAPER,true)
	label(content,"汴河 · 虹桥",Rect2(552,58,460,48),32,Color("374b42"),true)
	label(content,"北宋市井的一个想象片段",Rect2(555,111,500,35),16,Color("566556"))
	var note=panel(content,Rect2(1010,738,355,107),Color(.92,.89,.8,.91))
	label(note,"今日的身份",Rect2(22,12,280,25),14,MUTED)
	label(note,"阿安  /  虹桥边的卖梨人",Rect2(22,45,310,40),22,INK,true)
	label(content,"按住右键环顾     ·     M 声音     ·     F11 全屏",Rect2(552,842,650,25),14,Color("4c6154"))

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
	var s=g.state
	var top=panel(content,Rect2(28,25,1384,86),Color(.93,.9,.82,.96))
	label(top,"汴梁一日",Rect2(22,10,180,48),30,INK,true)
	label(top,"走进清明上河图",Rect2(24,56,195,20),12,MUTED)
	line(top,Rect2(219,18,1,50))
	label(top,"%02d  %s" % [s.chapter+1,DayState.CHAPTER_NAMES[s.chapter]],Rect2(245,10,430,38),23,INK,true)
	label(top,DayState.HOURS[s.chapter]+("    /    独立体验" if s.sandbox else "    /    阿安的一天"),Rect2(248,53,420,25),14,MUTED)
	label(top,"钱袋  %d 文"%s.coins,Rect2(784,18,184,33),20,JADE)
	label(top,"鲜梨  %d"%s.stock,Rect2(784,52,120,24),13,MUTED)
	label(top,"人情  %+d"%s.reputation,Rect2(908,52,130,24),13,MUTED)
	button(top,"手卷 / 章节",Rect2(1052,22,153,43),"journal")
	button(top,"暂停",Rect2(1220,22,139,43),"pause")
	var perspectives=panel(content,Rect2(28,133,186,192),Color(.93,.9,.82,.96))
	label(perspectives,"视 角",Rect2(17,9,155,25),13,MUTED)
	for i in range(3):
		var names=["1  我 · 亲历其境","2  你 · 对坐相逢","3  他 · 画外观人"]
		mode_buttons.append(button(perspectives,names[i],Rect2(10,42+i*47,166,39),"view_%d"%(i+1),g.view_mode==i+1))
	var tasks=panel(content,Rect2(1135,133,277,190),Color(.93,.9,.82,.95))
	label(tasks,"此刻 · 要做的事",Rect2(18,12,241,27),17,JADE,true)
	line(tasks,Rect2(18,50,240,1))
	paragraph(tasks,g.task_text(),Rect2(18,66,240,110),16,INK)
	label(content,"＋",Rect2(704,409,32,36),24,Color(.94,.9,.78,.65)).horizontal_alignment=HORIZONTAL_ALIGNMENT_CENTER
	focus_label=label(content,"",Rect2(410,574,620,35),17,PAPER)
	focus_label.horizontal_alignment=HORIZONTAL_ALIGNMENT_CENTER
	focus_label.add_theme_color_override("font_shadow_color",INK)
	focus_label.add_theme_constant_override("shadow_offset_x",1)
	focus_label.add_theme_constant_override("shadow_offset_y",1)
	var card=panel(content,Rect2(240,640,1172,214),Color(.94,.91,.84,.98))
	var seal=panel(card,Rect2(20,20,42,42),RED,Color.TRANSPARENT)
	label(seal,"记",Rect2(7,3,34,36),25,PAPER,true)
	label(card,g.speaker(),Rect2(80,17,960,32),17,RED)
	paragraph(card,g.narrative(),Rect2(80,57,1056,72),20,INK)
	var actions=g.actions()
	var total=actions.size()
	if total>0:
		var width=minf(328,1050.0/total-12)
		for i in range(total):
			var a:Dictionary=actions[i]
			button(card,a.text,Rect2(80+i*(width+12),147,width,45),a.id,a.get("primary",i==0),a.get("disabled",false))
	if s.chapter==5 and s.paid_wine and not s.toasted:
		progress=ProgressBar.new()
		progress.position=Vector2(29,369)
		progress.size=Vector2(184,12)
		progress.max_value=1
		progress.value=s.pour
		progress.show_percentage=false
		progress.add_theme_stylebox_override("background",style(Color("cec3aa"),Color.TRANSPARENT,0))
		progress.add_theme_stylebox_override("fill",style(JADE,Color.TRANSPARENT,0))
		content.add_child(progress)
		dynamic_tip=label(content,"斟酒  %d%%" % int(s.pour*100),Rect2(31,334,182,30),15,PAPER)
	panel(content,Rect2(240,862,1172,30),Color(.18,.24,.2,.8),Color.TRANSPARENT)
	label(content,"右键拖动：环顾  ·  滚轮：远近  ·  R：视角归位  ·  E：交互  ·  M：声音  ·  Esc：暂停",Rect2(253,865,1140,23),13,Color("ded9c6"))
	var chapter_mark=panel(content,Rect2(28,750,185,104),Color(.23,.32,.27,.9),Color.TRANSPARENT)
	label(chapter_mark,"人在画中",Rect2(15,13,155,29),23,PAPER,true)
	label(chapter_mark,"阿安 · 卖梨人" if s.chapter!=3 else "船夫 · 汴河渡口",Rect2(16,54,170,27),14,Color("c6c9b3"))

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
	toast_label.visible=true
	toast_timer=3.8

func _process(delta: float) -> void:
	if toast_timer>0:
		toast_timer-=delta
		toast_label.visible=toast_timer>0
	if is_instance_valid(progress) and game:
		progress.value=game.state.pour
		if is_instance_valid(dynamic_tip): dynamic_tip.text="斟酒  %d%%" % int(game.state.pour*100)

const DayState=preload("res://scripts/day_state.gd")
