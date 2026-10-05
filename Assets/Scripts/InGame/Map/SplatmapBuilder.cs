using UnityEngine;

/// <summary>
/// 영역별 지형 레이어 배치로 Unity Terrain 의 스플랫맵(레이어별 섞임 비율)을 만든다.
/// 국경에서는 양쪽 레이어를 살짝 블러해 텍스처가 칼로 자른 듯 끊기지 않고 자연스럽게 섞이게 한다.
/// </summary>
public static class SplatmapBuilder
{
    /// <summary>
    /// TerrainData.SetAlphamaps 에 그대로 넣을 수 있는 [행, 열, 레이어] 배열을 만든다.
    /// </summary>
    /// <param name="grid">스플랫맵 격자 (칸 중심 표본)</param>
    /// <param name="regionCells">격자별 영역 번호 (0 = 바다, n = n-1 번째 영역)</param>
    /// <param name="regionLayers">영역 순번 → 레이어 번호</param>
    /// <param name="layerCount">레이어 수</param>
    /// <param name="blendRadius">국경 섞임 반경 (표본 수)</param>
    public static float[,,] Build(MapGrid grid, int[] regionCells, int[] regionLayers, int layerCount, int blendRadius)
    {
        // 1) 레이어마다 "이 표본이 해당 레이어인가"(0/1) 마스크를 만들어 블러한다.
        //    바다 표본은 첫 번째 레이어로 둔다. (수면 아래라 보이지 않지만 합이 0 이 되는 것을 막는다)
        var blurred = new float[layerCount][];
        for (var layer = 0; layer < layerCount; layer++)
        {
            var mask = new float[grid.Count];
            for (var i = 0; i < mask.Length; i++)
                mask[i] = LayerOf(regionCells[i], regionLayers) == layer ? 1f : 0f;
            blurred[layer] = GridBlur.Blur(mask, grid.Width, grid.Height, blendRadius);
        }

        // 2) 표본마다 레이어 비율의 합이 1 이 되도록 정규화한다.
        var alphamaps = new float[grid.Height, grid.Width, layerCount];
        for (var row = 0; row < grid.Height; row++)
        {
            for (var column = 0; column < grid.Width; column++)
            {
                var index = grid.Index(column, row);
                var total = 0f;
                for (var layer = 0; layer < layerCount; layer++)
                    total += blurred[layer][index];

                for (var layer = 0; layer < layerCount; layer++)
                    alphamaps[row, column, layer] = total > 0f ? blurred[layer][index] / total : (layer == 0 ? 1f : 0f);
            }
        }

        return alphamaps;
    }

    /// <summary>
    /// 영역 번호(0 = 바다)를 레이어 번호로 바꾼다.
    /// </summary>
    static int LayerOf(int regionCell, int[] regionLayers)
    {
        return regionCell > 0 ? regionLayers[regionCell - 1] : 0;
    }
}
