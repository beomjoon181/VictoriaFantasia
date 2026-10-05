using UnityEngine;

/// <summary>
/// 지도 범위를 일정 간격으로 나눈 격자(높이맵·스플랫맵 공용)의 좌표 계산을 담당한다.
///
/// 두 가지 표본 방식을 지원한다.
/// - 꼭짓점 표본(cellCentered = false): 첫/마지막 표본이 범위 가장자리에 놓인다. (Unity 높이맵 방식)
/// - 칸 중심 표본(cellCentered = true) : 각 칸의 가운데에서 표본을 뽑는다. (Unity 스플랫맵 방식)
///
/// 배열 인덱스는 행 우선(index = row * Width + column)이며, row 0 이 지도 남쪽(yMin)이다.
/// </summary>
public readonly struct MapGrid
{
    /// <summary>격자가 덮는 지도 범위</summary>
    public readonly Rect Bounds;

    /// <summary>가로 표본 수</summary>
    public readonly int Width;

    /// <summary>세로 표본 수</summary>
    public readonly int Height;

    /// <summary>true 면 칸 중심 표본, false 면 꼭짓점 표본</summary>
    public readonly bool CellCentered;

    public MapGrid(Rect bounds, int width, int height, bool cellCentered)
    {
        Bounds = bounds;
        Width = width;
        Height = height;
        CellCentered = cellCentered;
    }

    /// <summary>전체 표본 수</summary>
    public int Count => Width * Height;

    /// <summary>(열, 행) → 1차원 배열 인덱스</summary>
    public int Index(int column, int row) => row * Width + column;

    /// <summary>열 번호의 지도 x 좌표</summary>
    public float X(int column) => Bounds.xMin + ToFraction(column, Width) * Bounds.width;

    /// <summary>행 번호의 지도 y 좌표</summary>
    public float Y(int row) => Bounds.yMin + ToFraction(row, Height) * Bounds.height;

    /// <summary>(열, 행) 표본의 지도 좌표</summary>
    public Vector2 Point(int column, int row) => new Vector2(X(column), Y(row));

    /// <summary>
    /// 지도 x 좌표가 몇 번째 열에 해당하는지 (소수 포함). 래스터화에서 칠할 열 범위를 구할 때 쓴다.
    /// </summary>
    public float ColumnOf(float x)
    {
        var fraction = (x - Bounds.xMin) / Bounds.width;
        return CellCentered ? fraction * Width - 0.5f : fraction * (Width - 1);
    }

    /// <summary>
    /// 표본 번호를 범위 안의 0~1 위치로 바꾼다.
    /// </summary>
    float ToFraction(int index, int count)
    {
        return CellCentered ? (index + 0.5f) / count : (float)index / (count - 1);
    }
}
