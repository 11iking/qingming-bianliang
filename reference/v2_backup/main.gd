extends Node3D
const World = preload("res://scripts/world.gd")
const DayState = preload("res://scripts/day_state.gd")
const HUD = preload("res://scripts/hud.gd")
const Sound = preload("res://scripts/sound.gd")
var world
var state = DayState.new()
var hud
var sound
var camera: Camera3D
var hand: Node3D
var held_visual: Node3D
var stream_visual: MeshInstance3D
var held := ""
var view_mode := 3
var playing := false
var paused := false
var current_focus := ""
var drag := false
var look_offset := Vector2.ZERO
var base_position := Vector3.ZERO
var base_target := Vector3.ZERO
var camera_goal := Transform3D.IDENTITY
var zoom := 55.0
var t := 0.0
var row_cooldown := 0.0
var narrative_override := ""
var show_help := true
var test_mode := false
var screenshot_mode := false
var watching_bridge := false
var toast_animation := 0.0
var entering_painting := false

func _ready() -> void:
	world=World.new()
	add_child(world)
	camera=Camera3D.new()
	camera.current=true
	camera.far=180
	camera.near=.06
	camera.fov=55
	add_child(camera)
	hand=Node3D.new()
	camera.add_child(hand)
	hand.position=Vector3(.38,-.37,-.92)
	hand.scale=Vector3.ONE*.65
	sound=Sound.new()
	add_child(sound)
	hud=HUD.new()
	add_child(hud)
	hud.action.connect(on_action)
	get_tree().auto_accept_quit=false
	var args=OS.get_cmdline_user_args()
	test_mode="--self-test" in args
	screenshot_mode="--capture" in args
	set_menu()
	if test_mode:
		call_deferred("run_self_test")
	elif screenshot_mode:
		call_deferred("capture_scenes")

func set_menu() -> void:
	world.visible=false
	playing=false
	paused=false
	held=""
	update_held()
	world.set_time(0)
	world.player.position=Vector3(-3,.2,9.25)
	world.player.visible=true
	world.friend.visible=true
	world.customer.visible=true
	look_offset=Vector2.ZERO
	camera.h_offset=-6.5
	base_position=Vector3(30,23,35)
	base_target=Vector3(0,0,1)
	update_camera_goal()
	camera.transform=camera_goal
	hud.show_menu(FileAccess.file_exists(DayState.SAVE_PATH))

func new_day() -> void:
	state=DayState.new()
	start_chapter(0)

func enter_painting(fresh: bool = true) -> void:
	if entering_painting: return
	entering_painting=true
	await hud.enter_transition_in()
	if fresh:
		state=DayState.new()
		state.sandbox=test_mode or screenshot_mode
	start_chapter(0)
	await hud.enter_transition_out()
	entering_painting=false

func start_chapter(index: int) -> void:
	world.visible=true
	hud.clear_toast()
	state.chapter=clampi(index,0,7)
	playing=true
	paused=false
	held=""
	narrative_override=""
	watching_bridge=false
	toast_animation=0
	world.get_node("FriendCup").position=Vector3(-14,1.13,10.7)
	look_offset=Vector2.ZERO
	camera.h_offset=0
	zoom=55
	world.set_time(state.chapter)
	if state.chapter==1 and state.customer_ready: held="pear"
	world.set_stock(state.stock if state.chapter>0 else state.placed*4)
	world.customer.visible=state.chapter==1 and state.customer_index<3
	for area in world.targets.values(): area.collision_layer=0
	var active_targets=[]
	match state.chapter:
		0,4: active_targets=["basket","counter"]
		1: active_targets=["counter","customer"]
		5: active_targets=["jug","cup"]
	for id in active_targets: world.targets[id].collision_layer=1
	world.friend.visible=true
	world.crossing=state.bridge_helped
	world.cargo_boat.position.x=14 if state.bridge_watched else -5
	world.boat_progress=state.strokes/10.0
	world.row_boat.position.x=-17+world.boat_progress*19
	var spots=[Vector3(-3,.22,9.25),Vector3(-3,.22,9.25),Vector3(6.7,world.bridge_height(0)+.12,0),Vector3(-17,.5,2.5),Vector3(-3,.22,9.25),Vector3(-14,0,12.8),Vector3(22,.1,15.1),Vector3(-3,.22,9.25)]
	world.player.position=spots[state.chapter]
	world.player.rotation.y=PI if state.chapter>=5 else 0.0
	view_mode=1 if state.chapter in [2,3] else 3
	if state.chapter==7:
		state.finished=true
		world.player.visible=true
		base_position=Vector3(30,23,35)
		base_target=Vector3(0,0,1)
		update_camera_goal()
		hud.show_summary(self)
	else:
		set_view(view_mode,false)
		hud.show_game(self)
	update_held()
	state.save_day()
	sound.tone("chapter")

