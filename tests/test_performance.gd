extends SceneTree
## Measure ordinary frame delivery without PNG readback or movie encoding stalls.
func _initialize() -> void:
	call_deferred("run")

func run() -> void:
	var game=load("res://scenes/main.tscn").instantiate()
	root.add_child(game)
	DisplayServer.window_move_to_foreground()
	DisplayServer.window_set_vsync_mode(DisplayServer.VSYNC_DISABLED)
	await process_frame
	game.state.sandbox=true
	for ch in [0,5]:
		game.start_chapter(ch)
		for view in [3,1]:
			game.set_view(view)
			await create_timer(1.5).timeout
			var begin=Time.get_ticks_msec()
			var count=0
			while Time.get_ticks_msec()-begin<2500:
				if view==3: game.seek_scroll(sin(count*.01)*15)
				await process_frame
				count+=1
			print("PERF chapter=",ch," view=",view," FPS=",snappedf(count/((Time.get_ticks_msec()-begin)/1000.0),.1)," calls=",Performance.get_monitor(Performance.RENDER_TOTAL_DRAW_CALLS_IN_FRAME)," focused=",DisplayServer.window_is_focused()," process_ms=",Performance.get_monitor(Performance.TIME_PROCESS)*1000," max_fps=",Engine.max_fps)
	game.sound.stop_all()
	await create_timer(.2).timeout
	quit()
