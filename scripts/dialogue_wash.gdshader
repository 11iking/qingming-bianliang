shader_type canvas_item;
render_mode unshaded;
void fragment() {
    float side=smoothstep(0.0,.08,UV.x)*smoothstep(0.0,.08,1.0-UV.x);
    float ink=fract(sin(dot(floor(UV*vec2(1332.0,220.0)),vec2(13.1,87.7)))*43758.54);
    COLOR=vec4(vec3(.947,.919,.850)+(ink-.5)*.015,side*smoothstep(0.0,.3,UV.y)*.97);
}
