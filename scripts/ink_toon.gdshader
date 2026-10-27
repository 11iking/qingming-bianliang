shader_type spatial;
render_mode specular_disabled;

// Three deliberately separated ink-wash values; no plastic specular highlights.
uniform vec4 pigment : source_color = vec4(0.7,0.6,0.4,1.0);
uniform float glow : hint_range(0.0,1.0) = 0.0;
varying vec3 paper_position;

float paper_hash(vec3 p) {
 return fract(sin(dot(p,vec3(12.9898,78.233,43.156)))*43758.5453);
}

void vertex() {
 paper_position = (MODEL_MATRIX * vec4(VERTEX,1.0)).xyz;
}

void fragment() {
 float fibre=paper_hash(floor(paper_position*95.0));
 ALBEDO=pigment.rgb*(0.98+fibre*0.045);
 ROUGHNESS=1.0;
 SPECULAR=0.0;
 EMISSION=pigment.rgb*glow;
}

void light() {
 float ndl=dot(normalize(NORMAL),normalize(LIGHT));
 float shade=0.24;
 if(ndl>0.18) {shade=0.63;}
 if(ndl>0.64) {shade=1.0;}
 float cast_shadow=mix(0.36,1.0,step(0.5,ATTENUATION));
 DIFFUSE_LIGHT+=ALBEDO*LIGHT_COLOR*(shade*cast_shadow)/3.14159265;
}