func set_view(mode: int,redraw: bool=true) -> void:
	view_mode=mode
	look_offset=Vector2.ZERO
	zoom=55
	var ch=state.chapter
	hand.visible=mode!=3
	var positions: Array
	var targets: Array
	match ch:
		0,1,4:
			positions=[Vector3(-3,1.79,9.3),Vector3(-3,1.73,12.7),Vector3(4.6,6.8,17.9)]
			targets=[Vector3(-3,1.03,12),Vector3(-3,1.3,9.3),Vector3(-3,1,11.3)]
		2:
			positions=[Vector3(6.25,4.35,.2),Vector3(9.45,4.35,.5),Vector3(16,10,12)]
			targets=[Vector3(-1,.7,0),Vector3(6.5,3.8,0),Vector3(7,1.5,0)]
		3:
			var x=world.row_boat.position.x
			positions=[Vector3(x-.8,1.65,2.5),Vector3(x+1,1.65,2.5),Vector3(x+5,6.2,10)]
			targets=[Vector3(x+7,1,1.8),Vector3(x-1,1.3,2.5),Vector3(x,0.5,2.5)]
		5:
			positions=[Vector3(-14,1.65,12.8),Vector3(-14,1.65,9.65),Vector3(-8.6,5.4,17.4)]
			targets=[Vector3(-14,1.19,10.5),Vector3(-14,1.3,12.8),Vector3(-14,1.05,11.3)]
		6:
			positions=[Vector3(22,1.85,13.9),Vector3(24,1.85,14.1),Vector3(16.8,6.5,20)]
			targets=[Vector3(23,2.1,11),Vector3(22,1.5,15.1),Vector3(23,2,11.8)]
		_:
			positions=[Vector3(30,23,35),Vector3(30,23,35),Vector3(30,23,35)]
			targets=[Vector3.ZERO,Vector3.ZERO,Vector3.ZERO]
	base_position=positions[mode-1]
	base_target=targets[mode-1]
	world.player.visible=mode!=1
	world.customer.visible=ch==1 and state.customer_index<3 and mode!=2
	world.friend.visible=not (ch==5 and mode==2)
	update_camera_goal()
	if redraw: hud.show_game(self)

func update_camera_goal() -> void:
	var transform_goal=Transform3D(Basis.IDENTITY,base_position).looking_at(base_target)
	transform_goal.basis=transform_goal.basis*Basis(Vector3.UP,look_offset.x)*Basis(Vector3.RIGHT,look_offset.y)
	camera_goal=transform_goal

func _process(delta: float) -> void:
	t+=delta
	row_cooldown=maxf(0,row_cooldown-delta)
	if playing and not paused and state.chapter==3:
		var x=world.row_boat.position.x
		var shift=x-(-17+state.strokes/10.0*19)
		# Follow the boat's real, smoothly interpolated position in all three views.
		var v=look_offset
		set_boat_camera(x)
		look_offset=v
		update_camera_goal()
		world.player.position=Vector3(x-.8,.48,2.5)
		world.player.rotation.y=PI/2
	camera.transform=camera.transform.interpolate_with(camera_goal,1-exp(-delta*5))
	camera.fov=lerpf(camera.fov,zoom,1-exp(-delta*7))
	if is_instance_valid(held_visual):
		hand.position.y=-.37+sin(t*2)*.008
	if playing and not paused and state.chapter==2 and watching_bridge and world.cargo_boat.position.x>=11:
		state.bridge_watched=true
		watching_bridge=false
		state.save_day()
		hud.show_game(self)
		hud.toast("船已过桥。船夫拱手谢过，你也记住了这一刻。")
	if playing and not paused and state.chapter==5 and toast_animation>0:
		var before=toast_animation
		toast_animation=maxf(0,toast_animation-delta)
		var amount=sin((1.0-toast_animation/1.6)*PI)
		hand.position=Vector3(.38,-.37,-.92).lerp(Vector3(.06,-.12,-.96),amount)
		world.get_node("FriendCup").position=Vector3(-14,1.13,10.7).lerp(Vector3(-13.94,1.36,11.76),amount)
		world.friend.get_node("RightArm").rotation.x=-amount*1.2
		if before>.8 and toast_animation<=.8: sound.tone("toast")
	if playing and not paused and state.chapter==5 and held=="jug" and state.pour<1:
		var pouring=Input.is_physical_key_pressed(KEY_E) or Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT)
		if pouring:
			state.pour=minf(state.pour+delta*.32,1)
			held_visual.rotation.z=lerpf(held_visual.rotation.z,.95,delta*8)
			stream_visual.visible=true
			if state.pour>=1:
				held=""
				update_held()
				state.add_memory("亲手斟了一盏酒")
				state.save_day()
				hud.toast("酒已斟满。端起酒盏，敬朋友，也敬今天。")
				hud.show_game(self)
		else:
			held_visual.rotation.z=lerpf(held_visual.rotation.z,0.0,delta*8)
			stream_visual.visible=false
	if playing and not paused and hud.screen=="game":
		update_focus()
	world.set_process(playing and not paused)

