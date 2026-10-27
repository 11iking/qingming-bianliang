shader_type spatial;
render_mode cull_disabled,unshaded;
varying vec3 world_pos;
void vertex(){world_pos=(MODEL_MATRIX*vec4(VERTEX,1.0)).xyz;}
void fragment(){
 float ripple=sin(world_pos.z*8.0+sin(world_pos.x*.5+TIME*.24)*.6);
 float broken=step(.45,sin(world_pos.x*1.8+world_pos.z*.7+TIME*.2));
 float line=smoothstep(.975,.995,ripple)*broken;
 ALBEDO=mix(vec3(.38,.43,.36),vec3(.64,.64,.51),line*.55);
 ROUGHNESS=1.0;
 SPECULAR=0.0;
}
