extends Node3D
## Original, procedural Song-inspired miniature. No external model dependencies.

const INK = Color("3d4440")
const WOOD = Color("786045")
const PALE_WOOD = Color("b09566")
const PLASTER = Color("d7c7a2")
const ROOF = Color("5e7069")
const RED = Color("934b3b")
const JADE = Color("577b72")
const SAND = Color("c7b48b")
var mats: Dictionary = {}
var targets: Dictionary = {}
var walkers: Array = []
var lanterns: Array = []
var boats: Array = []
var actors: Array = []
var night_lights: Array = []
var player: Node3D
var friend: Node3D
var customer: Node3D
var cargo_boat: Node3D
var row_boat: Node3D
var oar: Node3D
var sun: DirectionalLight3D
var environment: Environment
var pear_display: Node3D
var stream: MeshInstance3D
var water_mat: ShaderMaterial
var elapsed := 0.0
var is_night := false
var boat_progress := 0.0
var crossing := false
var rng := RandomNumberGenerator.new()
var font: FontVariation

func material(color: Color) -> StandardMaterial3D:
	var key = color.to_html()
	if not mats.has(key):
		var m = StandardMaterial3D.new()
		m.albedo_color = color
		m.roughness = 0.92
		mats[key] = m
	return mats[key]

func box(parent: Node3D, pos: Vector3, size: Vector3, color: Color) -> MeshInstance3D:
	var m = MeshInstance3D.new()
	var mesh = BoxMesh.new()
	mesh.size = size
	m.mesh = mesh
	m.material_override = material(color)
	m.position = pos
	parent.add_child(m)
	return m

func cylinder(parent: Node3D, pos: Vector3, bottom: float, top: float, height: float, color: Color, segments: int = 10) -> MeshInstance3D:
	var m = MeshInstance3D.new()
	var mesh = CylinderMesh.new()
	mesh.bottom_radius = bottom
	mesh.top_radius = top
	mesh.height = height
	mesh.radial_segments = segments
	m.mesh = mesh
	m.material_override = material(color)
	m.position = pos
	parent.add_child(m)
	return m

func sphere(parent: Node3D, pos: Vector3, scale_v: Vector3, color: Color) -> MeshInstance3D:
	var m = MeshInstance3D.new()
	var mesh = SphereMesh.new()
	mesh.radial_segments = 10
	mesh.rings = 5
	m.mesh = mesh
	m.material_override = material(color)
	m.position = pos
	m.scale = scale_v
	parent.add_child(m)
	return m

func beam(parent: Node3D, a: Vector3, b: Vector3, thickness: float, color: Color) -> MeshInstance3D:
	var m = box(parent, (a+b)*0.5, Vector3(thickness, a.distance_to(b), thickness), color)
	m.quaternion = Quaternion(Vector3.UP, (b-a).normalized())
	return m

func text3d(parent: Node3D, text: String, pos: Vector3, size: int = 52, color: Color = INK) -> Label3D:
	var label = Label3D.new()
	label.text = text
	label.font = font
	label.font_size = size
	label.pixel_size = 0.008
	label.modulate = color
	label.outline_size = 0
	label.position = pos
	parent.add_child(label)
	return label

func _ready() -> void:
	rng.seed = 1085
	font=FontVariation.new()
	font.base_font=preload("res://assets/fonts/NotoSerifSC.ttf")
	font.variation_opentype={"wght":550.0}
	build_lighting()
	build_land()
	build_bridge()
	build_town()
	build_market()
	build_tavern()
	build_stage()
	build_life()
	build_targets()

func build_lighting() -> void:
	var we = WorldEnvironment.new()
	environment = Environment.new()
	environment.background_mode = Environment.BG_COLOR
	environment.background_color = Color("d9d6c2")
	environment.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	environment.ambient_light_color = Color("eadbb7")
	environment.ambient_light_energy = 0.38
	environment.tonemap_mode = Environment.TONE_MAPPER_LINEAR
	we.environment = environment
	add_child(we)
	sun = DirectionalLight3D.new()
	sun.rotation_degrees = Vector3(-48, -28, 0)
	sun.light_color = Color("fff0d8")
	sun.light_energy = 0.7
	sun.shadow_enabled = true
	sun.directional_shadow_max_distance = 85
	add_child(sun)