func set_boat_camera(x: float) -> void:
	match view_mode:
		1: base_position=Vector3(x-.8,1.65,2.5); base_target=Vector3(x+7,1,1.8)
		2: base_position=Vector3(x+1,1.65,2.5); base_target=Vector3(x-.8,1.35,2.5)
		3: base_position=Vector3(x+5,6.2,10); base_target=Vector3(x,.5,2.5)

func _input(event: InputEvent) -> void:
	if entering_painting: return
	if event is InputEventMouseButton and event.button_index==MOUSE_BUTTON_RIGHT:
		drag=event.pressed
	if event is InputEventMouseMotion and drag and playing and not paused:
		look_offset.x=clampf(look_offset.x-event.relative.x*.0035,-1.5,1.5)
		look_offset.y=clampf(look_offset.y-event.relative.y*.0035,-.75,.65)
		update_camera_goal()
	if event is InputEventKey and event.pressed and not event.echo:
		match event.keycode:
			KEY_F11:
				DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED if DisplayServer.window_get_mode()==DisplayServer.WINDOW_MODE_FULLSCREEN else DisplayServer.WINDOW_MODE_FULLSCREEN)
			KEY_M:
				sound.toggle()
				hud.toast("声音已关闭" if sound.muted else "声音已开启")
			KEY_ESCAPE:
				if playing:
					if paused: resume_game()
					elif hud.screen=="summary": set_menu()
					else: pause_game()
			KEY_1,KEY_2,KEY_3:
				if playing and not paused and state.chapter<7:
					set_view(int(event.keycode-KEY_1)+1)
			KEY_R:
				if playing and not paused: set_view(view_mode)
			KEY_E:
				if playing and not paused and hud.screen=="game" and held!="jug": primary_interaction()
			KEY_A,KEY_LEFT:
				if playing and not paused and state.chapter==3: row("left")
			KEY_D,KEY_RIGHT:
				if playing and not paused and state.chapter==3: row("right")
			KEY_TAB:
				if playing and state.chapter<7:
					if paused: resume_game()
					else: paused=true; hud.show_journal(self)

func _unhandled_input(event: InputEvent) -> void:
	if entering_painting: return
	if event is InputEventMouseButton and playing and not paused:
		if event.button_index==MOUSE_BUTTON_WHEEL_UP: zoom=clampf(zoom-3,35,75)
		if event.button_index==MOUSE_BUTTON_WHEEL_DOWN: zoom=clampf(zoom+3,35,75)
		if event.button_index==MOUSE_BUTTON_LEFT and event.pressed and playing and hud.screen=="game":
			var target_id=ray_target(event.position)
			if not target_id.is_empty(): interact_target(target_id)

func ray_target(pos: Vector2) -> String:
	var origin=camera.project_ray_origin(pos)
	var end=origin+camera.project_ray_normal(pos)*80
	var query=PhysicsRayQueryParameters3D.create(origin,end)
	var result=get_world_3d().direct_space_state.intersect_ray(query)
	if result.is_empty(): return ""
	return result.collider.get_meta("target","")

func update_focus() -> void:
	current_focus=ray_target(get_viewport().get_mouse_position())
	if is_instance_valid(hud.focus_label):
		hud.focus_label.text=""
		var hints={}
		match state.chapter:
			0: hints={"basket":"拿起竹篮", "counter":"放下鲜梨" if held=="basket" else "先从左侧竹篮拿货"}
			1: hints={"counter":"拿起一只梨", "customer":"招呼客人"}
			4: hints={"basket":"收好竹篮", "counter":"清点铜钱"}
			5: hints={"jug":"拿起酒壶", "cup":"举杯相碰" if state.pour>=1 else "先拿起酒壶斟酒"}
		if hints.has(current_focus): hud.focus_label.text="点击  /  "+hints[current_focus]

func interact_target(id: String) -> void:
	match state.chapter:
		0:
			if id=="basket" and held=="": do_action("pick_basket")
			elif id=="counter" and held=="basket": do_action("place")
		1:
			if id=="counter": do_action("show_pear")
			elif id=="customer" and not state.customer_ready: do_action("show_pear")
		4:
			if id in ["basket","counter"]: do_action("pack" if not state.packed else "count")
		5:
			if id=="jug" and state.paid_wine: do_action("pick_jug")
			elif id=="cup" and state.pour>=1: do_action("toast")

func primary_interaction() -> void:
	var a=actions()
	if a.is_empty(): return
	# Selecting a price always requires an explicit choice; E only handles physical actions.
	if state.chapter==1 and state.customer_ready: return
	if current_focus!="":
		var old_held=held
		interact_target(current_focus)
		if old_held!=held: return
	do_action(a[0].id)

