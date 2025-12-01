Shader "Custom/DepthFOVUnlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)

        [Header(Depth FOV)]
        _FarFOV ("Far FOV (degrees)", Float) = 60
        _FarDistance ("Far Distance (meters)", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
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
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;

            float _FarFOV;
            float _FarDistance;

            v2f vert(appdata v)
            {
                v2f o;

                // オブジェクト空間 → ビュー空間（カメラ基準）
                float3 viewPos = UnityObjectToViewPos(v.vertex);
                float depth = -viewPos.z; // カメラからの距離（正の値）

                // オブジェクト空間 → クリップ空間
                float4 clipPos = UnityObjectToClipPos(v.vertex);

                // NDC座標に変換 (-1 to 1)
                float2 ndc = clipPos.xy / clipPos.w;

                // 深度ベースFOV変換
                // カメラのFOVをNearFOVとして使用
                float cameraScale = unity_CameraProjection[1][1]; // 1/tan(fov/2)
                float nearScale = 1.0 / cameraScale; // tan(cameraFov/2)
                float farScale = tan(radians(_FarFOV * 0.5));

                // 非線形補間: depth=0 → t=0, depth=farDist → t=0.5, depth=∞ → t=1
                float t = depth / (depth + _FarDistance);
                float currentScale = lerp(nearScale, farScale, t);

                // 元のカメラFOVに対する比率でスケール
                // 逆数にして小さいFOV=大きく映る（望遠効果）
                float scaleFactor = nearScale / currentScale;

                clipPos.xy = ndc * scaleFactor * clipPos.w;

                o.vertex = clipPos;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                return col;
            }
            ENDCG
        }
    }
}
