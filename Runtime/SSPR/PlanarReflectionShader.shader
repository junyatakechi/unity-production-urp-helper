// 床用シェーダー
// LightMode = "PlanarReflection" で描画される

Shader "JayT/PlanarReflectionFloor"
{
    Properties
    {
        [MainColor]   _BaseColor     ("Base Color",  Color)   = (1,1,1,1)
        [MainTexture] _BaseMap       ("Base Map",    2D)      = "white" {}
        _Roughness    ("Roughness",  Range(0,1))              = 0.1
        _ReflectionStrength ("Reflection Strength", Range(0,1)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ---- Reflection RT (RendererFeature が SetGlobalTexture で渡す) ----
            TEXTURE2D(_PlanarReflection_ColorRT);

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                half   _Roughness;
                half   _ReflectionStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 screenPos   : TEXCOORD1;
                float3 posWS       : TEXCOORD2;
                float3 normalWS    : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.screenPos   = ComputeScreenPos(OUT.positionHCS);
                OUT.posWS       = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // ---- ベースカラー ----
                half4 base = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                // ---- 反射 ----
                half2 screenUV = IN.screenPos.xy / IN.screenPos.w;

                half4 reflectionSample = SAMPLE_TEXTURE2D(_PlanarReflection_ColorRT, sampler_LinearClamp, screenUV);

                // ---- Fresnel（反射強度を視線角度で調整） ----
                float3 viewWS   = normalize(_WorldSpaceCameraPos - IN.posWS);
                half   fresnel  = pow(1.0h - saturate(dot(viewWS, IN.normalWS)), 4.0h);
                half   reflBlend = reflectionSample.a * _ReflectionStrength * (0.2h + 0.8h * fresnel);

                // ---- フォールバック: リフレクションプローブ ----
                float3 reflectDirWS    = reflect(-viewWS, IN.normalWS) * float3(1, -1, 1);
                half3  probeReflection = GlossyEnvironmentReflection(half3(reflectDirWS), IN.posWS, _Roughness, 1.0h);

                // ---- 合成: Probe → SSPR の順で lerp ----
                half3 reflection = lerp(probeReflection, reflectionSample.rgb, reflectionSample.a);
                half3 finalColor = lerp(base.rgb, reflection, reflBlend);

                return half4(finalColor, base.a);
            }
            ENDHLSL
        }
    }
}