func update_held() -> void:
	world.get_node("WineJug").visible=held!="jug"
	world.get_node("PlayerCup").visible=held!="cup"
	for child in hand.get_children():
		hand.remove_child(child)
		child.queue_free()
	held_visual=null
	if held=="": return
	held_visual=Node3D.new()
	hand.add_child(held_visual)
	world.sphere(held_visual,Vector3(.08,-.18,.15),Vector3(.28,.2,.24),Color("c6a579"))
	world.cylinder(held_visual,Vector3(.12,-.36,.2),.17,.14,.38,World.JADE)
	match held:
		"basket":
			world.basket(held_visual,Vector3(0,.06,0),.4)
			for i in range(3): world.pear(held_visual,Vector3((i-1)*.19,.24,0))
		"pear": world.pear(held_visual,Vector3(0,.08,0)).scale=Vector3.ONE*1.6
		"jug":
			world.jug(held_visual,Vector3(0,0,0))
			stream_visual=world.beam(held_visual,Vector3(.34,.43,0),Vector3(.54,-.5,0),.025,Color("d3c99e"))
			stream_visual.visible=false
		"cup": world.cup(held_visual,Vector3(0,.02,0))

func on_action(id: String) -> void:
	if entering_painting: return
	sound.tone()
	if id.begins_with("paint_"):
		hud.painting_action(id)
		return
	if id.begins_with("chapter_"):
		state=DayState.new()
		state.prepare_chapter(int(id.trim_prefix("chapter_")))
		start_chapter(state.chapter)
		return
	if id.begins_with("view_"):
		set_view(int(id.trim_prefix("view_")))
		return
	match id:
		"new": enter_painting()
		"continue":
			state=DayState.new()
			if state.load_day(): start_chapter(state.chapter)
			else: hud.toast("存档无法读取，已为你展开新的一天。"); new_day()
		"menu":
			if playing: state.save_day()
			set_menu()
		"chapters": paused=true; hud.show_chapters()
		"journal": paused=true; hud.show_journal(self)
		"pause": pause_game()
		"resume": resume_game()
		"sound": sound.toggle(); hud.show_pause()
		"quit": state.save_day(); get_tree().quit()
		_: do_action(id)

func pause_game() -> void:
	paused=true
	state.save_day()
	hud.game=self
	hud.show_pause()

func resume_game() -> void:
	paused=false
	if playing:
		if state.chapter==7: hud.show_summary(self)
		else: hud.show_game(self)
	else: set_menu()

func do_action(id: String) -> void:
	if paused or not playing: return
	narrative_override=""
	match id:
		"pick_basket":
			if state.placed>=3: return
			held="basket"
			set_view(1,false)
			hud.toast("已拿起竹篮。点击摊面，或按 E 放下。")
		"place":
			if held!="basket" or state.placed>=3: return
			held=""
			state.placed+=1
			world.set_stock(state.placed*4)
			sound.tone("water")
			if state.placed==3: state.add_memory("晨雾里支起梨摊")
		"show_pear":
			if state.customer_index>=3: return
			state.customer_ready=true
			held="pear"
			set_view(1,false)
		"fair","deal","high":
			var result=state.trade(id)
			narrative_override=result.text
			if result.ok:
				held=""
				world.set_stock(state.stock)
				sound.tone("coin")
				if state.customer_index==3:
					state.add_memory("卖出了十二只鲜梨")
					world.customer.visible=false
				else:
					world.customer.rotation.y=PI
				hud.toast(result.text)
		"bridge_call":
			if not state.bridge_helped:
				state.bridge_helped=true
				state.reputation+=1
				world.crossing=true
				state.add_memory("虹桥上帮船夫看水路")
				hud.toast("你向船夫招手：留神桥洞！船夫应声，将船稳住。")
		"bridge_watch":
			if not state.bridge_helped: return
			watching_bridge=true
			set_view(3,false)
			hud.toast("稍停片刻，看货船从虹桥下驶过。右键可以环顾。")
		"row_left": row("left"); return
		"row_right": row("right"); return
		"pack":
			state.packed=true
			world.set_stock(0)
			held="basket"
			hud.toast("竹篮收好了。鲜梨售罄，明日再来。")
		"count":
			state.counted=true
			held=""
			sound.tone("coin")
			state.add_memory("薄暮清账，收摊歇脚")
		"buy_wine":
			if not state.paid_wine and state.pay(6): state.paid_wine=true
			set_view(1,false)
		"pick_jug":
			if not state.paid_wine or state.pour>=1: return
			held="jug"
			set_view(1,false)
			hud.toast("握住酒壶，按住 E 或鼠标左键，慢慢把酒斟满。")
		"pour_hint":
			hud.toast("按住 E，或在画面中按住鼠标左键约三秒。")
		"toast":
			if state.pour<1 or state.toasted: return
			state.toasted=true
			state.reputation+=1
			held="cup"
			set_view(1,false)
			toast_animation=1.6
			state.add_memory("与老友碰杯话家常")
		"talk":
			set_view(2,false)
			narrative_override="老周望着你笑：“你看这河上的船，来了又去。钱挣多挣少，日子总得过得热乎。”"
		"buy_show":
			if not state.theatre_paid and state.pay(4): state.theatre_paid=true
			set_view(1,false)
		"listen":
			if state.theatre_paid and state.verses<3:
				state.verses+=1
				sound.tone("chapter")
				if state.verses==3: state.add_memory("灯下听完一折市井故事")
		"next":
			if state.is_complete(): start_chapter(state.chapter+1)
			return
		"to_close":
			start_chapter(4)
			return
		"to_theatre":
			start_chapter(6)
			return
		"finish":
			start_chapter(7)
			return
		_: return
	update_held()
	state.save_day()
	hud.show_game(self)

