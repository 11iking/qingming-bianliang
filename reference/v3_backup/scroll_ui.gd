extends RefCounted
## Light, scene-first interface for the interactive horizontal scroll.

static func build(h,g) -> void:
	var s=g.state
	var panoramic=g.view_mode==3
	var preview=panoramic and g.selected_site!=g.site_for_chapter()
	if panoramic:
		h.scroll_surface=h.ScrollSurface.new()
		h.scroll_surface.position=Vector2(0,86)
		h.scroll_surface.size=Vector2(1440,557)
		h.content.add_child(h.scroll_surface)
		h.scroll_surface.pan.connect(g.pan_pixels)
		h.scroll_surface.coast.connect(g.coast_pixels)
		h.scroll_surface.tap.connect(g.panorama_tap)
	var top=h.panel(h.content,Rect2(28,20,1384,66),Color(.94,.91,.84,.96))
	h.label(top,"汴梁一日",Rect2(19,10,160,41),27,h.INK,true)
	h.line(top,Rect2(193,16,1,34))
	h.label(top,"%02d  %s"%[s.chapter+1,h.DayState.CHAPTER_NAMES[s.chapter]],Rect2(216,7,360,29),21,h.INK,true)
	h.label(top,h.DayState.HOURS[s.chapter]+("  /  独立体验" if s.sandbox else "  /  阿安的一天"),Rect2(218,37,380,22),12,h.MUTED)
	h.label(top,"钱袋 %d 文    /    鲜梨 %d    /    人情 %+d"%[s.coins,s.stock,s.reputation],Rect2(674,19,430,30),17,h.JADE)
	h.button(top,"手卷",Rect2(1129,10,104,43),"journal")
	h.button(top,"暂停",Rect2(1247,10,112,43),"pause")
	var task=h.panel(h.content,Rect2(28,103,864,42),Color(.94,.91,.84,.92))
	h.label(task,g.task_text().replace("\n","   ·   "),Rect2(16,8,832,26),14,h.INK)
	for i in range(3):
		h.mode_buttons.append(h.button(h.content,["1  亲历","2  对坐","3  横卷"][i],Rect2(942+i*159,103,151,42),"view_%d"%(i+1),g.view_mode==i+1))
	if panoramic:
		h.button(h.content,"‹",Rect2(28,382,51,63),"pan_left")
		h.button(h.content,"›",Rect2(1361,382,51,63),"pan_right")
		for i in range(g.SITES.size()):
			var site=g.SITES[i]
			var holder=Control.new()
			holder.size=Vector2(154,72)
			holder.mouse_filter=Control.MOUSE_FILTER_IGNORE
			h.content.add_child(holder)
			var b=h.button(holder,("◎  " if i==g.site_for_chapter() else "◇  ")+site.name,Rect2(0,0,154,38),"site_%d"%i,i==g.selected_site)
			b.add_theme_font_size_override("font_size",15)
			var stem=h.label(holder,"│\n●",Rect2(61,42,32,34),13,h.JADE)
			stem.horizontal_alignment=HORIZONTAL_ALIGNMENT_CENTER
			h.site_markers.append({"node":holder,"index":i,"stem":stem})
		var guide=h.panel(h.content,Rect2(465,605,510,30),Color(.19,.28,.23,.78),Color.TRANSPARENT)
		h.label(guide,"←  拖动画卷 / 滚轮 / 方向键  →    点击地点走近人物",Rect2(12,3,486,24),13,h.PAPER).horizontal_alignment=HORIZONTAL_ALIGNMENT_CENTER
	else:
		h.label(h.content,"＋",Rect2(704,409,32,36),24,Color(.94,.9,.78,.65)).horizontal_alignment=HORIZONTAL_ALIGNMENT_CENTER
		h.focus_label=h.label(h.content,"",Rect2(410,590,620,33),17,h.PAPER)
		h.focus_label.horizontal_alignment=HORIZONTAL_ALIGNMENT_CENTER
		h.focus_label.add_theme_color_override("font_shadow_color",h.INK)
		h.focus_label.add_theme_constant_override("shadow_offset_x",1)
		h.focus_label.add_theme_constant_override("shadow_offset_y",1)
	var card=h.panel(h.content,Rect2(28,650,1384,152),Color(.95,.92,.85,.97))
	h.line(card,Rect2(214,19,1,110))
	var title=g.SITES[g.selected_site].name if preview else g.speaker()
	var narrative=g.SITES[g.selected_site].line if preview else g.narrative()
	h.paragraph(card,title.replace("  /  ","\n"),Rect2(22,18,175,61),19,h.RED)
	h.label(card,"画中一隅" if preview else "人在画中 · 今日此刻",Rect2(23,87,172,25),12,h.MUTED)
	var story=h.paragraph(card,narrative,Rect2(240,14,1106,67),19,h.INK)
	var actions=g.actions()
	if preview:
		actions=[{"id":"pan_home","text":"回到今日进程","primary":true},{"id":"chapter_%d"%g.SITES[g.selected_site].chapter,"text":"在此独立体验"}]
	if actions.size()>0:
		var width=minf(342,1106.0/actions.size()-12)
		for i in range(actions.size()):
			var a=actions[i]
			h.button(card,a.text,Rect2(240+i*(width+12),92,width,43),a.id,a.get("primary",i==0),a.get("disabled",false))
	if not preview and s.chapter==5 and s.paid_wine and not s.toasted:
		h.progress=ProgressBar.new()
		h.progress.position=Vector2(23,131)
		h.progress.size=Vector2(165,8)
		h.progress.scale=Vector2(1,.35)
		h.progress.max_value=1
		h.progress.value=s.pour
		h.progress.show_percentage=false
		h.progress.add_theme_font_size_override("font_size",1)
		h.progress.add_theme_stylebox_override("background",h.style(Color("cec3aa"),Color.TRANSPARENT,0))
		h.progress.add_theme_stylebox_override("fill",h.style(h.JADE,Color.TRANSPARENT,0))
		card.add_child(h.progress)
		h.dynamic_tip=h.label(card,"斟酒  %d%%"%int(s.pour*100),Rect2(23,107,164,22),13,h.JADE)
	if narrative!=h.last_narrative:
		story.visible_characters=0
		h.text_tween=h.create_tween()
		h.text_tween.tween_property(story,"visible_characters",narrative.length(),minf(narrative.length()*.016,1.8))
		card.position.y+=14
		card.modulate.a=0
		h.card_tween=h.create_tween().set_parallel()
		h.card_tween.tween_property(card,"position:y",650,.3).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
		h.card_tween.tween_property(card,"modulate:a",1.0,.25)
	h.last_narrative=narrative
	var foot=h.panel(h.content,Rect2(28,818,1384,73),Color(.94,.91,.84,.97))
	if panoramic:
		h.label(foot,"汴河长卷",Rect2(20,9,214,31),24,h.INK,true)
		h.label(foot,"左右展读 · 点击寻人",Rect2(23,45,216,20),12,h.MUTED)
		h.scroll_map=h.ScrollMap.new()
		h.scroll_map.position=Vector2(252,37)
		h.scroll_map.size=Vector2(824,32)
		foot.add_child(h.scroll_map)
		h.scroll_map.seek.connect(g.seek_scroll)
		for i in range(g.SITES.size()):
			var x=252+(g.SITES[i].point.x+23)/46*824-55
			var b=h.button(foot,g.SITES[i].name,Rect2(x,3,110,32),"site_%d"%i,i==g.selected_site)
			b.add_theme_font_size_override("font_size",13)
		h.button(foot,"R  回到当前任务",Rect2(1142,14,216,43),"pan_home")
	else:
		h.button(foot,"←  返回横向长卷",Rect2(18,14,250,43),"view_3",true)
		h.label(foot,"右键环顾  ·  点击物件 / E 交互  ·  1 / 2 切换视角  ·  3 返回长卷  ·  Esc 暂停",Rect2(294,22,1056,31),15,h.MUTED)
