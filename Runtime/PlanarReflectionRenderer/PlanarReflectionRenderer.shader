Shader "JayT/URPHelper/PlanarReflectionFloor"
{
    Properties
    {
        _MainTex ("Floor Texture", 2D) = "white" {}
        _ReflectionTex ("Reflection Texture", 2D) = "black" {}
        _FresnelPower ("Fresnel Power", Range(0.1, 10.0)) = 2.0
        _ReflectionStrength ("Reflection Strength", Range(0.0, 1.0)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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
                float3 normalWS    : TEXCOORD2;
                float3 viewDirWS   : TEXCOORD3;
                float  fogFactor   : TEXCOORD4;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_ReflectionTex);
            SAMPLER(sampler_ReflectionTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float  _FresnelPower;
                float  _ReflectionStrength;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = posInputs.positionCS;
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.screenPos   = ComputeScreenPos(posInputs.positionCS);
                OUT.normalWS    = normalInputs.normalWS;
                OUT.viewDirWS   = GetWorldSpaceViewDir(posInputs.positionWS);
                OUT.fogFactor   = ComputeFogFactor(posInputs.positionCS.z);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 床テクスチャ
                half4 floorColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                // スクリーンスペースUVで反射テクスチャをサンプリング
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                // Y軸反転
                screenUV.y = 1.0 - screenUV.y;
                half4 reflectionColor = SAMPLE_TEXTURE2D(_ReflectionTex, sampler_ReflectionTex, screenUV);

                // フレネル
                float3 normalWS  = normalize(IN.normalWS);
                float3 viewDirWS = normalize(IN.viewDirWS);
                float  fresnel   = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelPower);
                fresnel *= _ReflectionStrength;

                // 合成
                half4 finalColor = lerp(floorColor, reflectionColor, fresnel);
                finalColor.rgb = MixFog(finalColor.rgb, IN.fogFactor);

                return finalColor;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}