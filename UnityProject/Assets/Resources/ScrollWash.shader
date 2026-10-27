Shader "Qingming/ScrollWash"
{
    Properties { _MainTex("Scene",2D)="white" {} }
    SubShader {
        Cull Off ZWrite Off ZTest Always
        Pass {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Pigment,_Night;
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            fixed4 frag(v2f_img i):SV_Target
            {
                float3 source=tex2D(_MainTex,i.uv).rgb;
                float luma=dot(source,float3(.299,.587,.114));
                // Warm ink and aged silk bound the entire scene's tonal range.
                float3 ink=lerp(float3(.17,.15,.12),float3(.105,.108,.105),_Night);
                float3 silk=lerp(float3(.79,.685,.505),float3(.74,.615,.425),_Night);
                float dayTone=pow(saturate((luma-.12)/.73),1.1);
                float tone=lerp(dayTone,pow(saturate(luma),.82),_Night);
                float3 color=lerp(ink,silk,tone);
                // Only the entry transition is tinted; its endpoint is the original scene.
                color+=(source-luma)*lerp(.04,.34,_Pigment);
                float2 pixel=floor(i.uv*_ScreenParams.xy);
                float grain=(hash(pixel)-.5)*.035;
                float weave=(sin(pixel.x*3.14159)+sin(pixel.y*2.0944))*.003;
                float2 p=i.uv-.5;
                color+=grain+weave-dot(p,p)*.025;
                return fixed4(lerp(saturate(color),source,saturate(_Pigment)),1);
            }
            ENDCG
        }
    }
}
