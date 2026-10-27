extends SceneTree
const State=preload("res://scripts/day_state.gd")

func _init() -> void:
	# Exhaustive 27 combinations of the three customer choices.
	var paths=0
	for a in ["fair","deal","high"]:
		for b in ["fair","deal","high"]:
			for c in ["fair","deal","high"]:
				var s=State.new()
				s.chapter=1
				for choice in [a,b,c]:
					s.customer_ready=true
					if choice=="high":
						var before=s.coins
						assert(not s.trade(choice).ok)
						assert(s.coins==before and s.haggled)
					assert(s.trade(choice).ok)
				assert(s.stock==0 and s.customer_index==3 and s.is_complete())
				assert(s.coins==18+s.revenue-s.expenses)
				assert(s.revenue>=33 and s.revenue<=52)
				assert(not s.trade("fair").ok)
				paths+=1
	var saved=State.new()
	saved.chapter=5
	saved.paid_wine=true
	saved.pour=.62
	saved.coins=12
	saved.expenses=6
	saved.add_memory("酒馆斟酒中途存档")
	var path="user://qa_day_state.json"
	saved.save_day(path)
	var restored=State.new()
	assert(restored.load_day(path))
	assert(restored.chapter==5 and restored.paid_wine)
	assert(is_equal_approx(restored.pour,.62))
	assert(restored.memories==saved.memories)
	assert(not restored.pay(999) and restored.coins==12)
	assert(restored.pay(4) and restored.coins==8)
	var sandbox=State.new()
	sandbox.prepare_chapter(6)
	sandbox.save_day(path)
	assert(State.new().load_day(path))
	var original=State.new()
	original.load_day(path)
	assert(original.chapter==5)
	DirAccess.remove_absolute(path)
	print("STATE_TEST_PASS: %d trade paths; inventory conservation; fair/high/discount totals; insufficient funds; save/load including partial pour; sandbox does not overwrite save"%paths)
	quit(0)