func build_land() -> void:
	box(self, Vector3(0,-0.6,0), Vector3(68,0.8,46), Color("a08b64"))
	box(self, Vector3(0,-0.22,14), Vector3(66,0.8,18), SAND)
	box(self, Vector3(0,-0.22,-14), Vector3(66,0.8,18), SAND)
	for z in [-5.35,5.35]:
		box(self, Vector3(0,-0.02,z), Vector3(66,0.75,0.65), Color("9f9d88"))
		for x in range(-32,33,2):
			box(self, Vector3(x,0.36,z), Vector3(1.93,0.15,0.84), Color("bcb49b"))
	water_mat = ShaderMaterial.new()
	water_mat.shader = preload("res://scripts/water.gdshader")
	var water = box(self, Vector3(0,-0.1,0), Vector3(67,0.04,10.1), JADE)
	water.material_override = water_mat
	# Hand-laid street, thin inset stones, reeds and dock.
	for i in range(180):
		var x = rng.randf_range(-31,31)
		var z = rng.randf_range(5.8,21)
		box(self, Vector3(x,0.202,z), Vector3(rng.randf_range(.25,.8),.016,.22), Color("b4a784"))
	for x in [-24,-9,17,27]:
		for j in range(8):
			box(self, Vector3(x+j*.23,.25,4.1), Vector3(.21,.12,2.9), WOOD)
		for dx in [-.1,1.8]:
			cylinder(self,Vector3(x+dx,.1,2.9),.1,.09,1.6,WOOD)
	for i in range(18):
		var x = rng.randf_range(-30,30)
		for j in range(3):
			beam(self,Vector3(x+j*.15,.1,-5),Vector3(x+j*.15+.13,.8+rng.randf()*.4,-4.8),.025,JADE)
	for p in [Vector3(-25,0,7),Vector3(-20,0,-8),Vector3(-8,0,-7),Vector3(17,0,7),Vector3(28,0,-7),Vector3(24,0,19),Vector3(-26,0,18)]:
		willow(p)

func roof(parent: Node3D, width: float, depth: float, y: float, color: Color) -> void:
	var st = SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	for side in [-1,1]:
		for i in range(6):
			var a = float(i)/6.0
			var b = float(i+1)/6.0
			var ya = y + 1.15 * pow(1.0-a,2) + .16*pow(a,8)
			var yb = y + 1.15 * pow(1.0-b,2) + .16*pow(b,8)
			var pts = [Vector3(-width/2,ya,side*a*depth/2),Vector3(width/2,ya,side*a*depth/2),Vector3(-width/2,yb,side*b*depth/2),Vector3(width/2,yb,side*b*depth/2)]
			for idx in ([0,1,2,1,3,2] if side == 1 else [0,2,1,1,2,3]):
				st.add_vertex(pts[idx])
	st.generate_normals()
	var mesh = MeshInstance3D.new()
	mesh.mesh = st.commit()
	mesh.material_override = material(color)
	parent.add_child(mesh)
	beam(parent, Vector3(-width/2-.12,y+1.22,0),Vector3(width/2+.12,y+1.22,0),.15,INK)
	for side in [-1,1]:
		beam(parent,Vector3(-width/2,y+.17,side*depth/2),Vector3(width/2,y+.17,side*depth/2),.14,INK)
		# Tile courses following the upturned roof slope.
		for x in range(int(width/.37)+1):
			var px = -width/2+x*.37
			for i in range(4):
				var a = i/4.0
				var b = (i+1)/4.0
				beam(parent,Vector3(px,y+1.15*pow(1-a,2)+.16*pow(a,8)+.035,side*a*depth/2),Vector3(px,y+1.15*pow(1-b,2)+.16*pow(b,8)+.035,side*b*depth/2),.035,color.lightened(.12))

