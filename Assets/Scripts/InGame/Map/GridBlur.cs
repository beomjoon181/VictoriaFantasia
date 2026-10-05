using UnityEngine;

/// <summary>
/// 격자 값을 부드럽게 퍼뜨리는 블러 유틸리티.
/// 가로·세로로 나눠 처리하는 박스 블러를 여러 번 반복해 가우시안 블러에 가까운 결과를 낸다.
/// 누적합(running sum)을 써서 반경과 무관하게 표본당 일정한 비용으로 계산한다.
/// </summary>
public static class GridBlur
{
    /// <summary>
    /// 블러를 적용한 새 배열을 돌려준다. (원본은 바꾸지 않음)
    /// </summary>
    /// <param name="source">행 우선 격자 값</param>
    /// <param name="width">가로 표본 수</param>
    /// <param name="height">세로 표본 수</param>
    /// <param name="radius">박스 반경 (표본 수). 0 이하이면 복사본만 돌려준다.</param>
    /// <param name="passes">반복 횟수 (3 이면 가우시안과 거의 같다)</param>
    public static float[] Blur(float[] source, int width, int height, int radius, int passes = 3)
    {
        var current = (float[])source.Clone();
        if (radius <= 0)
            return current;

        var buffer = new float[current.Length];
        for (var pass = 0; pass < passes; pass++)
        {
            BoxPass(current, buffer, width, height, radius, true);
            BoxPass(buffer, current, width, height, radius, false);
        }

        return current;
    }

    /// <summary>
    /// 한 방향 박스 블러. 범위 밖 표본은 가장자리 값을 반복한 것으로 본다.
    /// </summary>
    /// <param name="horizontal">true 면 행 방향, false 면 열 방향</param>
    static void BoxPass(float[] input, float[] output, int width, int height, int radius, bool horizontal)
    {
        var lineCount = horizontal ? height : width;
        var lineLength = horizontal ? width : height;
        var scale = 1f / (radius * 2 + 1);

        for (var line = 0; line < lineCount; line++)
        {
            // 한 줄 안의 k 번째 표본을 1차원 인덱스로 바꾸는 함수 (가장자리 고정)
            int At(int k)
            {
                k = Mathf.Clamp(k, 0, lineLength - 1);
                return horizontal ? line * width + k : k * width + line;
            }

            // 첫 창의 합을 구한 뒤, 한 칸씩 밀면서 들어오는 값은 더하고 나가는 값은 뺀다.
            var sum = 0f;
            for (var k = -radius; k <= radius; k++)
                sum += input[At(k)];

            for (var k = 0; k < lineLength; k++)
            {
                output[At(k)] = sum * scale;
                sum += input[At(k + radius + 1)] - input[At(k - radius)];
            }
        }
    }
}
