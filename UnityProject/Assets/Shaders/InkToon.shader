Shader "Qingming/InkToon"
{
    Properties {
        _Color("Pigment",Color)=(0.7,0.6,0.4,1)
        _Glow("Glow",Range(0,1))=0
        _Outline("Outline pixels",Range(0,2))=1.1
        _SurfaceDetail("Surface wear (0 none, 1 stone, 2 wood)",Range(0,2))=0
    }
    SubShader {
        Tags {"RenderType"="Opaque"}
        Pass {
            Cull Front
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _Outline;
            float4 vert(appdata_base v):SV_POSITION {
                float4 p=UnityObjectToClipPos(v.vertex);
                float3 n=mul((float3x3)UNITY_MATRIX_IT_MV,v.normal);
                float2 d=mul((float3x3)UNITY_MATRIX_P,n).xy;
                p.xy+=d/max(length(d),.0001)*_Outline*2/_ScreenParams.xy*p.w;
                return p;
            }
            fixed4 frag():SV_Target{return fixed4(.18,.17,.13,1);}
            ENDCG
        }
        Pass {
            Tags {"LightMode"="ForwardBase"}
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            fixed4 _Color;float _Glow,_SurfaceDetail;
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p){float2 cell=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(cell),hash(cell+float2(1,0)),f.x),lerp(hash(cell+float2(0,1)),hash(cell+1),f.x),f.y);}
            struct V {float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 world:TEXCOORD1;SHADOW_COORDS(2) float3 fill:TEXCOORD3;};
            V vert(appdata_base v) {
                V o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);
                o.world=mul(unity_ObjectToWorld,v.vertex).xyz;
                o.fill=0;
                #ifdef VERTEXLIGHT_ON
                o.fill=Shade4PointLights(unity_4LightPosX0,unity_4LightPosY0,unity_4LightPosZ0,
                    unity_LightColor[0].rgb,unity_LightColor[1].rgb,unity_LightColor[2].rgb,unity_LightColor[3].rgb,
                    unity_4LightAtten0,o.world,normalize(o.normal));
                #endif
                TRANSFER_SHADOW(o);return o;
            }
            fixed4 frag(V i):SV_Target {
                float d=dot(normalize(i.normal),normalize(_WorldSpaceLightPos0.xyz));
                float shade=d>.64?1:d>.18?.63:.24;
                float shadow=lerp(.36,1,step(.5,SHADOW_ATTENUATION(i)));
                float grain=frac(sin(dot(floor(i.world*95),float3(12.9898,78.233,43.156)))*43758.5453);
                float3 illumination=UNITY_LIGHTMODEL_AMBIENT.rgb+_LightColor0.rgb*shade*shadow+i.fill;
                float wear=1;
                if(_SurfaceDetail>.5){
                    float mottling=noise(i.world.xz*5.5)+noise((i.world.xz+i.world.y)*24)*.35;
                    wear=.91+mottling*.13;
                    if(_SurfaceDetail>1.5)wear*=.96+.05*sin(i.world.x*95+noise(i.world.zy*4)*3);
                }
                return fixed4(_Color.rgb*(.98+grain*.045)*wear*(illumination+_Glow),1);
            }
            ENDCG
        }
        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
    Fallback "Diffuse"
}