func house(pos: Vector3, width: float, depth: float, floors: int, title: String, color: Color = PLASTER) -> Node3D:
	var node = Node3D.new()
	add_child(node)
	node.position = pos
	var h = floors*2.6
	box(node,Vector3(0,h/2,0),Vector3(width,h,depth),color)
	box(node,Vector3(0,.25,0),Vector3(width+.18,.3,depth+.16),Color("999480"))
	# Rear elevations and side timbers keep the miniature legible from every angle.
	for x in [-width/2+.1,0,width/2-.1]:
		box(node,Vector3(x,h/2,-depth/2-.035),Vector3(.12,h,.12),WOOD)
	for f in range(floors):
		box(node,Vector3(0,f*2.6+.5,-depth/2-.08),Vector3(width,.12,.14),WOOD)
		box(node,Vector3(0,f*2.6+2.4,-depth/2-.08),Vector3(width,.14,.14),WOOD)
		for x in [-width*.27,width*.27]:
			box(node,Vector3(x,f*2.6+1.5,-depth/2-.09),Vector3(width*.23,1.03,.1),INK)
			for k in range(4):
				box(node,Vector3(x-width*.10+k*width*.068,f*2.6+1.5,-depth/2-.16),Vector3(.04,1.03,.04),PALE_WOOD)
			box(node,Vector3(x,f*2.6+1.5,-depth/2-.17),Vector3(width*.23,.045,.05),PALE_WOOD)
		for side in [-1,1]:
			box(node,Vector3(side*(width/2+.03),f*2.6+.5,0),Vector3(.1,.12,depth),WOOD)
			box(node,Vector3(side*(width/2+.03),f*2.6+2.4,0),Vector3(.1,.12,depth),WOOD)
			box(node,Vector3(side*(width/2+.03),h/2,0),Vector3(.1,h,.12),WOOD)
	for x in [-width/2+.1,0,width/2-.1]:
		box(node,Vector3(x,h/2,depth/2+.05),Vector3(.13,h,.15),WOOD)
	for f in range(floors):
		box(node,Vector3(0,f*2.6+.55,depth/2+.08),Vector3(width,.12,.14),WOOD)
		box(node,Vector3(0,f*2.6+2.45,depth/2+.08),Vector3(width,.15,.15),WOOD)
		for x in [-width*.28,width*.28]:
			box(node,Vector3(x,f*2.6+1.55,depth/2+.09),Vector3(width*.27,1.2,.12),INK)
			for k in range(5):
				box(node,Vector3(x-width*.125+k*width*.0625,f*2.6+1.55,depth/2+.17),Vector3(.045,1.2,.045),PALE_WOOD)
			for k in [-.35,0,.35]:
				box(node,Vector3(x,f*2.6+1.55+k,depth/2+.18),Vector3(width*.28,.035,.04),PALE_WOOD)
	if floors>1:
		box(node,Vector3(0,2.7,depth/2+.4),Vector3(width+.5,.15,.9),WOOD)
		for x in range(9):
			box(node,Vector3(-width/2+x*width/8,3.05,depth/2+.8),Vector3(.065,.7,.065),WOOD)
		box(node,Vector3(0,3.42,depth/2+.8),Vector3(width+.2,.1,.1),WOOD)
	roof(node,width+.9,depth+1.0,h,ROOF)
	if title != "":
		box(node,Vector3(0,2.12,depth/2+.23),Vector3(minf(width*.8,3),.65,.13),WOOD)
		text3d(node,title,Vector3(0,2.12,depth/2+.31),42,Color("e5d4ac"))
		lantern(node,Vector3(-width/2+.4,2.05,depth/2+.75))
	return node

