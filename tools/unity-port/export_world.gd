extends SceneTree
var world
var nodes: Array = []
var file: FileAccess
var out_dir = "res://UnityProject/Assets/Resources/Imported"
func _initialize():
    call_deferred("export_world")
func v3(v: Vector3) -> Array: return [v.x,v.y,v.z]
func path(n: Node) -> String: return str(world.get_path_to(n))
func encode_metadata(value):
    if value is Node: return path(value)
    if value is Vector3: return v3(value)
    if value is Array:
        var a=[]
        for x in value: a.append(encode_metadata(x))
        return a
    if value is Dictionary:
        var d={}
        for k in value: d[str(k)]=encode_metadata(value[k])
        return d
    return value
func walk(n: Node3D, parent: int):
    if n.is_queued_for_deletion(): return
    var index=nodes.size()
    nodes.append({"node":n,"parent":parent})
    for c in n.get_children():
        if c is Node3D: walk(c,index)
func put_string(s: String):
    var bytes=s.to_utf8_buffer()
    file.store_32(bytes.size())
    file.store_buffer(bytes)
func put_v3(v: Vector3):
    file.store_float(v.x); file.store_float(v.y); file.store_float(v.z)
func put_color(c: Color):
    file.store_float(c.r);file.store_float(c.g);file.store_float(c.b);file.store_float(c.a)
func export_world():
    DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(out_dir))
    world=load("res://scripts/world.gd").new()
    world.name="BianliangWorld"
    root.add_child(world)
    world.process_mode=Node.PROCESS_MODE_DISABLED
    await process_frame
    var props=Node3D.new();props.name="HeldTemplates";world.add_child(props)
    for kind in ["basket","pear","jug","cup"]:
        var holder=Node3D.new();holder.name=kind;props.add_child(holder)
        match kind:
            "basket":
                world.basket(holder,Vector3(0,.06,0),.4)
                for i in range(3):world.pear(holder,Vector3((i-1)*.19,.24,0))
            "pear": world.pear(holder,Vector3(0,.08,0)).scale=Vector3.ONE*1.6
            "jug": world.jug(holder,Vector3.ZERO)
            "cup": world.cup(holder,Vector3(0,.02,0))
    props.visible=false
    walk(world,-1)
    file=FileAccess.open(out_dir+"/World.bytes",FileAccess.WRITE)
    file.store_32(0x514d5731);file.store_32(nodes.size())
    var mesh_count=0;var vertices=0
    for entry in nodes:
        var n=entry.node
        file.store_32(entry.parent)
        put_string(str(n.name))
        put_v3(n.position);var q=n.quaternion
        file.store_float(q.x);file.store_float(q.y);file.store_float(q.z);file.store_float(q.w)
        put_v3(n.scale);file.store_8(1 if n.visible else 0)
        var count=n.mesh.get_surface_count() if n is MeshInstance3D and n.mesh else 0
        file.store_32(count)
        for surface in range(count):
            var arrays=n.mesh.surface_get_arrays(surface)
            var vs=arrays[Mesh.ARRAY_VERTEX];var ns=arrays[Mesh.ARRAY_NORMAL];var ids=arrays[Mesh.ARRAY_INDEX]
            var mat=n.material_override if n.material_override else n.mesh.surface_get_material(surface)
            var pigment=Color("7a8974");var water=false;var outlined=false
            if mat is ShaderMaterial:
                water=mat.shader.resource_path.ends_with("water.gdshader")
                var color=mat.get_shader_parameter("pigment")
                if color is Color:pigment=color
                outlined=mat.next_pass!=null
            elif mat is StandardMaterial3D:pigment=mat.albedo_color
            put_color(pigment);file.store_8(1 if water else 0);file.store_8(1 if outlined else 0)
            file.store_32(vs.size());vertices+=vs.size()
            for v in vs:put_v3(v)
            for i in range(vs.size()):put_v3(ns[i] if ns!=null and i<ns.size() else Vector3.UP)
            var indices=ids if ids!=null and ids.size()>0 else range(vs.size())
            file.store_32(indices.size())
            for id in indices:file.store_32(id)
            mesh_count+=1
        if n is Label3D:
            file.store_8(1);put_string(n.text);file.store_32(n.font_size);file.store_float(n.pixel_size);put_color(n.modulate)
        else:file.store_8(0)
    file.close()
    var metadata={"version":1,"source":"Godot procedural world.gd","nodeCount":nodes.size(),"meshCount":mesh_count,"vertices":vertices}
    for key in ["player","friend","customer","cargo_boat","row_boat","oar","pear_display","sun","leaf_parent","targets","walkers","lanterns","boats","actors","night_lights","willows","banners","wind_lanterns","drifting_leaves"]:
        metadata[key]=encode_metadata(world.get(key))
    FileAccess.open(out_dir+"/WorldMetadata.json",FileAccess.WRITE).store_string(JSON.stringify(metadata,"\t"))
    print("UNITY_EXPORT_OK nodes=",nodes.size()," meshes=",mesh_count," vertices=",vertices)
    quit()