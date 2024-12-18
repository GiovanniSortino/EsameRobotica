Shader "Custom/ThermalVision"
{
    Properties
    {
        _HeatLevel ("Heat Level", Range(0, 1)) = 0.5 // Valore di calore
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            float _HeatLevel; // Livello di calore da 0 a 1

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Gradiente termico: da blu (freddo) a rosso (caldo)
                fixed3 cold = fixed3(0.0, 0.0, 1.0); // Blu
                fixed3 medium = fixed3(0.0, 1.0, 0.0); // Verde
                fixed3 warm = fixed3(1.0, 1.0, 0.0); // Giallo
                fixed3 hot = fixed3(1.0, 0.0, 0.0); // Rosso

                fixed3 color;

                // Interpolazione graduale tra colori in base al valore di _HeatLevel
                if (_HeatLevel < 0.33)
                {
                    color = lerp(cold, medium, _HeatLevel / 0.33);
                }
                else if (_HeatLevel < 0.66)
                {
                    color = lerp(medium, warm, (_HeatLevel - 0.33) / 0.33);
                }
                else
                {
                    color = lerp(warm, hot, (_HeatLevel - 0.66) / 0.34);
                }

                return fixed4(color, 1.0);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