func willow(pos: Vector3) -> void:
	var node = Node3D.new()
	add_child(node)
	node.position = pos
	beam(node,Vector3.ZERO,Vector3(.4,4.3,0),.3,WOOD)
	for i in range(6):
		var angle = i*TAU/6
		var tip = Vector3(cos(angle)*1.8,4.2+rng.randf(),sin(angle)*1.6)
		beam(node,Vector3(.3,2.8,0),tip,.12,WOOD)
		sphere(node,tip,Vector3(2.6,.65,1.7),Color("7c8e65"))
		for j in range(3):
			var start = tip+Vector3(j*.3-.3,0,.2)
			var end = start+Vector3(.25,-1.7-rng.randf(),.13)
			beam(node,start,end,.04,Color("899366"))
			for k in range(4):
				sphere(node,start.lerp(end,k/4.0),Vector3(.25,.45,.18),Color("899366"))

func lantern(parent: Node3D, pos: Vector3) -> void:
	beam(parent,pos+Vector3(0,.4,0),pos+Vector3(0,.8,0),.035,WOOD)
	var lamp = sphere(parent,pos,Vector3(.55,.55,.55),RED)
	cylinder(parent,pos+Vector3(0,-.29,0),.16,.16,.07,WOOD)
	cylinder(parent,pos+Vector3(0,.29,0),.16,.16,.07,WOOD)
	beam(parent,pos+Vector3(0,-.3,0),pos+Vector3(0,-.65,0),.04,RED)
	lanterns.append(lamp)

func build_town() -> void:
	var names = ["脚店","绸缎庄","茶坊","香药铺","解库","客舍","纸马铺","脚店"]
	for i in range(8):
		house(Vector3(-28+i*8,0,-12),6,5,2 if i%3==1 else 1,names[i])
	for i in range(7):
		house(Vector3(-27+i*8.4,0,-21),6.7,5,2,"",Color("bdb697"))
	house(Vector3(-25,0,17),6,4,2,"顺风客舍")
	house(Vector3(-14,0,7),7,3,2,"孙羊正店")
	house(Vector3(0,0,20),5.8,4,1,"赵家茶坊").rotation.y=PI
	house(Vector3(10,0,20),6,4,2,"绸 · 布").rotation.y=PI
	house(Vector3(-14,0,21),6,4,1,"陈家香铺").rotation.y=PI
	house(Vector3(-6.8,0,21),4.3,4,2,"客 舍").rotation.y=PI
	house(Vector3(26,0,18),5,4,2,"瓦舍")
	for x in [-21,0,20]:
		beam(self,Vector3(x,0,-7),Vector3(x,4.8,-7),.12,WOOD)
		box(self,Vector3(x+.6,3.9,-7),Vector3(1.1,1.6,.045),PLASTER)
		text3d(self,"茶\n肆",Vector3(x+.6,3.9,-6.96),48,RED)

func build_bridge() -> void:
	for i in range(32):
		var z = -6.3+i*.4
		var y = bridge_height(z)
		var plank = box(self,Vector3(8,y,z),Vector3(4.1,.19,.395),PALE_WOOD)
		plank.rotation.x = atan((bridge_height(z-.1)-bridge_height(z+.1))/.2)
		if i%2==0:
			for x in [5.92,10.08]:
				box(self,Vector3(x,y+.57,z),Vector3(.1,1.12,.1),WOOD)
	for i in range(16):
		var z = -6.4+i*.8
		for x in [5.92,10.08]:
			for h in [.45,1.05]:
				beam(self,Vector3(x,bridge_height(z)+h,z),Vector3(x,bridge_height(z+.8)+h,z+.8),.11,WOOD)
	for x in [6.2,9.8]:
		for i in range(8):
			var z = -6.4+i*1.6
			beam(self,Vector3(x,bridge_height(z)-.35,z),Vector3(x,bridge_height(z+1.6)-.35,z+1.6),.27,WOOD)
		beam(self,Vector3(x,.3,-5.8),Vector3(x,2.35,-.7),.22,WOOD)
		beam(self,Vector3(x,.3,5.8),Vector3(x,2.35,.7),.22,WOOD)

func bridge_height(z: float) -> float:
	return .28+2.35*maxf(0.0,1.0-pow(z/6.5,2))

