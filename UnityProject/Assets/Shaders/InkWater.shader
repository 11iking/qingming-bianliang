Shader "Qingming/InkWater" {
    SubShader {
        Tags {"RenderType"="Opaque"}
        Pass {
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct V{float4 pos:SV_POSITION;float3 world:TEXCOORD0;};
            V vert(appdata_base v){V o;o.pos=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
            fixed4 frag(V i):SV_Target{
                float z=-i.world.z,t=_Time.y;
                float ripple=sin(z*8+sin(i.world.x*.5+t*.24)*.6);
                float broken=step(.45,sin(i.world.x*1.8+z*.7+t*.2));
                float rippleLine=smoothstep(.975,.995,ripple)*broken;
                return fixed4(lerp(float3(.38,.43,.36),float3(.64,.64,.51),rippleLine*.55),1);
            }
            ENDCG
        }
    }
}
