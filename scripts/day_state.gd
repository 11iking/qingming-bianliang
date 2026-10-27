extends RefCounted
## All persistent decisions live here, independent of presentation and input.
const SAVE_PATH = "user://day_v1.json"
const CHAPTER_NAMES = ["晨起出摊", "市井买卖", "虹桥观舟", "汴河行船", "薄暮收摊", "小酌叙旧", "勾栏听曲", "一日入画"]
const HOURS = ["卯时 · 06:00", "巳时 · 10:00", "午时 · 12:00", "未时 · 14:00", "酉时 · 18:00", "戌时 · 19:00", "亥时 · 21:00", "夜深 · 22:00"]
const CUSTOMERS = [
	{"name":"早起的邻里", "line":"阿安，家里孩子惦记这口甜。三只梨，能算便宜些么？", "qty":3, "fair":3, "high":5, "deal":2, "max":4},
	{"name":"赶路的客商", "line":"这梨倒新鲜。我要四只带上船，价钱合适就买。", "qty":4, "fair":4, "high":6, "deal":3, "max":5},
	{"name":"茶坊的掌柜", "line":"剩下这几只，我拿去配茶。咱们往后常做生意，如何？", "qty":5, "fair":4, "high":5, "deal":3, "max":4}
]
var chapter := 0
var coins := 18
var stock := 12
var reputation := 0
var revenue := 0
var expenses := 0
var placed := 0
var customer_index := 0
var haggled := false
var customer_ready := false
var bridge_helped := false
var bridge_watched := false
var strokes := 0
var last_side := ""
var packed := false
var counted := false
var paid_wine := false
var pour := 0.0
var toasted := false
var theatre_paid := false
var verses := 0
var finished := false
var memories: Array = []
var sales: Array = []
var sandbox := false

func is_complete() -> bool:
	match chapter:
		0: return placed >= 3
		1: return customer_index >= 3
		2: return bridge_helped and bridge_watched
		3: return strokes >= 10
		4: return packed and counted
		5: return toasted
		6: return verses >= 3
		7: return true
	return false

func trade(choice: String) -> Dictionary:
	if customer_index>=3: return {"ok":false,"text":"今日的客人都招呼过了。"}
	if not customer_ready: return {"ok":false,"text":"先拿起一只梨，请客人瞧瞧。"}
	var c: Dictionary=CUSTOMERS[customer_index]
	if choice=="high" and not haggled:
		haggled=true
		return {"ok":false,"text":"“%d 文一只？太贵啦！最多 %d 文，再商量商量？”" % [c.high,c.max]}
	var price=int(c.fair)
	if choice=="deal":
		price=int(c.deal)
		reputation+=2
	elif choice=="high":
		price=int(c.max)
		reputation-=1
	else:
		reputation+=1
	var qty=mini(stock,int(c.qty))
	var total=price*qty
	coins+=total
	revenue+=total
	stock-=qty
	sales.append({"customer":c.name,"qty":qty,"price":price,"total":total})
	customer_index+=1
	customer_ready=false
	haggled=false
	return {"ok":true,"text":"成交！%d 只鲜梨，共 %d 文。%s" % [qty,total,"客人记下了这份人情。" if choice=="deal" else "钱货两清，互道一声好。"]}

func pay(amount: int) -> bool:
	if coins<amount: return false
	coins-=amount
	expenses+=amount
	return true

func add_memory(text: String) -> void:
	if not memories.has(text): memories.append(text)

func data() -> Dictionary:
	return {"version":1,"chapter":chapter,"coins":coins,"stock":stock,"reputation":reputation,"revenue":revenue,"expenses":expenses,"placed":placed,"customer_index":customer_index,"haggled":haggled,"customer_ready":customer_ready,"bridge_helped":bridge_helped,"bridge_watched":bridge_watched,"strokes":strokes,"last_side":last_side,"packed":packed,"counted":counted,"paid_wine":paid_wine,"pour":pour,"toasted":toasted,"theatre_paid":theatre_paid,"verses":verses,"finished":finished,"memories":memories,"sales":sales}

func save_day(path: String = SAVE_PATH) -> void:
	if sandbox: return
	var file=FileAccess.open(path,FileAccess.WRITE)
	if file: file.store_string(JSON.stringify(data()))

func load_day(path: String = SAVE_PATH) -> bool:
	if not FileAccess.file_exists(path): return false
	var d=JSON.parse_string(FileAccess.get_file_as_string(path))
	if not d is Dictionary or d.get("version",0)!=1: return false
	for key in data().keys():
		if key!="version" and d.has(key): set(key,d[key])
	chapter=clampi(chapter,0,7)
	coins=maxi(coins,0)
	stock=clampi(stock,0,12)
	placed=clampi(placed,0,3)
	customer_index=clampi(customer_index,0,3)
	strokes=clampi(strokes,0,10)
	pour=clampf(pour,0,1)
	verses=clampi(verses,0,3)
	return true

func prepare_chapter(index: int) -> void:
	sandbox=true
	chapter=index
	if index>0: placed=3
	if index>=4:
		coins=65
		revenue=47
		stock=0
		customer_index=3