func stall(pos: Vector3, cloth: Color, title: String) -> Node3D:
	var n = Node3D.new()
	add_child(n)
	n.position = pos
	for x in [-1.45,1.45]:
		for z in [-.8,.8]:
			box(n,Vector3(x,1.4,z),Vector3(.1,2.8,.1),WOOD)
	var shade = box(n,Vector3(0,2.8,0),Vector3(3.4,.1,2.4),cloth)
	shade.rotation.x = .07
	for x in range(9):
		box(n,Vector3(-1.6+x*.4,2.64,1.18),Vector3(.35,.35,.045),cloth.lightened(.04))
	box(n,Vector3(0,.96,0),Vector3(3.1,.17,1.7),WOOD)
	for x in [-1.2,1.2]:
		box(n,Vector3(x,.5,0),Vector3(.12,.9,1.5),WOOD)
	text3d(n,title,Vector3(0,2.72,1.26),48,Color("eee4c9"))
	return n

func basket(parent: Node3D,pos: Vector3,radius: float = .4) -> void:
	cylinder(parent,pos,radius*.8,radius,radius*.75,PALE_WOOD)
	for i in range(4):
		var tor = TorusMesh.new()
		tor.inner_radius = radius*(.84+i*.04)
		tor.outer_radius = tor.inner_radius+.035
		tor.rings = 16
		tor.ring_segments = 6
		var m = MeshInstance3D.new()
		m.mesh=tor
		m.material_override=material(WOOD)
		m.position=pos+Vector3(0,-radius*.3+i*radius*.18,0)
		parent.add_child(m)

func pear(parent: Node3D,pos: Vector3) -> Node3D:
	var n = Node3D.new()
	parent.add_child(n)
	n.position=pos
	sphere(n,Vector3.ZERO,Vector3(.25,.3,.25),Color("c5b15f"))
	sphere(n,Vector3(0,.1,0),Vector3(.14,.2,.14),Color("c5b15f"))
	beam(n,Vector3(0,.16,0),Vector3(.02,.24,0),.025,WOOD)
	return n

func build_market() -> void:
	stall(Vector3(-3,0,10.8),Color("657f75"),"阿 · 安 · 鲜 · 梨")
	pear_display=Node3D.new()
	add_child(pear_display)
	for i in range(12):
		pear(pear_display,Vector3(-3.85+(i%4)*.55,1.16,10.35+floori(i/4.0)*.42))
	basket(self,Vector3(-5.1,.52,12),.52)
	basket(self,Vector3(-4.8,.42,10),.46)
	for i in range(5):
		pear(self,Vector3(-5.3+(i%2)*.22,.76,11.8+(i/2)*.16))
	var s = stall(Vector3(3,0,13),RED,"热 汤 · 炊 饼")
	for x in [-.8,0,.8]:
		basket(s,Vector3(x,1.16,0),.3)
		for i in range(3):
			sphere(s,Vector3(x+i*.11-.11,1.28,0),Vector3(.23,.14,.23),Color("e3d3ae"))
	s = stall(Vector3(14,0,9.6),Color("ac9465"),"竹 器")
	for x in [-.8,0,.8]:
		basket(s,Vector3(x,1.25,0),.34)
	box(self,Vector3(-.9,1.9,10.8),Vector3(.7,1.5,.08),PLASTER)
	text3d(self,"鲜\n梨",Vector3(-.9,1.9,10.87),58,INK)
	# Handcart and storage boxes.
	box(self,Vector3(-6,.7,14),Vector3(1.8,.18,1.2),WOOD)
	for x in [-6.95,-5.05]:
		var wheel=cylinder(self,Vector3(x,.5,14),.47,.47,.13,INK,16)
		wheel.rotation.z=PI/2
		beam(self,Vector3(x,.7,14),Vector3(x,1,16),.09,WOOD)
	box(self,Vector3(-6,1,14),Vector3(1.3,.6,1),PALE_WOOD)

func jug(parent: Node3D,pos: Vector3) -> Node3D:
	var n = Node3D.new()
	parent.add_child(n)
	n.position=pos
	sphere(n,Vector3(0,.18,0),Vector3(.43,.5,.43),JADE)
	cylinder(n,Vector3(0,.48,0),.12,.11,.2,JADE)
	cylinder(n,Vector3(0,.6,0),.14,.13,.07,WOOD)
	beam(n,Vector3(.13,.26,0),Vector3(.35,.46,0),.09,JADE)
	return n