func row(side: String) -> void:
	if state.strokes>=10 or row_cooldown>0: return
	if side==state.last_side:
		hud.toast("换到另一侧划桨，船才走得稳。")
		return
	row_cooldown=.38
	state.last_side=side
	state.strokes+=1
	world.boat_progress=state.strokes/10.0
	world.oar.rotation.z=.6 if side=="left" else -.6
	sound.tone("water")
	if state.strokes>=10:
		state.add_memory("摇桨行过一段汴河")
		hud.toast("稳稳靠岸。两岸的叫卖声，又近了。")
	state.save_day()
	hud.show_game(self)

func speaker() -> String:
	match state.chapter:
		0: return "阿安  /  清晨的自语"
		1:
			return DayState.CUSTOMERS[state.customer_index].name if state.customer_index<3 else "阿安  /  生意告一段落"
		2: return "虹桥  /  桥上与船上的呼应"
		3: return "船夫  /  借你一支桨"
		4: return "阿安  /  薄暮清账"
		5: return "老周  /  同桌的朋友" if view_mode!=2 else "你是老周  /  看着阿安的一天"
		6: return "勾栏  /  灯火中的一折小戏"
	return "汴梁一日"

func narrative() -> String:
	if narrative_override!="": return narrative_override
	match state.chapter:
		0:
			if state.placed>=3: return "三筐鲜梨摆好了。河风吹起布幌，街上的脚步渐渐密起来。该迎接第一位客人了。"
			if held=="basket": return "竹篮有些分量。把这筐梨放到摊面上，摆得齐整些，客人才愿意停下脚步。"
			return "天刚亮，阿安已把小车推到虹桥边。拿起左边的竹篮，把十二只鲜梨分三次摆上摊。"
		1:
			if state.customer_index>=3: return "最后一位客人走远，梨摊空了，钱袋沉了些。桥上传来一阵喧闹，要不要去看看？"
			var c=DayState.CUSTOMERS[state.customer_index]
			if not state.customer_ready: return "%s走到摊前。先拿起一只梨，递近些，让客人看看成色。"%c.name
			if state.haggled: return "“这个价有些高。最多 %d 文一只，行不行？”你可以接受还价，也可以换个价钱。"%c.max
			return "“%s”"%c.line
		2:
			if state.is_complete(): return "船缓缓穿过桥影，船夫向岸上拱了拱手。热闹散开，汴河依旧向前。去水上走一段吧。"
			if watching_bridge: return "你暂且停下脚步。货船正沿着水路穿过虹桥，船身轻轻起伏，桥上的人也跟着松了一口气。"
			if state.bridge_helped: return "“多谢照应！”船夫在水面上应声。换到画外视角，看舟船怎样从桥下经过。"
			return "桥上的人纷纷倚栏。一艘货船正驶向桥洞，船夫忙着看水路。你站在桥头，替他照应一声。"
		3:
			if state.is_complete(): return "十桨过后，船轻轻贴近码头。你听懂了这条河的节奏：左一下，右一下，日子往前走。"
			return "“别急，用腰带着手。”按 A 划左桨，按 D 划右桨；也可以点下面的按钮。左右交替，划满十桨。"
		4:
			if state.is_complete(): return "售梨收入 %d 文，钱袋共 %d 文。日头落了，去酒馆找老周坐坐，或直接去勾栏听戏。"%[state.revenue,state.coins]
			if state.packed: return "收起竹篮，再把铜钱一枚枚理清。今天挣下的不只是盘缠，还有熟客的一声招呼。"
			return "夕照落在空空的摊面上。把竹篮收上车，清点今日所得，给忙碌的一天留个收尾。"
		5:
			if state.toasted: return "“来，为今日的好生意。”两盏相碰，清脆一声。可以换到朋友的视角，再看看阿安。"
			if state.pour>=1: return "酒斟到八分，给余下的留点人情。拿起桌上的酒盏，与老周碰一杯。"
			if held=="jug": return "慢慢倾斜酒壶。按住 E 或鼠标左键斟酒，松开便停下；左侧刻度会告诉你斟了多少。"
			if state.paid_wine: return "一壶薄酒，两个粗瓷盏。拿起桌上的青瓷酒壶，为这位老朋友斟酒。"
			return "老周早在孙羊正店等着：“忙了一日，坐。今夜的河风正好，咱们喝一壶？”酒钱六文。"
		6:
			if not state.theatre_paid: return "鼓板响了，勾栏里还有空位。花四文入座，听一折为本体验创作的市井小戏《一担春光》。"
			var verses=["开场 · “汴水悠悠接远帆，一担春光过市南。”台上的卖货郎挑担而来，故事刚刚开始。", "第二折 · “三文买梨嫌价贵，半盏清茶话短长。”客人几番还价，卖货郎却多赠了一只。", "第三折 · “莫道市声皆为利，一声相识便心安。”归家的路上，他又遇见了白日的客人。", "终场 · “画中不记寻常事，灯火深深各自还。”台上人作揖，台下人轻轻拍手。该回家了。"]
			return verses[state.verses]
	return ""

