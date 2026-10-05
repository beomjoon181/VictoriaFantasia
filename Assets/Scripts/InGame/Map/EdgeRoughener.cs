using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 직선으로 된 다각형 변을 중점 변위(Midpoint Displacement)로 잘게 나눠 손으로 그린 지도처럼 울퉁불퉁하게 만든다.
///
/// 핵심: 흔들림 값은 "변의 두 끝점"만으로 결정되는 결정적(deterministic) 난수를 쓴다.
/// 그래서 이웃한 두 국가가 같은 국경 변을 반대 방향으로 갖고 있어도 완전히 같은 곡선이 만들어져
/// 국경 사이에 틈이나 겹침이 생기지 않는다.
/// </summary>
public static class EdgeRoughener
{
    /// <summary>
    /// 닫힌 다각형의 모든 변을 거칠게 만든 새 꼭짓점 목록을 돌려준다.
    /// </summary>
    /// <param name="outline">원본 외곽선 (닫힌 다각형, 마지막 점과 첫 점이 이어진다)</param>
    /// <param name="depth">변을 반으로 나누는 재귀 횟수 (변 하나당 2^depth 조각)</param>
    /// <param name="amount">변 길이 대비 최대 흔들림 비율</param>
    public static List<Vector2> Roughen(IReadOnlyList<Vector2> outline, int depth, float amount)
    {
        var result = new List<Vector2>();
        for (var i = 0; i < outline.Count; i++)
        {
            var a = outline[i];
            var b = outline[(i + 1) % outline.Count];

            // 시작점 a 는 넣고, 끝점 b 는 다음 변의 시작점으로 들어가므로 생략한다.
            result.Add(a);
            result.AddRange(RoughenEdgeInterior(a, b, depth, amount));
        }

        return result;
    }

    /// <summary>
    /// 변 a→b 사이에 들어갈 중간 꼭짓점들(양 끝점 제외)을 a→b 순서로 돌려준다.
    /// 두 끝점을 정해진 순서(정규 순서)로 정렬해 계산한 뒤, 필요하면 뒤집는다.
    /// </summary>
    static List<Vector2> RoughenEdgeInterior(Vector2 a, Vector2 b, int depth, float amount)
    {
        var points = new List<Vector2>();
        if (depth <= 0 || amount <= 0f)
            return points;

        var reversed = IsGreater(a, b);
        var start = reversed ? b : a;
        var end = reversed ? a : b;

        Subdivide(start, end, depth, amount, points);

        if (reversed)
            points.Reverse();
        return points;
    }

    /// <summary>
    /// 재귀적으로 중점을 수직 방향으로 밀어 내며 중간 점을 start→end 순서로 채운다.
    /// </summary>
    static void Subdivide(Vector2 start, Vector2 end, int depth, float amount, List<Vector2> output)
    {
        if (depth <= 0)
            return;

        var delta = end - start;
        var perpendicular = new Vector2(-delta.y, delta.x); // 길이 = 변 길이
        var mid = (start + end) * 0.5f + perpendicular * (Hash(start, end) * amount);

        Subdivide(start, mid, depth - 1, amount, output);
        output.Add(mid);
        Subdivide(mid, end, depth - 1, amount, output);
    }

    /// <summary>
    /// 두 점의 정규 순서 비교: x 가 크면 크고, x 가 같으면 y 로 비교한다.
    /// </summary>
    static bool IsGreater(Vector2 a, Vector2 b)
    {
        if (!Mathf.Approximately(a.x, b.x))
            return a.x > b.x;
        return a.y > b.y;
    }

    /// <summary>
    /// 두 점의 좌표만으로 정해지는 -1 ~ 1 범위의 결정적 난수.
    /// 같은 입력이면 언제나 같은 값이 나오므로 맵을 다시 만들어도 모양이 바뀌지 않는다.
    /// </summary>
    static float Hash(Vector2 a, Vector2 b)
    {
        unchecked
        {
            var h = Quantize(a.x) * 73856093;
            h ^= Quantize(a.y) * 19349663;
            h ^= Quantize(b.x) * 83492791;
            h ^= Quantize(b.y) * 50331653;
            h ^= h >> 13;
            h *= 1274126177;
            h ^= h >> 16;
            return (h & 0xFFFF) / 65535f * 2f - 1f;
        }
    }

    /// <summary>
    /// 부동소수 오차에 흔들리지 않도록 좌표를 0.001 단위 정수로 바꾼다.
    /// </summary>
    static int Quantize(float value) => Mathf.RoundToInt(value * 1000f);
}