func cup(parent: Node3D,pos: Vector3) -> Node3D:
	var n = Node3D.new()
	parent.add_child(n)
	n.position=pos
	cylinder(n,Vector3(0,.08,0),.08,.16,.16,Color("b5c4b0"))
	cylinder(n,Vector3(0,.164,0),.135,.135,.006,Color("6b6753"))
	return n

func build_tavern() -> void:
	add_night_light(Vector3(-14,2.8,11.8),6.0)
	box(self,Vector3(-14,.21,11),Vector3(7,.08,5),Color("a69776"))
	cylinder(self,Vector3(-14,1.04,11.2),1.1,1.1,.15,WOOD,20)
	cylinder(self,Vector3(-14,.6,11.2),.18,.18,.85,WOOD)
	for z in [9.65,12.8]:
		box(self,Vector3(-14,.52,z),Vector3(1,.12,.45),WOOD)
		for x in [-14.35,-13.65]:
			box(self,Vector3(x,.3,z),Vector3(.1,.5,.35),WOOD)
	jug(self,Vector3(-14.5,1.13,11.25)).name="WineJug"
	cup(self,Vector3(-13.6,1.13,11.5)).name="PlayerCup"
	cup(self,Vector3(-14,1.13,10.7)).name="FriendCup"
	var plate = cylinder(self,Vector3(-14.25,1.15,11.65),.24,.27,.05,PLASTER)
	for i in range(5):
		sphere(plate,Vector3(rng.randf_range(-.15,.15),.05,rng.randf_range(-.15,.15)),Vector3(.09,.07,.09),Color("9c7b4a"))
	for x in [-17,-11]:
		beam(self,Vector3(x,0,12.5),Vector3(x,3.4,12.5),.15,WOOD)
		lantern(self,Vector3(x,2.7,12.5))
	for i in range(3):
		cylinder(self,Vector3(-17+i*.7,.6,8.9),.3,.24,.9,Color("735b46"))

func build_stage() -> void:
	add_night_light(Vector3(23,3.5,12.5),7.0)
	box(self,Vector3(23,.6,11),Vector3(6,1,3.7),WOOD)
	box(self,Vector3(23,2.4,9.15),Vector3(6,3,.15),RED)
	for x in [20,26]:
		box(self,Vector3(x,2.35,11),Vector3(.18,3.5,3.8),WOOD)
	var n = Node3D.new()
	add_child(n)
	n.position = Vector3(23,0,11)
	roof(n,7,4.6,4.1,ROOF)
	box(self,Vector3(23,3.7,12.9),Vector3(3.3,.6,.12),WOOD)
	text3d(self,"勾 栏 听 曲",Vector3(23,3.7,12.98),45,PLASTER)
	for p in [Vector3(22,1.12,11),Vector3(24,1.12,11)]:
		var actor = person(self,p,RED if p.x<23 else JADE)
		actors.append(actor)
	for z in [15,17]:
		for x in [21,24]:
			box(self,Vector3(x,.55,z),Vector3(2,.12,.45),WOOD)
			for dx in [-.8,.8]:
				box(self,Vector3(x+dx,.3,z),Vector3(.1,.5,.4),WOOD)
	for x in [20.2,25.8]:
		lantern(self,Vector3(x,3.1,12.8))

func add_night_light(pos: Vector3,radius: float) -> void:
	var light=OmniLight3D.new()
	light.position=pos
	light.light_color=Color("ffbd76")
	light.light_energy=1.0
	light.omni_range=radius
	light.omni_attenuation=1.2
	light.visible=false
	add_child(light)
	night_lights.append(light)