func task_text() -> String:
	match state.chapter:
		0: return "%s 拿起竹篮\n%s 将鲜梨摆上摊\n摆货进度   %d / 3"%["✓" if held=="basket" or state.placed>0 else "○","✓" if state.placed>=3 else "○",state.placed]
		1: return "三位客人   %d / 3\n单价 × 数量 = 收入\n让利积人情，还价多收入"%state.customer_index
		2: return "%s 招呼船夫看水路\n%s 观察货船穿桥\n右键环顾，看看桥头"%["✓" if state.bridge_helped else "○","✓" if state.bridge_watched else "○"]
		3: return "划桨进度   %d / 10\n下一桨：%s\nA 左桨   ·   D 右桨"%[state.strokes,"右侧" if state.last_side=="left" else "左侧" if state.last_side=="right" else "任意一侧"]
		4: return "%s 收起竹篮\n%s 清点铜钱\n售梨收入   %d 文"%["✓" if state.packed else "○","✓" if state.counted else "○",state.revenue]
		5: return "%s 一壶酒，六文钱\n%s 拿起酒壶，按住 E 斟酒\n%s 举杯，与老友碰盏"%["✓" if state.paid_wine else "○","✓" if state.pour>=1 else "○","✓" if state.toasted else "○"]
		6: return "%s 花四文入座\n听曲进度   %d / 3\n听完一折，为今日落款"%["✓" if state.theatre_paid else "○",state.verses]
	return ""

func actions() -> Array:
	match state.chapter:
		0:
			if state.is_complete(): return [{"id":"next","text":"迎接第一位客人  →"}]
			if held=="basket": return [{"id":"place","text":"E  放到摊面"}]
			return [{"id":"pick_basket","text":"E  拿起一筐梨"}]
		1:
			if state.is_complete(): return [{"id":"next","text":"去虹桥看热闹  →"},{"id":"to_close","text":"略过游逛，准备收摊"}]
			if not state.customer_ready: return [{"id":"show_pear","text":"E  拿起鲜梨，招呼客人"}]
			var c=DayState.CUSTOMERS[state.customer_index]
			return [{"id":"fair","text":"公道价  ·  %d 文 / 只"%c.fair},{"id":"high","text":("接受还价  ·  %d 文"%c.max) if state.haggled else ("试探开价  ·  %d 文"%c.high)},{"id":"deal","text":"让些利  ·  %d 文 / 只"%c.deal}]
		2:
			if not state.bridge_helped: return [{"id":"bridge_call","text":"E  招手，照应船夫"}]
			if not state.bridge_watched: return [{"id":"bridge_watch","text":"货船正在穿桥…" if watching_bridge else "E  看船过桥","disabled":watching_bridge}]
			return [{"id":"next","text":"去汴河划船  →"},{"id":"to_close","text":"回去收摊"}]
		3:
			if state.is_complete(): return [{"id":"next","text":"靠岸，回去收摊  →"}]
			return [{"id":"row_left","text":"A  划左桨","disabled":state.last_side=="left"},{"id":"row_right","text":"D  划右桨","disabled":state.last_side=="right"}]
		4:
			if not state.packed: return [{"id":"pack","text":"E  收起竹篮"}]
			if not state.counted: return [{"id":"count","text":"E  清点今日铜钱"}]
			return [{"id":"next","text":"去酒馆找老周  →"},{"id":"to_theatre","text":"直接去勾栏听戏"}]
		5:
			if state.toasted: return [{"id":"talk","text":"换作老周，聊聊家常"},{"id":"next","text":"去勾栏听曲  →"},{"id":"finish","text":"酒足夜深，回家"}]
			if not state.paid_wine: return [{"id":"buy_wine","text":"要一壶酒  ·  6 文","disabled":state.coins<6},{"id":"to_theatre","text":"改去听戏"}]
			if state.pour>=1: return [{"id":"toast","text":"E  端起酒盏，碰杯"}]
			if held=="jug": return [{"id":"pour_hint","text":"按住 E / 鼠标左键斟酒"}]
			return [{"id":"pick_jug","text":"E  拿起青瓷酒壶"}]
		6:
			if state.is_complete(): return [{"id":"finish","text":"一日已尽，为画卷落款  →"}]
			if not state.theatre_paid: return [{"id":"buy_show","text":"入座听曲  ·  4 文","disabled":state.coins<4},{"id":"finish","text":"留些盘缠，回家"}]
			return [{"id":"listen","text":"E  "+["听下一折","听下一折","听终场，作揖谢幕"][state.verses]}]
	return []

