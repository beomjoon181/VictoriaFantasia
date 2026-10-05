using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 대륙 지도 전체의 데이터 에셋.
/// 국가별 영역 외곽선(지도 좌표), 영역별 지형, 산맥, 높낮이 규칙, 국경선 표시 방식을 보관한다.
/// 실제 오브젝트 생성은 WorldMapGenerator 가 담당하며, 이 클래스는 데이터와 좌표 변환만 가진다. (단일 책임)
///
/// 좌표계:
/// - 지도 좌표 : 참고 지도를 옮긴 2D 좌표 (x 오른쪽 = 동쪽, y 위쪽 = 북쪽, 1 = 지도상 100px)
/// - 월드 좌표 : 지도 좌표에 WorldScale 을 곱해 XZ 평면에 눕힌 3D 좌표 (단위 m, Y = 높이)
/// </summary>
[CreateAssetMenu(menuName = "VictoriaFantasia/Map/World Map Definition", fileName = "WorldMapDefinition")]
public class WorldMapDefinition : ScriptableObject
{
    /// <summary>
    /// 국가 하나가 차지하는 지도 영역.
    /// 이웃 국가와 맞닿는 국경은 양쪽 외곽선에 같은 꼭짓점을 넣어야 빈틈 없이 맞물린다.
    /// </summary>
    [Serializable]
    public class NationRegion
    {
        [Tooltip("이 영역을 소유한 국가")]
        public CountryDefinition country;

        [Tooltip("이 영역에 입힐 지형")]
        public TerrainDefinition terrain;

        [Tooltip("영역 외곽선 꼭짓점 (지도 좌표, 시계·반시계 방향 무관)")]
        public Vector2[] outline = Array.Empty<Vector2>();
    }

    /// <summary>
    /// 능선을 따라 솟아오르는 산맥.
    /// </summary>
    [Serializable]
    public class MountainRange
    {
        [Tooltip("표시용 이름")]
        public string name;

        [Tooltip("능선 꼭짓점 (지도 좌표). 이 선에서 멀어질수록 낮아진다.")]
        public Vector2[] ridge = Array.Empty<Vector2>();

        [Tooltip("능선에서 산기슭까지의 거리 (지도 좌표 단위)")]
        public float halfWidth = 0.35f;

        [Tooltip("가장 높은 봉우리 높이 (m, 평지 기준 추가 높이)")]
        public float peakHeight = 110f;

        [Tooltip("봉우리가 촘촘한 정도 (클수록 작은 봉우리가 많아진다)")]
        public float peakFrequency = 4f;
    }

    /// <summary>
    /// 지형 높낮이(해안·평지·바다) 규칙.
    /// </summary>
    [Serializable]
    public class ReliefSettings
    {
        [Tooltip("Terrain 의 최대 높이 (m). 모든 높이는 이 값 이하여야 한다.")]
        public float maxHeight = 200f;

        [Tooltip("바다 밑바닥 높이 (m, 지형에서 가장 낮은 높이). " +
                 "깊게 잡아야 멀리서 지형 LOD 가 폴리곤을 줄일 때 해안 근처 바다 밑바닥이 수면 위로 튀어나오지 않는다. " +
                 "(높이 차가 작으면 Unity 가 화면상 오차가 없다고 보고 해안을 크게 단순화한다)")]
        public float seaFloor = -300f;

        [Tooltip("해수면 높이 (m). 해안선(영역 외곽선)에서 지면이 정확히 이 높이가 된다.")]
        public float seaLevel = 10f;

        [Tooltip("내륙 평지의 기본 높이 (m)")]
        public float landBase = 20f;

        [Tooltip("평지의 완만한 기복 높이 (m)")]
        public float hillHeight = 16f;

        [Tooltip("기복의 촘촘한 정도 (지도 좌표 1 당 파형 수)")]
        public float hillFrequency = 0.6f;

        [Tooltip("해안 경사가 이어지는 폭 (높이맵 픽셀 수)")]
        public int coastBlurRadius = 5;

        [Tooltip("해안에서 먼바다(바다 밑바닥 깊이)까지 서서히 깊어지는 폭 (높이맵 픽셀 수). " +
                 "해안 바로 앞은 완만하게 두어 물가가 각지지 않게 하고, 그 바깥만 깊게 만든다. " +
                 "너무 넓으면 멀리서 볼 때 지형 LOD 오차가 해안선을 크게 흔들고, 너무 좁으면 가까이서 물가가 격자 모양으로 각진다.")]
        public int deepSeaBlurRadius = 8;

        /// <summary>
        /// 해안 바로 앞 얕은 바다 밑바닥 높이 (m). 해수면을 기준으로 평지와 대칭이 되게 잡아,
        /// 외곽선 양쪽의 해안 경사가 같아진다.
        /// </summary>
        public float ShallowSeaFloor => 2f * seaLevel - landBase;

