using UnityEngine;

/// <summary>
/// 지도 카메라를 움직이는 명령 인터페이스.
/// 입력 처리(키보드·마우스·추후 게임패드/터치)는 이 인터페이스에만 의존하므로,
/// 카메라 동작 방식을 바꾸거나 새 입력 장치를 추가할 때 서로 영향을 주지 않는다. (의존성 역전)
/// </summary>
public interface IMapCameraRig
{
    /// <summary>현재 목표 거리 (카메라 ↔ 바라보는 지점, m)</summary>
    float Distance { get; }

    /// <summary>현재 목표 좌우 회전 각도 (0 = 북쪽을 봄, 양수 = 시계 방향, 도 단위). 화면 기준 이동 방향 계산에 쓴다.</summary>
    float Yaw { get; }

    /// <summary>
    /// 바라보는 지점을 월드 XZ 방향으로 옮긴다. (Y 성분은 무시)
    /// </summary>
    /// <param name="worldDelta">이동량 (m)</param>
    /// <param name="immediate">true 면 부드러운 보간 없이 즉시 반영 (마우스 드래그용)</param>
    void Pan(Vector3 worldDelta, bool immediate = false);

    /// <summary>
    /// 거리를 배율만큼 바꾼다. (1 보다 작으면 확대, 크면 축소)
    /// </summary>
    /// <param name="distanceFactor">거리 배율</param>
    /// <param name="screenAnchor">이 화면 위치 아래의 지점이 확대 전후로 그대로 있도록 고정 (null 이면 화면 중앙 기준)</param>
    void Zoom(float distanceFactor, Vector2? screenAnchor = null);

    /// <summary>
    /// 카메라를 회전한다.
    /// </summary>
    /// <param name="yawDelta">좌우 회전량 (도). 양수면 바라보는 지점을 중심으로 시계 방향으로 돌아 시선이 오른쪽으로 향한다.</param>
    /// <param name="pitchDelta">내려다보는 각도 변화 (도). 양수면 시선이 아래(땅 쪽)로, 음수면 위(지평선 쪽)로 향한다.</param>
    void Rotate(float yawDelta, float pitchDelta);

    /// <summary>
    /// 화면 좌표 아래의 지면(기준 높이 평면) 지점을 구한다. 목표 시점 기준으로 계산한다.
    /// </summary>
    /// <returns>지면과 만나면 true (하늘을 가리키면 false)</returns>
    bool TryGetGroundPoint(Vector2 screenPosition, out Vector3 point);

    /// <summary>처음 시점(대륙 전체 보기, 회전 없음)으로 돌아간다.</summary>
    void ResetView();
}