func person(parent: Node3D,pos: Vector3,color: Color,hat: bool = true) -> Node3D:
	var n = Node3D.new()
	parent.add_child(n)
	n.position=pos
	for x in [-.14,.14]:
		box(n,Vector3(x,.17,.05),Vector3(.18,.18,.38),INK)
	cylinder(n,Vector3(0,.67,0),.39,.23,1.02,color)
	box(n,Vector3(0,.87,.015),Vector3(.52,.08,.42),WOOD)
	beam(n,Vector3(-.17,1.18,.17),Vector3(.2,.93,.22),.045,PLASTER)
	sphere(n,Vector3(0,1.43,0),Vector3(.42,.46,.4),Color("c6a579"))
	sphere(n,Vector3(0,1.59,-.04),Vector3(.44,.25,.42),INK)
	if hat:
		cylinder(n,Vector3(0,1.65,0),.29,.19,.16,INK)
	else:
		sphere(n,Vector3(0,1.75,-.06),Vector3(.16,.18,.16),INK)
	for x in [-.085,.085]:
		sphere(n,Vector3(x,1.46,.184),Vector3(.032,.038,.025),INK)
	for side in [-1,1]:
		var arm = Node3D.new()
		n.add_child(arm)
		arm.name = "LeftArm" if side<0 else "RightArm"
		arm.position=Vector3(side*.25,1.14,0)
		var sleeve = cylinder(arm,Vector3(side*.09,-.22,0),.17,.12,.48,color)
		sleeve.rotation.z = side*.27
		sphere(arm,Vector3(side*.15,-.49,0),Vector3(.14,.17,.14),Color("c6a579"))
	return n

func boat(pos: Vector3,covered: bool = true) -> Node3D:
	var n = Node3D.new()
	add_child(n)
	n.position=pos
	box(n,Vector3(0,.25,0),Vector3(4.5,.24,1.5),WOOD)
	for z in [-.76,.76]:
		box(n,Vector3(0,.58,z),Vector3(4.7,.6,.12),PALE_WOOD)
	for x in [-2.4,2.4]:
		var bow=box(n,Vector3(x,.46,0),Vector3(.7,.16,1.5),WOOD)
		bow.rotation.z = signf(x)*.45
	for x in [-1.6,-.8,0,.8,1.6]:
		box(n,Vector3(x,.43,0),Vector3(.07,.05,1.4),PALE_WOOD)
	if covered:
		for x in [-.8,.8]:
			for z in [-.65,.65]:
				box(n,Vector3(x,1.08,z),Vector3(.08,1.25,.08),WOOD)
		box(n,Vector3(0,1.75,0),Vector3(2.2,.1,1.7),Color("b09f78"))
		basket(n,Vector3(-1.5,.66,0),.35)
	return n

func build_life() -> void:
	player = person(self,Vector3(-3,0,9.25),JADE,false)
	friend = person(self,Vector3(-14,0,9.65),Color("a77c57"))
	customer = person(self,Vector3(-3,0,12.7),Color("7a7690"),false)
	customer.rotation.y=PI
	cargo_boat=boat(Vector3(-5,0,0.2))
	person(cargo_boat,Vector3(1.5,.4,0),Color("6d7169"),false).scale=Vector3.ONE*.8
	row_boat=boat(Vector3(-17,0,2.5),false)
	oar=beam(row_boat,Vector3(.7,.8,-.4),Vector3(1.9,-.2,-1.8),.07,WOOD)
	box(oar,Vector3(0,-.85,0),Vector3(.3,.6,.07),WOOD)
	for p in [Vector3(-24,0,-2),Vector3(21,0,-2.5),Vector3(29,0,1)]:
		boats.append(boat(p))
	var colors = [JADE,RED,Color("8b825e"),Color("717783"),Color("ab9876"),Color("817663")]
	for i in range(36):
		var p=Vector3(rng.randf_range(-29,29),.22,6.2+rng.randf()*1.5 if i<18 else 16+rng.randf()*2)
		var person_node=person(self,p,colors[i%colors.size()],i%3!=0)
		var direction = 1.0 if i%2==0 else -1.0
		person_node.rotation.y=direction*PI/2
		walkers.append({"node":person_node,"speed":rng.randf_range(.18,.48)*direction,"phase":rng.randf()*TAU})
	for i in range(9):
		var z=-4+i
		person(self,Vector3(6.6 if i%2 else 9.1,bridge_height(z)+.12,z),colors[i%6]).rotation.y = -PI/2
	for i in range(6):
		person(self,Vector3(20.6+(i%3)*1.4,.22,15+(i/3)*2),colors[i%6]).rotation.y=PI

