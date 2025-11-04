Shader "VoxelWorld/SurfacePlane"
{
    Properties
    {
        _Color ("Color", Color) = (0.6, 0.4, 0.2, 1)
        _MaxYHeight ("Max Y Height", Float) = 0.0
        _VoxelDataTex ("Voxel Data Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            TEXTURE2D(_VoxelDataTex);
            SAMPLER(sampler_VoxelDataTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _MaxYHeight;
                float4 _VoxelDataTex_ST;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _VoxelDataTex);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Y軸制限：指定高さ以上はピクセルを破棄
                if (input.positionWS.y > _MaxYHeight)
                {
                    discard;
                }

                // ボクセルデータテクスチャをサンプリング
                // 黒（0.0）= ボクセル有り（表示）
                // 白（1.0）= 空気（非表示）
                half voxelData = SAMPLE_TEXTURE2D(_VoxelDataTex, sampler_VoxelDataTex, input.uv).r;

                // 空気の部分は破棄（非表示）
                if (voxelData > 0.5)
                {
                    discard;
                }

                // ライティング無効化（常に一定の色で表示）
                return half4(_Color.rgb, _Color.a);
            }
            ENDHLSL
        }

        // ShadowCasterパスを意図的に省略（影を落とさない）
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