func _notification(what: int) -> void:
	if what==NOTIFICATION_WM_CLOSE_REQUEST:
		if playing: state.save_day()
		get_tree().quit()

func run_self_test() -> void:
	# Integration test exercises real scene, state, input routing, and chapter transitions.
	await get_tree().process_frame
	var save_before=FileAccess.get_file_as_string(DayState.SAVE_PATH) if FileAccess.file_exists(DayState.SAVE_PATH) else ""
	assert(not playing and not world.visible and hud.screen=="menu")
	var viewer=hud.painting_view
	viewer.show_full()
	assert(viewer.visible_source_rect().position.distance_to(Vector2.ZERO)<.1)
	assert(viewer.visible_source_rect().size.distance_to(viewer.IMAGE_SIZE)<.1)
	viewer.show_place("bridge")
	viewer.original_pixels()
	assert(is_equal_approx(viewer.image_zoom,1.0))
	var pivot=viewer.DETAIL.get_center()+Vector2(110,-30)
	var anchor_pixel=(pivot-viewer.image_origin())/viewer.image_zoom
	var wheel=InputEventMouseButton.new()
	wheel.position=pivot
	wheel.button_index=MOUSE_BUTTON_WHEEL_UP
	wheel.pressed=true
	viewer._gui_input(wheel)
	assert(is_equal_approx(viewer.image_zoom,1.24))
	assert(((pivot-viewer.image_origin())/viewer.image_zoom).distance_to(anchor_pixel)<.1)
	var press=InputEventMouseButton.new()
	press.position=viewer.DETAIL.get_center()
	press.button_index=MOUSE_BUTTON_LEFT
	press.pressed=true
	viewer._gui_input(press)
	var old_focus=viewer.image_focus
	var motion=InputEventMouseMotion.new()
	motion.relative=Vector2(50,12)
	viewer._gui_input(motion)
	assert(viewer.image_focus.distance_to(old_focus-motion.relative/viewer.image_zoom)<.1)
	press.pressed=false
	viewer._gui_input(press)
	assert(not viewer.dragging)
	viewer.move_overview(viewer.OVERVIEW.position)
	assert(viewer.visible_source_rect().position.x<.1 and viewer.tile_cache.has(0))
	viewer.move_overview(viewer.OVERVIEW.end)
	assert(absf(viewer.visible_source_rect().end.x-38414)<.1 and viewer.tile_cache.has(18))
	assert(viewer.tile_cache[18].get_width()==1550)
	assert(viewer.tile_cache.size()<=4)
	viewer.zoom_by(100)
	assert(is_equal_approx(viewer.image_zoom,viewer.max_zoom))
	viewer.zoom_by(.00001)
	assert(is_equal_approx(viewer.image_zoom,viewer.min_zoom))
	viewer.show_place("bridge")
	# Follow the same Button -> HUD signal -> main callback path used by a click.
	for child in hud.content.get_children():
		if child is Button and child.text.begins_with("点击入画"):
			child.pressed.emit()
	assert(entering_painting and not playing)
	on_action("new") # A second click cannot start another transition.
	await get_tree().create_timer(2.0).timeout
	assert(playing and world.visible and hud.screen=="game" and not entering_painting)
	assert(not hud.transition_cover.visible and state.sandbox)
	assert(world.material(World.WOOD).next_pass==world.ink_material)
	print("PAINTING_VIEW_PASS: full scroll, native tiles, zoom anchor, drag, both edges, final tile, zoom bounds, entry button, duplicate-click guard, toon material")
	camera.transform=camera_goal
	await get_tree().physics_frame
	await get_tree().physics_frame
	assert(ray_target(camera.unproject_position(world.targets["basket"].global_position))=="basket")
	var click=InputEventMouseButton.new()
	click.button_index=MOUSE_BUTTON_LEFT
	click.pressed=true
	click.position=camera.unproject_position(world.targets["basket"].global_position)
	_unhandled_input(click)
	assert(held=="basket")
	do_action("place")
	for i in range(2):
		do_action("pick_basket")
		assert(held=="basket")
		do_action("place")
	assert(state.is_complete())
	do_action("next")
	for i in range(3):
		do_action("show_pear")
		if i==1:
			do_action("high")
			assert(state.haggled)
			do_action("high")
		else: do_action("deal" if i==0 else "fair")
	assert(state.stock==0 and state.coins==64 and state.revenue==46)
	assert(state.reputation==2)
	do_action("next")
	do_action("bridge_call")
	do_action("bridge_call")
	assert(state.reputation==3)
	do_action("bridge_watch")
	assert(not state.is_complete())
	world.cargo_boat.position.x=12
	_process(.016)
	assert(state.is_complete())
	do_action("next")
	for i in range(10):
		row_cooldown=0
		row("left" if i%2==0 else "right")
	assert(state.is_complete())
	do_action("next")
	do_action("pack")
	do_action("count")
	do_action("next")
	do_action("buy_wine")
	do_action("buy_wine")
	assert(state.coins==58)
	do_action("pick_jug")
	var key=InputEventKey.new()
	key.keycode=KEY_E
	key.physical_keycode=KEY_E
	key.pressed=true
	Input.parse_input_event(key)
	Input.flush_buffered_events()
	await get_tree().process_frame
	for i in range(210): _process(1.0/60)
	var release=key.duplicate()
	release.pressed=false
	Input.parse_input_event(release)
	Input.flush_buffered_events()
	assert(state.pour>=1)
	do_action("toast")
	do_action("toast")
	assert(state.reputation==4)
	do_action("talk")
	assert(view_mode==2)
	do_action("next")
	do_action("buy_show")
	for i in range(3): do_action("listen")
	do_action("finish")
	assert(state.finished and state.coins==54 and state.expenses==10)
	for ch in range(7):
		state=DayState.new()
		state.prepare_chapter(ch)
		start_chapter(ch)
		for v in [1,2,3]:
			set_view(v)
			assert(camera_goal.origin.is_finite())
		assert(not actions().is_empty())
	var save_after=FileAccess.get_file_as_string(DayState.SAVE_PATH) if FileAccess.file_exists(DayState.SAVE_PATH) else ""
	assert(save_before==save_after)
	print("SELF_TEST_PASS: full day, 3 trades, haggle, guarded payments, alternating oars, pouring input, toast, theatre, 21 camera views, chapter sandbox, world raycast, existing save preserved")
	sound.stop_all()
	await get_tree().create_timer(.12).timeout
	get_tree().quit(0)

