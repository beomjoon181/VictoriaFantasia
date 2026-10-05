using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 다각형 내부에 들어가는 격자 표본을 찾아 값을 칠하는 스캔라인 래스터라이저.
/// 행마다 다각형 변과의 교차점을 구해 짝수-홀수 규칙으로 안쪽 구간만 칠하므로,
/// 표본마다 점-다각형 판정을 하는 것보다 훨씬 빠르다. (비용 ≈ 행 수 × 변 수)
/// </summary>
public static class PolygonRasterizer
{
    /// <summary>
    /// 다각형 안쪽 표본에 value 를 기록한다. 이미 값이 있어도 덮어쓴다.
    /// </summary>
    /// <param name="polygon">닫힌 다각형 (지도 좌표)</param>
    /// <param name="grid">표본 격자</param>
    /// <param name="cells">격자 크기의 결과 배열</param>
    /// <param name="value">칠할 값</param>
    public static void Fill(IReadOnlyList<Vector2> polygon, MapGrid grid, int[] cells, int value)
    {
        var crossings = new List<float>();
        var count = polygon.Count;

        for (var row = 0; row < grid.Height; row++)
        {
            var y = grid.Y(row);
            crossings.Clear();

            // 이 행의 수평선과 만나는 변의 교차 x 를 모은다.
            // 아래 끝점은 포함, 위 끝점은 제외해 꼭짓점이 두 번 세어지지 않게 한다.
            for (var i = 0; i < count; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % count];
                var crosses = (a.y <= y && b.y > y) || (b.y <= y && a.y > y);
                if (crosses)
                    crossings.Add(a.x + (y - a.y) / (b.y - a.y) * (b.x - a.x));
            }

            crossings.Sort();

            // (진입, 탈출) 쌍 사이의 표본이 다각형 내부다.
            for (var k = 0; k + 1 < crossings.Count; k += 2)
            {
                var first = Mathf.Max(0, Mathf.CeilToInt(grid.ColumnOf(crossings[k])));
                var last = Mathf.Min(grid.Width - 1, Mathf.CeilToInt(grid.ColumnOf(crossings[k + 1])) - 1);
                for (var column = first; column <= last; column++)
                    cells[grid.Index(column, row)] = value;
            }
        }
    }
}
