Shader "UI/PieChart"
{
    Properties
    {
        // 各セグメントの開始・終了割合
        _Segment0 ("Segment 0 (start, end, _, _)", Vector) = (0, 0, 0, 0)
        _Segment1 ("Segment 1 (start, end, _, _)", Vector) = (0, 0, 0, 0)
        _Segment2 ("Segment 2 (start, end, _, _)", Vector) = (0, 0, 0, 0)
        _Segment3 ("Segment 3 (start, end, _, _)", Vector) = (0, 0, 0, 0)
        
        _Color0 ("Color 0", Color) = (1, 0, 0, 1)
        _Color1 ("Color 1", Color) = (0, 1, 0, 1)
        _Color2 ("Color 2", Color) = (0, 0, 1, 1)
        _Color3 ("Color 3", Color) = (1, 1, 0, 1)
        _BackgroundColor ("Background", Color) = (0.2, 0.2, 0.2, 1)
        
        // UI用の必須プロパティ
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            #define PI 3.14159265

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            float4 _Segment0, _Segment1, _Segment2, _Segment3;
            float4 _Color0, _Color1, _Color2, _Color3;
            float4 _BackgroundColor;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 center = i.uv - 0.5;
                float dist = length(center);
    
                // 完全に外側は透明
                if (dist > 0.5)
                {
                    return float4(0, 0, 0, 0);
                }
    
                // 枠線の太さ（0.0〜0.5の範囲、値が大きいほど太い）
                float borderWidth = 0.03;
                float innerRadius = 0.5 - borderWidth;
    
                // 外周の枠線部分は背景色
                if (dist > innerRadius)
                {
                    return _BackgroundColor * i.color;
                }
    
                // 角度を0〜1に正規化（12時方向が0、時計回り）
                float angle = atan2(-center.x, -center.y);
                angle = angle / (2.0 * PI) + 0.5;
    
                // 各セグメントの判定
                float4 segments[4] = { _Segment0, _Segment1, _Segment2, _Segment3 };
                float4 colors[4] = { _Color0, _Color1, _Color2, _Color3 };
    
                for (int idx = 3; idx >= 0; idx--)
                {
                    float start = segments[idx].x;
                    float end = segments[idx].y;
        
                    if (start < end && angle >= start && angle < end)
                    {
                        return colors[idx] * i.color;
                    }
                }
                return _BackgroundColor * i.color;
            }
            ENDCG
        }
    }
}