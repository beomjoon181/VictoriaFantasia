using UnityEngine;

/// <summary>
/// 선(LineRenderer)들이 카메라 거리와 관계없이 화면에서 거의 같은 픽셀 두께로 보이도록 두께를 조절한다.
///
/// 지도가 수십 km 크기라 고정 두께(m)로 그리면 멀리서는 보이지 않고 가까이서는 너무 두껍다.
/// 매 프레임 화면 중앙이 가리키는 지면까지의 거리를 재서, 그 거리에서 원하는 픽셀 수에 해당하는 월드 두께로 맞춘다.
/// (화면 중앙보다 가까운 곳의 선은 조금 두껍게, 먼 곳의 선은 조금 얇게 보여 원근감은 유지된다)
/// 에디터에서도 씬·게임 뷰가 실제와 같게 보이도록 플레이 중이 아닐 때도 동작한다.
/// </summary>
[ExecuteAlways]
public class ScreenSpaceLineWidth : MonoBehaviour
{
    [Tooltip("기준 카메라 (비어 있으면 Camera.main)")]
    [SerializeField] Camera targetCamera;

    [Tooltip("두께를 조절할 선들")]
    [SerializeField] LineRenderer[] lines = new LineRenderer[0];

    [Tooltip("화면에서 보일 선 두께 (픽셀, 1080p 기준)")]
    [SerializeField, Min(0.1f)] float pixelWidth = 2f;

    [Tooltip("화면 중앙 지점까지 거리를 잴 때 쓰는 지면 높이 (월드 Y, m)")]
    [SerializeField] float referenceHeight;

    /// <summary>두께가 이 비율 이상 바뀔 때만 다시 설정한다. (매 프레임 렌더러 값을 건드리지 않게)</summary>
    const float UpdateThreshold = 0.01f;

    /// <summary>픽셀 두께의 기준 화면 높이 (해상도가 달라도 화면 대비 같은 비율로 보이게)</summary>
    const float ReferenceScreenHeight = 1080f;

    /// <summary>마지막으로 적용한 월드 두께 (m)</summary>
    float appliedWidth = -1f;

    /// <summary>
    /// 생성 직후 대상 선·두께·기준 높이를 주입한다. (WorldMapGenerator 전용)
    /// </summary>
    public void Initialize(LineRenderer[] targetLines, float widthInPixels, float groundHeight)
    {
        lines = targetLines;
        pixelWidth = widthInPixels;
        referenceHeight = groundHeight;
        appliedWidth = -1f;
    }

    /// <summary>
    /// 카메라 이동이 끝난 뒤 두께를 맞춘다.
    /// </summary>
    void LateUpdate()
    {
        Refresh(false);
    }

    /// <summary>
    /// 지금 카메라 거리에 맞춰 두께를 다시 계산한다.
    /// 에디터에서 지도를 막 만들었을 때처럼 아직 한 번도 갱신되지 않은 경우 바로 맞추는 데 쓴다.
    /// </summary>
    /// <param name="force">true 면 변화가 작아도 다시 설정한다.</param>
    public void Refresh(bool force = true)
    {
        var viewCamera = targetCamera != null ? targetCamera : Camera.main;
        if (viewCamera == null || lines == null)
            return;

        var width = WorldWidthFor(viewCamera);
        if (!force && appliedWidth > 0f && Mathf.Abs(width - appliedWidth) <= appliedWidth * UpdateThreshold)
            return;

        foreach (var line in lines)
        {
            if (line != null)
                line.widthMultiplier = width;
        }

        appliedWidth = width;
    }

    /// <summary>
    /// 화면 중앙 지면까지의 거리에서 pixelWidth 픽셀이 차지하는 월드 길이 (m).
    /// 1픽셀 크기 = 거리 × 2·tan(시야각/2) / 화면 높이(픽셀)
    /// </summary>
    float WorldWidthFor(Camera viewCamera)
    {
        var distance = DistanceToScreenCenterGround(viewCamera);
        if (viewCamera.orthographic)
            return viewCamera.orthographicSize * 2f / ReferenceScreenHeight * pixelWidth;

        var viewHeight = 2f * distance * Mathf.Tan(viewCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        return viewHeight / ReferenceScreenHeight * pixelWidth;
    }

    /// <summary>
    /// 화면 중앙에서 쏜 광선이 기준 높이 평면과 만나는 곳까지의 거리. 하늘을 보면 카메라 높이로 대신한다.
    /// </summary>
    float DistanceToScreenCenterGround(Camera viewCamera)
    {
        var cameraTransform = viewCamera.transform;
        var ray = new Ray(cameraTransform.position, cameraTransform.forward);
        var ground = new Plane(Vector3.up, new Vector3(0f, referenceHeight, 0f));
        if (ground.Raycast(ray, out var enter))
            return enter;

        return Mathf.Max(1f, cameraTransform.position.y - referenceHeight);
    }
}
