Shader "JayT/URPHelper/PlanarReflectionFloor"
{
    Properties
    {
        _MainTex            ("Floor Texture", 2D)               = "white" {}
        _BaseColor          ("Base Color", Color)                = (1,1,1,1)
        _BumpMap            ("Normal Map", 2D)                   = "bump" {}
        _BumpScale          ("Normal Scale", Range(0, 2))        = 1.0
        _ReflectionTex      ("Reflection Texture", 2D)          = "black" {}
        _FresnelPower       ("Fresnel Power", Range(0.1, 10.0))  = 2.0
        _ReflectionStrength ("Reflection Strength", Range(0.0, 1.0)) = 1.0
        _BlurSize           ("Blur Size", Range(0.0, 0.02))      = 0.005
        [IntRange] _BlurSamples ("Blur Samples", Range(4, 8))   = 4
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry"
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
                float4 tangentOS  : TANGENT;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 screenPos   : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float3 viewDirWS   : TEXCOORD3;
                float3 tangentWS   : TEXCOORD4;
                float3 bitangentWS : TEXCOORD5;
                float  fogFactor   : TEXCOORD6;
            };

            TEXTURE2D(_MainTex);       SAMPLER(sampler_MainTex);
            TEXTURE2D(_BumpMap);       SAMPLER(sampler_BumpMap);
            TEXTURE2D(_ReflectionTex); SAMPLER(sampler_ReflectionTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BumpMap_ST;
                float4 _BaseColor;
                float  _BumpScale;
                float  _FresnelPower;
                float  _ReflectionStrength;
                float  _BlurSize;
                int    _BlurSamples;
            CBUFFER_END

            half4 SampleReflectionBlur(float2 uv, float blurSize, int samples)
            {
                half4 color = 0;
                float step = blurSize / max(samples - 1, 1);
                float offset = blurSize * 0.5;

                for (int x = 0; x < samples; x++)
                {
                    for (int y = 0; y < samples; y++)
                    {
                        float2 uvOffset = uv + float2(
                            -offset + x * step,
                            -offset + y * step
                        );
                        color += SAMPLE_TEXTURE2D(_ReflectionTex, sampler_ReflectionTex, uvOffset);
                    }
                }

                return color / (samples * samples);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs posInputs    = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   normalInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionHCS = posInputs.positionCS;
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.screenPos   = ComputeScreenPos(posInputs.positionCS);
                OUT.normalWS    = normalInputs.normalWS;
                OUT.tangentWS   = normalInputs.tangentWS;
                OUT.bitangentWS = normalInputs.bitangentWS;
                OUT.viewDirWS   = GetWorldSpaceViewDir(posInputs.positionWS);
                OUT.fogFactor   = ComputeFogFactor(posInputs.positionCS.z);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 床テクスチャ
                half4 floorColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                // ノーマルマップ
                float3 normalTS = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, TRANSFORM_TEX(IN.uv, _BumpMap)),
                    _BumpScale
                );
                float3x3 TBN = float3x3(
                    normalize(IN.tangentWS),
                    normalize(IN.bitangentWS),
                    normalize(IN.normalWS)
                );
                float3 normalWS = normalize(mul(normalTS, TBN));

                // スクリーンスペースUV
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                screenUV.y = 1.0 - screenUV.y;

                // ブラー付き反射サンプリング
                half4 reflectionColor = SampleReflectionBlur(screenUV, _BlurSize, _BlurSamples);

                // フレネル
                float3 viewDirWS = normalize(IN.viewDirWS);
                float  fresnel   = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelPower);
                fresnel *= _ReflectionStrength;

                // 合成
                half4 finalColor = lerp(floorColor, reflectionColor, fresnel);

                // 最終的な明るさ調整
                finalColor *= _BaseColor;

                finalColor.rgb = MixFog(finalColor.rgb, IN.fogFactor);

                return finalColor;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}