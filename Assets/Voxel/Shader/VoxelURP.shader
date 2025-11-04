Shader "Universal Render Pipeline/Voxel/Standard"
{
    Properties
    {
        [Header(Surface Options)]
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _AlphaClip("Alpha Clip", Range(0.0, 1.0)) = 0.5
        
        [Header(Surface Inputs)]
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.1
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _SpecularColor("Specular Color", Color) = (0.2, 0.2, 0.2, 1.0)
        _SpecularPower("Specular Power", Range(1.0, 128.0)) = 32.0
        
        [Header(Advanced)]
        [Toggle] _ReceiveShadows("Receive Shadows", Float) = 1.0
        [Toggle] _CastShadows("Cast Shadows", Float) = 1.0
        
        [Space]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 1.0
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 0.0
        [Enum(Off, 0, On, 1)] _ZWrite("Z Write", Float) = 1.0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 2.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        
        LOD 300
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]
            
            HLSLPROGRAM
            #pragma target 2.0
            
            // キーワード
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local _CAST_SHADOWS_OFF
            
            // URP キーワード
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _CLUSTERED_RENDERING
            
            // Unity キーワード
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 voxelColor : TEXCOORD2;
                float2 uv : TEXCOORD3;
                
                #ifdef REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
                float4 shadowCoord : TEXCOORD4;
                #endif
                
                float3 vertexSH : TEXCOORD5;
                
                float fogCoord : TEXCOORD7;
                
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _AlphaClip;
                float _Smoothness;
                float _Metallic;
                float4 _SpecularColor;
                float _SpecularPower;
                float _ReceiveShadows;
                float _CastShadows;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.voxelColor = input.color; // 頂点カラー（VoxelDataのColor）
                output.uv = input.uv;

                // 環境光（Spherical Harmonics）を正しく計算
                OUTPUT_SH(normalInput.normalWS, output.vertexSH);
                
                #ifdef REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
                output.shadowCoord = GetShadowCoord(vertexInput);
                #endif
                
                output.fogCoord = ComputeFogFactor(output.positionCS.z);
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                // 基本色計算（ベースカラー × ボクセルカラー）
                float4 albedo = _BaseColor * input.voxelColor;
                
                // アルファテスト
                #ifdef _ALPHATEST_ON
                clip(albedo.a - _AlphaClip);
                #endif
                
                // サーフェイス情報
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(_WorldSpaceCameraPos - input.positionWS);
                
                // シャドウ座標
                #ifdef REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
                float4 shadowCoord = input.shadowCoord;
                #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #else
                float4 shadowCoord = float4(0, 0, 0, 0);
                #endif
                
                // グローバルイルミネーション（環境光）を正しく計算
                float3 bakedGI = SAMPLE_GI(float2(0,0), input.vertexSH, normalWS);

                // メインライト
                Light mainLight = GetMainLight(shadowCoord);
                float shadowAttenuation = _ReceiveShadows ? mainLight.shadowAttenuation : 1.0;
                
                // Lambert拡散反射
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float3 diffuse = albedo.rgb * mainLight.color * NdotL * shadowAttenuation;
                
                // Blinn-Phongスペキュラー
                float3 halfVector = normalize(mainLight.direction + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfVector));
                float specularFactor = pow(NdotH, _SpecularPower) * _Metallic;
                float3 specular = _SpecularColor.rgb * mainLight.color * specularFactor * shadowAttenuation;
                
                // 追加ライト
                #ifdef _ADDITIONAL_LIGHTS
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
                {
                    Light light = GetAdditionalLight(lightIndex, input.positionWS, shadowCoord);
                    float lightNdotL = saturate(dot(normalWS, light.direction));
                    float lightAttenuation = light.distanceAttenuation * light.shadowAttenuation;
                    diffuse += albedo.rgb * light.color * lightNdotL * lightAttenuation;

                    float3 lightHalfVector = normalize(light.direction + viewDirWS);
                    float lightNdotH = saturate(dot(normalWS, lightHalfVector));
                    float lightSpecularFactor = pow(lightNdotH, _SpecularPower) * _Metallic;
                    specular += _SpecularColor.rgb * light.color * lightSpecularFactor * lightAttenuation;
                }
                #endif

                // アンビエント（GIが正しく計算されているので係数を下げる）
                float3 ambient = bakedGI * albedo.rgb;
                
                // 最終色計算
                float3 color = ambient + diffuse + specular;
                
                // フォグ適用
                color = MixFog(color, input.fogCoord);
                
                return float4(color, albedo.a);
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]
            
            HLSLPROGRAM
            #pragma target 2.0
            
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            
            #pragma multi_compile_instancing
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct AttributesShadow
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct VaryingsShadow
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            VaryingsShadow ShadowPassVertex(AttributesShadow input)
            {
                VaryingsShadow output = (VaryingsShadow)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }
            
            float4 ShadowPassFragment(VaryingsShadow input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            
            ZWrite On
            ColorMask 0
            Cull[_Cull]
            
            HLSLPROGRAM
            #pragma target 2.0
            
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_instancing
            
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct AttributesDepth
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct VaryingsDepth
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            VaryingsDepth DepthOnlyVertex(AttributesDepth input)
            {
                VaryingsDepth output = (VaryingsDepth)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }
            
            float4 DepthOnlyFragment(VaryingsDepth input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    
    // CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.LitShader"
    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}