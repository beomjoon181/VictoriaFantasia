using UnityEngine;

/// <summary>
/// 월드 위치의 "보이는 지면" 높이를 알려 주는 인터페이스.
/// 카메라 등 지면 높이가 필요한 쪽은 이 인터페이스에만 의존해, 지형 구현(Unity Terrain 등)이 바뀌어도 영향을 받지 않는다.
/// </summary>
public interface IGroundHeightSampler
{
    /// <summary>
    /// 해당 위치(XZ)의 지면 높이 (월드 Y, m). 바다 위라면 해수면 높이를 돌려준다.
    /// </summary>
    float SampleHeight(Vector3 worldPosition);
}