func target(id: String,pos: Vector3,size: Vector3,title: String) -> void:
	var area=StaticBody3D.new()
	area.position=pos
	area.set_meta("target",id)
	area.set_meta("title",title)
	var shape=CollisionShape3D.new()
	var collision=BoxShape3D.new()
	collision.size=size
	shape.shape=collision
	area.add_child(shape)
	add_child(area)
	targets[id]=area

func build_targets() -> void:
	target("basket",Vector3(-5.1,.65,12),Vector3(1.2,1.2,1.2),"竹篮 · 拿起一筐梨")
	target("counter",Vector3(-3,1.1,10.8),Vector3(3.1,.45,1.8),"摊面 · 放下鲜梨")
	target("customer",Vector3(-3,1,12.7),Vector3(.8,1.9,.7),"顾客 · 招呼客人")
	target("jug",Vector3(-14.5,1.42,11.25),Vector3(.7,.8,.7),"青瓷酒壶 · 拿起")
	target("cup",Vector3(-13.6,1.28,11.5),Vector3(.5,.4,.5),"酒盏 · 举杯")

func set_stock(count: int) -> void:
	for i in range(pear_display.get_child_count()):
		pear_display.get_child(i).visible=i<count

func set_time(chapter: int) -> void:
	is_night=chapter>=5
	var bg = Color("dad7c2")
	var light = Color("fff0d8")
	var energy=.7
	if chapter==4:
		bg=Color("ccaa8a")
		light=Color("ffcc91")
		energy=.6
	elif is_night:
		bg=Color("36484e")
		light=Color("c7dbe0")
		energy=.18
	environment.background_color=bg
	environment.ambient_light_color=Color("b6c5c6") if is_night else Color("eadbb7")
	environment.ambient_light_energy=.24 if is_night else .38
	sun.light_color=light
	sun.light_energy=energy
	sun.rotation_degrees.x=-25 if chapter==4 else -48
	for light_node in night_lights: light_node.visible=is_night
	for lamp in lanterns:
		var m = material(Color("ffc075") if is_night else RED)
		if is_night:
			m.emission_enabled=true
			m.emission=Color("e59b42")
			m.emission_energy_multiplier=.6
		lamp.material_override=m

func _process(delta: float) -> void:
	elapsed+=delta
	for w in walkers:
		var n: Node3D=w.node
		n.position.x+=delta*w.speed
		if absf(n.position.x)>31:
			n.position.x=-signf(n.position.x)*30.8
		n.position.y=.22+sin(elapsed*4+w.phase)*.018
		n.get_node("LeftArm").rotation.x=sin(elapsed*4+w.phase)*.22
		n.get_node("RightArm").rotation.x=-sin(elapsed*4+w.phase)*.22
	for i in range(actors.size()):
		actors[i].rotation.y=sin(elapsed*.8+i)*.3
		actors[i].get_node("LeftArm").rotation.z=.8+sin(elapsed*1.4+i)*.5
		actors[i].get_node("RightArm").rotation.z=-.8-sin(elapsed*1.2+i)*.5
	for i in range(boats.size()):
		boats[i].position.y=sin(elapsed*.7+i)*.04
		boats[i].position.x+=delta*.12
		if boats[i].position.x>34:
			boats[i].position.x=-34
	if crossing:
		cargo_boat.position.x=move_toward(cargo_boat.position.x,14.0,delta*1.6)
	cargo_boat.position.y=sin(elapsed)*.035
	row_boat.position.y=sin(elapsed*1.3)*.04
	row_boat.position.x=lerpf(row_boat.position.x,-17+boat_progress*19,delta*2)
