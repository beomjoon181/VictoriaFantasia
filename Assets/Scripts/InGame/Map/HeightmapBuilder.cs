using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 육지 마스크와 산맥 정보로 3D 지형의 높이맵을 계산한다.
///
/// 높이 = 해안 경사(바다 밑바닥 ↔ 해수면 ↔ 평지) + 완만한 언덕 기복 + 산맥
/// - 해안 경사 : 육지(1)/바다(0) 마스크를 블러해 0~1 로 부드럽게 만든 값(landFactor). 외곽선 위에서 정확히 0.5 가 된다.
///              landFactor 0 → 0.5 구간은 바다 밑바닥 → 해수면, 0.5 → 1 구간은 해수면 → 평지로 이어 붙여
///              물가가 외곽선과 정확히 일치하게 한다. (바다 밑바닥 깊이를 바꿔도 육지 높이·해안선은 그대로)
/// - 언덕 기복 : 펄린 노이즈 합성(fBm). 육지 안쪽일수록 강하게 적용한다.
/// - 산맥      : 능선과의 거리로 산기슭 → 정상 프로필을 만들고, 능선형(ridged) 노이즈로 봉우리를 나눈다.
/// </summary>
public static class HeightmapBuilder
{
    /// <summary>노이즈가 원점 대칭으로 보이지 않도록 샘플 좌표를 옮기는 값</summary>
    static readonly Vector2 NoiseOffset = new Vector2(37.1f, 91.7f);

    /// <summary>넓게 블러한 육지 비율(offshore)이 이 값 아래로 내려가면 먼바다 쪽으로 깊어지기 시작한다. (해안에서 약 0.5)</summary>
    const float DeepSeaStart = 0.35f;

    /// <summary>offshore 가 이 값 이하인 곳은 바다 밑바닥(seaFloor) 깊이가 된다.</summary>
    const float DeepSeaEnd = 0.05f;