        [Tooltip("국경에서 이웃 지형 텍스처가 섞이는 폭 (스플랫맵 픽셀 수)")]
        public int borderBlendRadius = 2;

        [Tooltip("높이맵 해상도 (2^n + 1). 지형 크기 / (해상도 - 1) 이 높이 표본 간격이다. (120km 지형에서 2049 = 약 59m)")]
        public int heightmapResolution = 2049;

        [Tooltip("지형 텍스처 배치(스플랫맵) 해상도. 국경에서 텍스처가 섞이는 정밀도를 정한다.")]
        public int alphamapResolution = 1024;

        /// <summary>해수면 높이 (m)</summary>
        public float SeaLevel => seaLevel;

        /// <summary>지형이 담는 높이 범위 (m) = 최대 높이 - 바다 밑바닥. TerrainData 의 세로 크기가 된다.</summary>
        public float HeightRange => maxHeight - seaFloor;
    }

    [Tooltip("지도에 그릴 국가 영역 목록")]
    [SerializeField] List<NationRegion> regions = new List<NationRegion>();

    [Tooltip("지도 위 산맥 목록")]
    [SerializeField] List<MountainRange> mountainRanges = new List<MountainRange>();

    [Header("크기")]
    [Tooltip("지형이 만들어질 범위 (지도 좌표). 대륙 둘레에 바다 여백을 포함한다.")]
    [SerializeField] Rect mapBounds = new Rect(-6f, -7.2f, 12f, 14.4f);

    [Tooltip("지도 좌표 1 이 월드에서 몇 m 인지 (10000 = 참고 지도 100px 당 10km, 대륙 동서 약 89km)")]
    [SerializeField] float worldScale = 10000f;

    [Header("높낮이")]
    [SerializeField] ReliefSettings relief = new ReliefSettings();

    [Header("외곽선 거칠기 (해안·국경을 자연스럽게)")]
    [Tooltip("변 하나를 몇 번 반으로 나누며 흔들지 (0 이면 직선 그대로)")]
    [SerializeField, Range(0, 5)] int roughnessDepth = 3;

    [Tooltip("흔들림 크기 (변 길이 대비 비율)")]
    [SerializeField, Range(0f, 0.3f)] float roughnessAmount = 0.12f;

    [Header("국경선 표시")]
    [Tooltip("국경·해안선 색")]
    [SerializeField] Color borderColor = new Color(0.24f, 0.17f, 0.12f);

    [Tooltip("국경·해안선이 화면에서 보일 두께 (픽셀, 1080p 기준). 카메라 거리에 따라 실제 두께가 자동으로 바뀐다.")]
    [SerializeField, Min(0.1f)] float borderPixelWidth = 2f;

    [Tooltip("국경선을 지면에서 띄우는 높이 (m). 지면에 묻혀 끊겨 보이지 않게 한다.")]
    [SerializeField] float borderLift = 2.5f;

    /// <summary>지도에 그릴 국가 영역 목록</summary>
    public IReadOnlyList<NationRegion> Regions => regions;

    /// <summary>산맥 목록</summary>
    public IReadOnlyList<MountainRange> MountainRanges => mountainRanges;

    /// <summary>지형 생성 범위 (지도 좌표)</summary>
    public Rect MapBounds => mapBounds;

    /// <summary>지도 좌표 1 당 월드 m</summary>
    public float WorldScale => worldScale;

    /// <summary>높낮이 규칙</summary>
    public ReliefSettings Relief => relief;

    /// <summary>외곽선 세분화 횟수</summary>
    public int RoughnessDepth => roughnessDepth;

    /// <summary>외곽선 흔들림 비율</summary>
    public float RoughnessAmount => roughnessAmount;

    /// <summary>국경·해안선 색</summary>
    public Color BorderColor => borderColor;

    /// <summary>국경·해안선의 화면상 두께 (픽셀)</summary>
    public float BorderPixelWidth => borderPixelWidth;

    /// <summary>국경선을 지면에서 띄우는 높이 (m)</summary>
    public float BorderLift => borderLift;

    /// <summary>
    /// 지도 좌표를 주어진 높이의 월드 좌표로 바꾼다. (지도 y → 월드 z)
    /// </summary>
    public Vector3 ToWorld(Vector2 mapPoint, float height)
    {
        return new Vector3(mapPoint.x * worldScale, height, mapPoint.y * worldScale);
    }

    /// <summary>
    /// 지도 좌표를 지형 범위 안의 0~1 정규화 좌표로 바꾼다. (TerrainData 높이 조회용)
    /// </summary>
    public Vector2 ToNormalized(Vector2 mapPoint)
    {
        return new Vector2(
            (mapPoint.x - mapBounds.xMin) / mapBounds.width,
            (mapPoint.y - mapBounds.yMin) / mapBounds.height);
    }
}
