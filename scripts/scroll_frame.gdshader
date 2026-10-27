shader_type canvas_item;
render_mode unshaded;
uniform float unfold : hint_range(0.0,1.0) = 1.0;
uniform float top_edge = 92.0;
uniform float bottom_edge = 816.0;
uniform vec4 paper : source_color = vec4(0.91,0.88,0.80,1.0);
float grain(vec2 p) { return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453); }
void fragment() {
    vec2 p=UV*vec2(1440.0,900.0);
    float l=mix(1388.0,42.0,unfold);
    float r=1398.0;
    float n=grain(floor(p))-.5;
    vec3 silk=paper.rgb+vec3(n*.025+sin(p.y*2.5)*.006);
    vec4 c=vec4(silk,1.0);
    bool opening=p.x>l+12.0 && p.x<r-12.0 && p.y>top_edge+12.0 && p.y<bottom_edge-12.0;
    if(opening) {
        float edge=min(min(p.x-l-12.0,r-12.0-p.x),min(p.y-top_edge-12.0,bottom_edge-12.0-p.y));
        c=vec4(vec3(.36,.29,.19),.22*(1.0-smoothstep(0.0,9.0,edge)));
    }
    bool mounting=p.x>l && p.x<r && p.y>top_edge && p.y<bottom_edge && !opening;
    if(mounting) {
        c=vec4(vec3(.68,.62,.47)+n*.018+sin(p.x*2.0)*sin(p.y*2.0)*.012,1.0);
        float rule=min(p.y-top_edge,bottom_edge-p.y);
        if(rule<1.2 || abs(rule-9.0)<.6) c.rgb=vec3(.44,.38,.27);
    }
    float rod_x=min(abs(p.x-l),abs(p.x-r));
    if(p.y>top_edge-9.0 && p.y<bottom_edge+9.0 && rod_x<8.0) {
        float light=pow(1.0-rod_x/8.0,1.4);
        c=vec4(mix(vec3(.25,.20,.14),vec3(.59,.46,.29),light)+n*.03,1.0);
        if(p.y<top_edge || p.y>bottom_edge) c.rgb=vec3(.20,.19,.15)+light*.06;
    }
    COLOR=c;
}