    /// <summary>
    /// Unity TerrainData.SetHeights 에 그대로 넣을 수 있는 정규화(0~1) 높이 배열을 만든다.
    /// </summary>
    /// <param name="grid">높이맵 격자 (꼭짓점 표본)</param>
    /// <param name="landCells">격자별 영역 번호 (0 = 바다)</param>
    /// <param name="relief">높낮이 규칙</param>
    /// <param name="mountains">산맥 목록</param>
    /// <returns>[행, 열] 순서의 0~1 높이 (0 = 바다 밑바닥 seaFloor, 1 = maxHeight)</returns>
    public static float[,] Build(MapGrid grid, int[] landCells, WorldMapDefinition.ReliefSettings relief,
        IReadOnlyList<WorldMapDefinition.MountainRange> mountains)
    {
        // 1) 육지 마스크 → 블러로 해안 경사 만들기
        var land = new float[grid.Count];
        for (var i = 0; i < land.Length; i++)
            land[i] = landCells[i] > 0 ? 1f : 0f;
        var shore = GridBlur.Blur(land, grid.Width, grid.Height, relief.coastBlurRadius);
        // 같은 마스크를 넓게 블러한 값: 해안에서 멀어질수록 0 에 가까워져 먼바다를 깊게 만드는 데 쓴다.
        var offshore = GridBlur.Blur(land, grid.Width, grid.Height, relief.deepSeaBlurRadius);

        var heights = new float[grid.Height, grid.Width];
        for (var row = 0; row < grid.Height; row++)
        {
            for (var column = 0; column < grid.Width; column++)
            {
                var point = grid.Point(column, row);
                var landFactor = Mathf.SmoothStep(0f, 1f, shore[grid.Index(column, row)]);

                // 2) 해안 경사 + 언덕 기복 + 산맥 (기복·산맥은 육지에만)
                //    언덕 기복은 외곽선(landFactor 0.5)에서 0 이 되게 해, 물가가 외곽선(=해안 국경선)과 정확히 일치하게 한다.
                var hillWeight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 1f, landFactor));
                var height = CoastProfile(landFactor, offshore[grid.Index(column, row)], relief);
                height += Hills(point, relief) * hillWeight;
                height += Mountains(point, mountains) * landFactor;

                // TerrainData 높이는 0~1 이므로 바다 밑바닥(가장 낮은 곳)을 0 으로 맞춘다. (지형 오브젝트는 그만큼 아래에 놓인다)
                heights[row, column] = Mathf.Clamp01((height - relief.seaFloor) / relief.HeightRange);
            }
        }

        return heights;
    }

    /// <summary>
    /// 해안 경사 높이 (m).
    /// - 육지 쪽 (landFactor 0.5 → 1): 해수면 → 평지.
    /// - 바다 쪽 (landFactor 0 → 0.5): 얕은 바다 밑바닥 → 해수면. 해수면을 사이에 두고 육지 쪽과 같은 완만한 경사라
    ///   물가 모양이 높이맵 격자에 따라 각지지 않는다.
    /// - 먼바다: 해안에서 멀어질수록(offshore 가 0 에 가까울수록) 바다 밑바닥(seaFloor)까지 서서히 깊어진다.
    ///   해안 부근 높이 차가 커야 멀리서 지형 LOD 가 해안을 지나치게 단순화하지 않는다.
    /// </summary>
    /// <param name="landFactor">좁게 블러한 육지 비율 (외곽선에서 0.5)</param>
    /// <param name="offshore">넓게 블러한 육지 비율 (해안에서 약 0.5, 먼바다에서 0)</param>
    static float CoastProfile(float landFactor, float offshore, WorldMapDefinition.ReliefSettings relief)
    {
        if (landFactor >= 0.5f)
            return Mathf.Lerp(relief.SeaLevel, relief.landBase, (landFactor - 0.5f) * 2f);

        var shallow = Mathf.Lerp(relief.ShallowSeaFloor, relief.SeaLevel, landFactor * 2f);
        // 해안(offshore ≈ 0.5) 바로 앞은 깊어지지 않게 두고(얕은 바다 띠), 조금 떨어진 곳부터 먼바다 깊이로 내려간다.
        var deepWeight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(DeepSeaStart, DeepSeaEnd, offshore)); // 해안 0 → 먼바다 1
        return shallow + (relief.seaFloor - relief.ShallowSeaFloor) * deepWeight;
    }

    /// <summary>
    /// 평지의 완만한 언덕 높이 (m). 서로 다른 주파수의 펄린 노이즈 3 겹을 합친다.
    /// </summary>
    static float Hills(Vector2 point, WorldMapDefinition.ReliefSettings relief)
    {
        var p = point * relief.hillFrequency + NoiseOffset;
        var value = Mathf.PerlinNoise(p.x, p.y) * 0.6f
                  + Mathf.PerlinNoise(p.x * 2.1f, p.y * 2.1f) * 0.3f
                  + Mathf.PerlinNoise(p.x * 4.3f, p.y * 4.3f) * 0.1f;
        return value * relief.hillHeight;
    }

    /// <summary>
    /// 모든 산맥의 높이 기여 (m). 산맥이 겹치면 더 높은 쪽을 쓴다.
    /// </summary>
    static float Mountains(Vector2 point, IReadOnlyList<WorldMapDefinition.MountainRange> mountains)
    {
        var highest = 0f;
        foreach (var range in mountains)
        {
            if (range.ridge == null || range.ridge.Length < 2 || range.halfWidth <= 0f)
                continue;

            var distance = DistanceToPolyline(point, range.ridge);
            if (distance >= range.halfWidth)
                continue;

            // 산기슭(0) → 능선(1) 프로필을 S 곡선으로 만들고, 능선형 노이즈로 봉우리와 안부를 나눈다.
            var t = 1f - distance / range.halfWidth;
            var profile = t * t * (3f - 2f * t);
            var p = point * range.peakFrequency + NoiseOffset;
            var ridged = 1f - Mathf.Abs(Mathf.PerlinNoise(p.x, p.y) * 2f - 1f);
            var peaks = 0.45f + 0.55f * ridged * ridged;

            highest = Mathf.Max(highest, range.peakHeight * profile * peaks);
        }

        return highest;
    }

    /// <summary>
    /// 점과 꺾은선 사이의 최단 거리.
    /// </summary>
    static float DistanceToPolyline(Vector2 point, Vector2[] polyline)
    {
        var best = float.MaxValue;
        for (var i = 0; i + 1 < polyline.Length; i++)
            best = Mathf.Min(best, DistanceToSegment(point, polyline[i], polyline[i + 1]));
        return best;
    }

    /// <summary>
    /// 점과 선분 ab 사이의 최단 거리.
    /// </summary>
    static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var lengthSquared = ab.sqrMagnitude;
        var t = lengthSquared > 0f ? Mathf.Clamp01(Vector2.Dot(point - a, ab) / lengthSquared) : 0f;
        return Vector2.Distance(point, a + ab * t);
    }
}
