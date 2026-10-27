extends RefCounted
## The scroll stays clear until the visitor chooses a person or place.
const Hotspot=preload("res://scripts/scroll_hotspot.gd")

static func build(h,g) -> void:
	var s=g.state
	var panoramic=g.view_mode==3
	var preview=panoramic and g.selected_site!=g.site_for_chapter()
	if panoramic:
		h.scroll_surface=h.ScrollSurface.new()
		h.scroll_surface.position=Vector2(54,104)
		h.scroll_surface.size=Vector2(1332,700)
		h.content.add_child(h.scroll_surface)
		h.scroll_surface.pan.connect(g.pan_pixels)
		h.scroll_surface.coast.connect(g.coast_pixels)
		h.scroll_surface.tap.connect(g.panorama_tap)
	h.add_scroll_frame()
	h.label(h.content,"汴梁一日",Rect2(63,22,195,46),28,h.INK,true)
	h.seal(h.content,"入画",Vector2(226,30),28)
	h.label(h.content,h.DayState.HOURS[s.chapter].split(" · ")[0]+"  /  "+h.DayState.CHAPTER_NAMES[s.chapter],Rect2(279,33,660,27),16,h.MUTED,true)
	h.ink_button(h.content,"卷录  ·  Esc",Rect2(1235,27,144,39),"pause",false,14)
	if panoramic:
		for i in range(g.SITES.size()):
			var marker=Hotspot.new()
			marker.caption=g.SITES[i].name
			marker.active=i==g.site_for_chapter()
			marker.chosen.connect(func(): g.browse_site(i))
			marker.pan.connect(g.pan_pixels)
			h.content.add_child(marker)
			h.site_markers.append({"node":marker,"index":i})
		var hint=h.label(h.content,"向左右展读，循一枚印记走入人间",Rect2(410,847,620,27),16,h.MUTED,true)
		hint.horizontal_alignment=HORIZONTAL_ALIGNMENT_CENTER
		h.ink_button(h.content,"回到阿安  ↗",Rect2(1221,840,170,40),"pan_home",false,14)
	else:
		h.ink_button(h.content,"←  退回画卷",Rect2(56,840,170,40),"view_3",false,15)
		h.ink_button(h.content,"人物对话  ↗",Rect2(1221,840,170,40),"open_interaction",false,14)
		var hint=h.label(h.content,"右键环顾  ·  点击物件或 E 交互",Rect2(415,847,610,27),14,h.MUTED)
		hint.horizontal_alignment=HORIZONTAL_ALIGNMENT_CENTER
		h.focus_label=h.label(h.content,"",Rect2(470,558,500,31),16,h.PAPER)
		h.focus_label.horizontal_alignment=HORIZONTAL_ALIGNMENT_CENTER
		h.focus_label.add_theme_color_override("font_shadow_color",h.INK)
		h.focus_label.add_theme_constant_override("shadow_offset_y",1)
		h.label(h.content,"·",Rect2(711,440,18,24),20,h.PAPER)
	var inscription=("此处  ·  "+g.SITES[g.selected_site].name) if preview else ("此刻  ·  "+h.DayState.CHAPTER_NAMES[s.chapter])
	h.task_inscription=h.ink_button(h.content,inscription+"  ↗",Rect2(59,840,312,40),"open_interaction",false,14) if panoramic else null
	var drawer=Control.new()
	drawer.position=Vector2(54,603)
	drawer.size=Vector2(1332,201)
	drawer.mouse_filter=Control.MOUSE_FILTER_IGNORE
	h.content.add_child(drawer)
	h.interaction_drawer=drawer
	var wash=ColorRect.new()
	wash.size=drawer.size
	wash.mouse_filter=Control.MOUSE_FILTER_STOP
	wash.material=ShaderMaterial.new()
	wash.material.shader=preload("res://scripts/dialogue_wash.gdshader")
	drawer.add_child(wash)
	var title=g.SITES[g.selected_site].name if preview else g.speaker().split("  /  ")[0]
	var narrative=g.SITES[g.selected_site].line if preview else g.narrative()
	h.label(drawer,title,Rect2(84,39,500,27),18,h.RED,true)
	if not preview:
		h.label(drawer,"钱袋 %d 文  /  鲜梨 %d"%[s.coins,s.stock],Rect2(690,43,254,23),12,h.MUTED)
		for i in range(3):
			h.ink_button(drawer,["亲历","对坐","观卷"][i],Rect2(964+i*65,36,58,32),"view_%d"%(i+1),g.view_mode==i+1,13)
	h.ink_button(drawer,"收起",Rect2(1166,35,76,34),"close_interaction",false,13)
	var story=h.paragraph(drawer,narrative,Rect2(84,77,1160,62),18,h.INK)
	var actions=g.actions()
	if preview:
		actions=[{"id":"chapter_%d"%g.SITES[g.selected_site].chapter,"text":"走入这一刻  →","primary":true},{"id":"pan_home","text":"回到阿安的一天"}]
	var x=84.0
	for i in range(actions.size()):
		var a=actions[i]
		var width=clampf(h.sans.get_string_size(a.text,HORIZONTAL_ALIGNMENT_LEFT,-1,17).x+42,190,365)
		var b=h.ink_button(drawer,a.text,Rect2(x,148,width,40),a.id,a.get("primary",i==0),17)
		b.disabled=a.get("disabled",false)
		x+=width+28
	if not preview and s.chapter==5 and s.paid_wine and not s.toasted:
		h.progress=ProgressBar.new()
		h.progress.position=Vector2(770,162)
		h.progress.size=Vector2(185,5)
		h.progress.scale.y=.3
		h.progress.max_value=1
		h.progress.value=s.pour
		h.progress.show_percentage=false
		h.progress.add_theme_font_size_override("font_size",1)
		h.progress.add_theme_stylebox_override("background",h.style(Color("cec3aa"),Color.TRANSPARENT,0))
		h.progress.add_theme_stylebox_override("fill",h.style(h.JADE,Color.TRANSPARENT,0))
		drawer.add_child(h.progress)
		h.dynamic_tip=h.label(drawer,"斟酒  %d%%"%int(s.pour*100),Rect2(978,151,170,28),13,h.JADE)
	drawer.visible=g.interaction_open
	if drawer.visible and narrative!=h.last_narrative:
		story.visible_characters=0
		h.text_tween=h.create_tween()
		h.text_tween.tween_property(story,"visible_characters",narrative.length(),minf(narrative.length()*.013,1.4))
		drawer.position.y+=10
		drawer.modulate.a=0
		h.card_tween=h.create_tween().set_parallel()
		h.card_tween.tween_property(drawer,"position:y",603,.35).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
		h.card_tween.tween_property(drawer,"modulate:a",1.0,.3)
	h.last_narrative=narrative
