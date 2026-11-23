Shader "ATM/Patrol Laser Orb"
{
    Properties { [PerRendererData] _MainTex ("Sprite", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off Lighting Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 frag(v2f_img i) : SV_Target
            {
                float r = 2 * length(i.uv - .5);
                float edge = max(fwidth(r), .015);
                float core = 1 - smoothstep(.39 - edge, .39 + edge, r);
                float rim = 1 - smoothstep(.66 - edge, .66 + edge, r);
                float halo = .55 * (1 - smoothstep(.64, 1, r));
                return fixed4(lerp(fixed3(1,.018,.042), fixed3(1,.97,.95), core), max(rim, halo));
            }
            ENDCG
        }
    }
}
