shader_type canvas_item;
void fragment(){
 vec2 uv=UV;
 float noise=fract(sin(dot(floor(uv*vec2(1440.0,900.0)),vec2(12.9898,78.233)))*43758.5453);
 float edge=pow(abs(uv.x-.5)*1.6,3.0)+pow(abs(uv.y-.5)*1.7,3.0);
 COLOR=vec4(.24,.22,.16,.027*noise+.14*edge);
}