func capture_scenes() -> void:
	var capture_dir=ProjectSettings.globalize_path("res://captures/v2")
	DirAccess.make_dir_recursive_absolute(capture_dir)
	await get_tree().create_timer(2.0).timeout
	await RenderingServer.frame_post_draw
	get_viewport().get_texture().get_image().save_png(capture_dir+"/01_title.png")
	hud.painting_view.original_pixels()
	await RenderingServer.frame_post_draw
	get_viewport().get_texture().get_image().save_png(capture_dir+"/12_original_pixels.png")
	hud.painting_view.show_full()
	await RenderingServer.frame_post_draw
	get_viewport().get_texture().get_image().save_png(capture_dir+"/13_full_scroll.png")
	hud.painting_view.show_place("bridge")
	state=DayState.new()
	state.sandbox=true
	await enter_painting(false)
	for i in range(3): do_action("pick_basket"); do_action("place")
	set_view(3)
	hud.clear_toast()
	await get_tree().create_timer(1.5).timeout
	await RenderingServer.frame_post_draw
	get_viewport().get_texture().get_image().save_png(capture_dir+"/02_market.png")
	start_chapter(1)
	do_action("show_pear")
	await get_tree().create_timer(1.5).timeout
	await RenderingServer.frame_post_draw
	get_viewport().get_texture().get_image().save_png(capture_dir+"/03_bargain.png")
	for i in range(3):
		if not state.customer_ready: do_action("show_pear")
		do_action("fair")
	start_chapter(2)
	await get_tree().create_timer(1.5).timeout
	await RenderingServer.frame_post_draw
	get_viewport().get_texture().get_image().save_png(capture_dir+"/04_bridge.png")
	start_chapter(3)
	await get_tree().create_timer(1.5).timeout
	await RenderingServer.frame_post_draw
	get_viewport().get_texture().get_image().save_png(capture_dir+"/05_boat.png")
	state.add_memory("虹桥边观舟，汴河上摇桨")
	start_chapter(5)
	do_action("buy_wine")
	do_action("pick_jug")
	await get_tree().create_timer(1.5).timeout
	await RenderingServer.frame_post_draw
	get_viewport().get_texture().get_image().save_png(capture_dir+"/06_tavern.png")
	set_view(2)
	await get_tree().create_timer(1.5).timeout
	await RenderingServer.frame_post_draw
	get_viewport().get_texture().get_image().save_png(capture_dir+"/07_second_person.png")
	start_chapter(6)
	do_action("buy_show")
	await get_tree().create_timer(1.5).timeout
	await RenderingServer.frame_post_draw
	get_viewport().get_texture().get_image().save_png(capture_dir+"/08_theatre.png")
	for i in range(3): do_action("listen")
	do_action("finish")
	await get_tree().create_timer(1.5).timeout
	await RenderingServer.frame_post_draw
	get_viewport().get_texture().get_image().save_png(capture_dir+"/09_summary.png")
	hud.show_chapters()
	await RenderingServer.frame_post_draw
	get_viewport().get_texture().get_image().save_png(capture_dir+"/10_chapters.png")
	hud.show_journal(self)
	await RenderingServer.frame_post_draw
	get_viewport().get_texture().get_image().save_png(capture_dir+"/11_journal.png")
	print("CAPTURE_PASS")
	get_tree().quit()
