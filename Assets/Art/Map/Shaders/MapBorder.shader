// 지도 국경선(LineRenderer)용 셰이더.
//
// - 색은 정점 색(LineRenderer 의 startColor / endColor)을 그대로 쓴다. (조명 없음)
// - 깊이 당김: 정점을 카메라 쪽으로 "카메라까지 거리 × _DepthPull" 만큼 시선 방향을 따라 끌어당긴다.
//   같은 시선 위의 점은 화면에 같은 위치로 찍히므로 선의 화면상 모양은 그대로이고, 깊이만 앞당겨진다.
//   덕분에 지면 바로 위에 그린 선이 언덕·산 비탈에 묻히지 않는다. 당기는 양이 거리에 비례하므로
//   가까이서 볼 때나 수십 km 밖에서 볼 때나 같은 비율로 안전하게 보인다.
Shader "VictoriaFantasia/MapBorder"
{
    Properties
    {
        // 카메라까지 거리 중 얼마만큼 카메라 쪽으로 당길지 (0.01 = 1%)
        _DepthPull ("Depth Pull (fraction of camera distance)", Range(0, 0.1)) = 0.01
    }

    SubShader
    {
        // 지형·바다(불투명)를 다 그린 뒤 위에 덧그린다.
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _DepthPull;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float4 color : COLOR;
            };

            v2f vert(appdata input)
            {
                // 월드 좌표에서 카메라 쪽으로 시선 방향을 따라 당긴다.
                float3 worldPosition = mul(unity_ObjectToWorld, input.vertex).xyz;
                worldPosition += (_WorldSpaceCameraPos - worldPosition) * _DepthPull;

                v2f output;
                output.position = mul(UNITY_MATRIX_VP, float4(worldPosition, 1.0));
                output.color = input.color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                return input.color;
            }
            ENDCG
        }
    }
}
