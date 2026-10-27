Shader "Qingming/WorldText"
{
    Properties { _MainTex("Font Atlas",2D)="white" {} _Color("Tint",Color)=(1,1,1,1) }
    SubShader {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        Lighting Off ZWrite Off ZTest LEqual Cull Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;fixed4 _Color;
            struct V {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            V vert(appdata_full v){V o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord.xy;o.color=v.color*_Color;return o;}
            fixed4 frag(V i):SV_Target {fixed4 c=i.color;c.a*=tex2D(_MainTex,i.uv).a;return c;}
            ENDCG
        }
    }
}
