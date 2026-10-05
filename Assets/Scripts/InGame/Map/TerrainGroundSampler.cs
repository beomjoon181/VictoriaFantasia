using UnityEngine;

/// <summary>
/// Unity Terrain 과 해수면으로 지면 높이를 알려 주는 컴포넌트. (지도 Terrain 오브젝트에 붙는다)
/// 바다 밑바닥은 수면에 가려 보이지 않으므로, 지형 높이와 해수면 중 높은 쪽을 "보이는 지면" 으로 본다.
/// </summary>
public class TerrainGroundSampler : MonoBehaviour, IGroundHeightSampler
{
    [Tooltip("높이를 조회할 지형")]
    [SerializeField] Terrain terrain;

    [Tooltip("해수면 높이 (월드 Y, m)")]
    [SerializeField] float seaLevel;

    /// <summary>
    /// 생성 직후 지형과 해수면을 주입한다. (WorldMapGenerator 전용)
    /// </summary>
    public void Initialize(Terrain targetTerrain, float waterLevel)
    {
        terrain = targetTerrain;
        seaLevel = waterLevel;
    }

    /// <inheritdoc />
    public float SampleHeight(Vector3 worldPosition)
    {
        if (terrain == null)
            return seaLevel;

        // Terrain.SampleHeight 는 지형 오브젝트 기준 높이를 돌려주므로 지형의 월드 높이를 더한다.
        // (지형 범위 밖은 가장자리 높이로 계산되며, 가장자리는 바다 밑바닥이라 결국 해수면이 된다)
        var groundHeight = terrain.SampleHeight(worldPosition) + terrain.transform.position.y;
        return Mathf.Max(groundHeight, seaLevel);
    }
}
