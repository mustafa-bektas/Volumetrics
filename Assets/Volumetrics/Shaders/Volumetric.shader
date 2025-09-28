Shader "Custom/Volumetric"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _FogDensity ("Fog Density", Float) = 0.01
        _FogColor ("Fog Color", Color) = (0.5, 0.5, 0.5, 1)
        _ShowFogOnly ("Show Fog Only", Int) = 0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float3 rayDir : TEXCOORD2;
            };
            
            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;
            float _FogDensity;
            float4 _FogColor;
            int _ShowFogOnly;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                
                // calculate world position and ray direction
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldPos = worldPos;
                o.rayDir = worldPos - _WorldSpaceCameraPos;
                
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Get scene color
                fixed4 sceneColor = tex2D(_MainTex, i.uv);
                
                // Get depth and calculate world distance
                float depth = tex2D(_CameraDepthTexture, i.uv).r;
                float linearDepth = LinearEyeDepth(depth);
                
                // calculate fog amount using beers law
                // fog = 1 - e^(-density * distance)
                float fogAmount = 1.0 - exp(-_FogDensity * linearDepth);
                
                // debug: show fog density only
                if (_ShowFogOnly == 1)
                {
                    return fixed4(fogAmount, fogAmount, fogAmount, 1);
                }
                
                // blend scene with fog
                fixed4 finalColor = lerp(sceneColor, _FogColor, fogAmount);
                return finalColor;
            }
            ENDCG
        }
    }
}