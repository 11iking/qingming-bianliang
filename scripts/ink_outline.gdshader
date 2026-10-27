shader_type spatial;
render_mode unshaded, cull_front, shadows_disabled;

uniform vec4 ink : source_color = vec4(0.18,0.17,0.13,1.0);
uniform float width_pixels : hint_range(0.0,4.0) = 1.1;

void vertex() {
 vec4 clip=PROJECTION_MATRIX*MODELVIEW_MATRIX*vec4(VERTEX,1.0);
 vec3 n=normalize(MODELVIEW_NORMAL_MATRIX*NORMAL);
 vec2 direction=(PROJECTION_MATRIX*vec4(n,0.0)).xy;
 direction=direction/max(length(direction),0.0001);
 clip.xy+=direction*width_pixels*2.0/VIEWPORT_SIZE*clip.w;
 POSITION=clip;
}

void fragment() {
 ALBEDO=ink.rgb;
}
