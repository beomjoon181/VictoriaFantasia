using UnityEngine;

/// <summary>
/// 전략 지도용 카메라 리그. 카메라에 붙어 "바라보는 지점(focus) + 거리(distance) + 회전(yaw, pitch)" 으로 시점을 관리한다.
///
/// - 좌우 회전(yaw): 바라보는 지점을 중심으로 카메라가 빙 돈다. (0 = 남쪽에서 북쪽을 봄)
/// - 내려다보는 각도(pitch) = 거리에 따른 자동 각도 + 사용자가 기울인 보정값.
///   자동 각도는 멀리서는 지도처럼 내려다보고, 가까이 갈수록 지평선 쪽으로 눕혀 지형의 입체감을 보여 준다.
///   최종 각도는 항상 minPitch ~ maxPitch 범위 안에 있다.
/// - 바라보는 지점은 대륙 범위(panBounds) 밖으로 나가지 않으며, 높이는 그 자리의 지면(산 위면 산 표면)에 맞춘다.
/// - 지형 회피: 카메라(와 바로 앞 시선)가 지면에 묻힐 것 같으면 거리는 유지한 채 더 내려다보도록 각도를 즉시 높이고,
///   지형을 벗어나면 천천히 원래 각도로 되돌린다.
/// - 명령은 "목표 상태"만 바꾸고, 실제 카메라는 LateUpdate 에서 목표를 부드럽게 따라간다.
///
/// 입력 해석은 하지 않는다. (MapCameraInput 등 입력 컴포넌트가 IMapCameraRig 로 명령)
/// </summary>
[RequireComponent(typeof(Camera))]
public class MapCameraRig : MonoBehaviour, IMapCameraRig
{
    [Header("시작 시점")]
    [Tooltip("처음 바라보는 지점 (월드)")]
    [SerializeField] Vector3 focus;

    [Tooltip("처음 거리 (m)")]
    [SerializeField] float distance = 220000f;

    [Tooltip("처음 좌우 회전 각도 (0 = 남쪽에서 북쪽을 봄, 양수 = 시계 방향)")]
    [SerializeField] float yaw;

    [Header("확대 / 축소")]
    [Tooltip("가장 가까이 다가갈 수 있는 거리 (m)")]
    [SerializeField] float minDistance = 250f;

    [Tooltip("가장 멀리 물러날 수 있는 거리 (m)")]
    [SerializeField] float maxDistance = 250000f;

    [Header("내려다보는 각도")]
    [Tooltip("가장 가까울 때의 자동 각도 (0 = 수평, 90 = 바로 위)")]
    [SerializeField, Range(10f, 90f)] float pitchAtMinDistance = 35f;

    [Tooltip("가장 멀 때의 자동 각도")]
    [SerializeField, Range(10f, 90f)] float pitchAtMaxDistance = 52f;

    [Tooltip("위로 기울일 수 있는 한계 (이보다 낮으면 지평선만 보여 지도를 읽기 어렵다)")]
    [SerializeField, Range(5f, 90f)] float minPitch = 15f;

    [Tooltip("아래로 기울일 수 있는 한계 (90 = 바로 위에서 내려다봄)")]
    [SerializeField, Range(5f, 90f)] float maxPitch = 89f;

    [Header("이동 범위")]
    [Tooltip("바라보는 지점이 머무를 수 있는 월드 XZ 범위 (x = 동서, y = 남북)")]
    [SerializeField] Rect panBounds = new Rect(-45000f, -58000f, 90000f, 116000f);

    [Tooltip("마우스가 가리키는 지점을 계산할 때 쓰는 지면 높이 (m)")]
    [SerializeField] float groundHeight = 20f;

    [Header("지형 회피")]
    [Tooltip("지면 높이를 알려 주는 컴포넌트 (없으면 지형 회피를 하지 않고 바라보는 높이를 0 으로 둔다)")]
    [SerializeField] TerrainGroundSampler groundSampler;

    [Tooltip("카메라가 지면에서 최소한 떨어져 있어야 하는 높이 (m). 시선을 따라 바라보는 지점에 가까워질수록 요구 간격이 0 으로 줄어든다.")]
    [SerializeField, Min(0f)] float minClearance = 25f;

    [Tooltip("카메라 가까이에서 뾰족한 봉우리 사이 빈틈을 메운 덮개 높이로 검사하는 시선 구간 (m). 카메라 바로 앞 화면이 땅에 묻히지 않게 한다. (근접 클리핑 거리보다 길게)")]
    [SerializeField, Min(0f)] float nearCheckLength = 30f;

    [Tooltip("바라보는 지점이 산에 가려 각도를 올릴 때 걸리는 시간 (초). 카메라 자체가 땅에 닿는 경우는 이 값과 무관하게 즉시 올린다.")]
    [SerializeField, Min(0f)] float terrainRiseTime = 0.2f;

    [Tooltip("지형 때문에 들어 올린 각도가 지형을 벗어난 뒤 원래대로 내려오는 데 걸리는 시간 (초)")]
    [SerializeField, Min(0f)] float terrainReleaseTime = 0.35f;

    [Header("부드러움")]
    [Tooltip("목표 시점을 따라가는 데 걸리는 시간 (초). 0 이면 즉시 이동.")]
    [SerializeField, Min(0f)] float smoothTime = 0.12f;

    /// <summary>
    /// 시선을 따라 지면 간격을 검사하는 간격 (m).
    /// 산 봉우리가 뾰족해(폭 약 20m) 띄엄띄엄 검사하면 봉우리가 사이로 끼어들므로 촘촘히 검사한다.
    /// </summary>
    const float SightCheckSpacing = 5f;

    /// <summary>시선 검사 지점 수의 상한. 멀리 있을 때는 카메라가 충분히 높아 성기게 검사해도 된다.</summary>
    const int MaxSightSamples = 120;

    /// <summary>
    /// 시선 검사를 멈추는 위치 (0 = 카메라, 1 = 바라보는 지점).
    /// 바라보는 지점 자체는 지면 위이므로, 그 바로 앞은 완만한 언덕에도 걸리기 쉬워 검사하지 않는다.
    /// </summary>
    const float SightCheckEnd = 0.95f;

    /// <summary>
    /// 지면 간격을 잴 때 주변을 함께 살피는 반경 (m).
    /// 뾰족한 봉우리 사이 빈틈을 메운 "덮개 높이"를 쓰면, 카메라가 봉우리 사이를 지날 때 통과·막힘이 번갈아
    /// 나타나 각도가 크게 튀는 일이 줄어든다.
    /// </summary>
    const float ClearanceProbeRadius = 20f;

    /// <summary>덮개 높이를 구할 때 중심 주변을 살피는 방향들 (XZ, 단위 벡터)</summary>
    static readonly Vector3[] ProbeDirections =
    {
        new Vector3(1f, 0f, 0f), new Vector3(-1f, 0f, 0f), new Vector3(0f, 0f, 1f), new Vector3(0f, 0f, -1f),
        new Vector3(0.7071f, 0f, 0.7071f), new Vector3(-0.7071f, 0f, 0.7071f),
        new Vector3(0.7071f, 0f, -0.7071f), new Vector3(-0.7071f, 0f, -0.7071f),
    };

    /// <summary>가까운 클리핑 거리 = 카메라 거리 × 이 값. 지면 최소 간격(minClearance)보다 충분히 작아야 한다.</summary>
    const float NearClipFraction = 0.01f;

    /// <summary>가까운 클리핑 거리의 최솟값 (m)</summary>
    const float MinNearClip = 1f;

    /// <summary>지형을 피할 각도를 찾을 때 처음 훑는 간격 (도)</summary>
    const float PitchSearchStep = 2f;

    /// <summary>훑어서 찾은 구간을 반씩 좁혀 정밀하게 맞추는 횟수 (2° → 약 0.03°, 이동 중 각도가 계단처럼 튀지 않게)</summary>
    const int PitchRefineIterations = 6;

    Camera cachedCamera;

    /// <summary>인터페이스로 다루는 지면 높이 조회 (없으면 null)</summary>
    IGroundHeightSampler Ground => groundSampler;

    /// <summary>사용자가 R/F 등으로 기울인 각도 (자동 각도에 더해진다, 목표 상태)</summary>
    float pitchOffset;

    // 실제 카메라가 보여 주고 있는 상태 (목표 상태를 부드럽게 따라간다)
    Vector3 currentFocus;
    float currentDistance;
    float currentYaw;
    float currentPitchOffset;

    /// <summary>
    /// 지형을 피하려고 원하는 각도보다 더 내려다보게 만든 양 (도, 0 이상).
    /// 카메라 앞 시선 구간 기준 목표로 부드럽게 따라가되(올릴 때 terrainRiseTime, 내릴 때 terrainReleaseTime),
    /// 카메라 자체가 땅에 닿지 않는 최소값 아래로는 절대 내려가지 않는다.
    /// </summary>
    float terrainLift;

    // ResetView 로 돌아갈 처음 시점
    Vector3 homeFocus;
    float homeDistance;
    float homeYaw;

    /// <inheritdoc />
    public float Distance => distance;

    /// <inheritdoc />
    public float Yaw => yaw;

    /// <summary>카메라 컴포넌트 (에디터에서 Awake 전에 불려도 안전하도록 지연 조회)</summary>
    Camera Camera => cachedCamera != null ? cachedCamera : cachedCamera = GetComponent<Camera>();

    /// <summary>
    /// 시작 시점을 기억하고 카메라를 즉시 그 자리에 놓는다.
    /// </summary>
    void Awake()
    {
        homeFocus = focus;
        homeDistance = distance;
        homeYaw = yaw;
        SnapToTarget();
    }

    /// <summary>
    /// 모든 입력 처리(Update)가 끝난 뒤 목표 시점을 향해 카메라를 옮긴다.
    /// 게임 일시 정지(timeScale)와 무관하게 움직이도록 unscaled 시간을 쓴다.
    /// </summary>
    void LateUpdate()
    {
        // 프레임 속도와 무관한 지수 감쇠 보간
        var t = smoothTime > 0f ? 1f - Mathf.Exp(-Time.unscaledDeltaTime / smoothTime) : 1f;
        currentFocus = Vector3.Lerp(currentFocus, focus, t);
        currentDistance = Mathf.Lerp(currentDistance, distance, t);
        // yaw 는 360 으로 감싸지 않고 누적하므로 일반 Lerp 로도 반대 방향으로 도는 일이 없다.
        currentYaw = Mathf.Lerp(currentYaw, yaw, t);
        currentPitchOffset = Mathf.Lerp(currentPitchOffset, pitchOffset, t);
        UpdateCameraPose(Time.unscaledDeltaTime);
    }

    /// <summary>
    /// 인스펙터 값이 바뀌면 범위를 다시 맞춘다.
    /// </summary>
    void OnValidate()
    {
        minDistance = Mathf.Max(1f, minDistance);
        maxDistance = Mathf.Max(minDistance, maxDistance);
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
        maxPitch = Mathf.Max(minPitch, maxPitch);
    }

    /// <inheritdoc />
    public void Pan(Vector3 worldDelta, bool immediate = false)
    {
        worldDelta.y = 0f;
        focus = ClampFocus(focus + worldDelta);
        if (immediate)
            currentFocus = focus;
    }

    /// <inheritdoc />
    public void Zoom(float distanceFactor, Vector2? screenAnchor = null)
    {
        // 기준 지점이 확대 전후로 같은 화면 위치에 오도록, 바뀐 시점에서 다시 구한 지점과의 차이만큼 밀어 준다.
        var before = Vector3.zero;
        var hasAnchor = screenAnchor.HasValue && TryGetGroundPoint(screenAnchor.Value, out before);

        distance = Mathf.Clamp(distance * distanceFactor, minDistance, maxDistance);

        if (hasAnchor && TryGetGroundPoint(screenAnchor.Value, out var after))
            focus = ClampFocus(focus + (before - after));
    }

    /// <inheritdoc />
    public void Rotate(float yawDelta, float pitchDelta)
    {
        yaw += yawDelta;

        // 지금 거리에서 최종 각도가 한계 안에 머물도록 보정값을 제한한다.
        var autoPitch = AutoPitchFor(distance);
        pitchOffset = Mathf.Clamp(pitchOffset + pitchDelta, minPitch - autoPitch, maxPitch - autoPitch);
    }

    /// <inheritdoc />
    public bool TryGetGroundPoint(Vector2 screenPosition, out Vector3 point)
    {
        // 실제 카메라가 아닌 "목표 시점" 기준으로 광선을 계산한다.
        // (부드러운 이동 중에도 드래그·확대 기준점이 흔들리지 않게)
        var ray = TargetRay(screenPosition);
        var ground = new Plane(Vector3.up, new Vector3(0f, groundHeight, 0f));
        if (ground.Raycast(ray, out var enter))
        {
            point = ray.GetPoint(enter);
            return true;
        }

        point = default;
        return false;
    }

    /// <inheritdoc />
    public void ResetView()
    {
        focus = homeFocus;
        distance = homeDistance;
        yaw = homeYaw;
        pitchOffset = 0f;
    }

    /// <summary>
    /// 보간 없이 카메라를 목표 시점에 바로 놓는다. (에디터 빌더에서 씬 저장 전 위치를 맞출 때도 사용)
    /// </summary>
    public void SnapToTarget()
    {
        focus = ClampFocus(focus);
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
        currentFocus = focus;
        currentDistance = distance;
        currentYaw = yaw;
        currentPitchOffset = pitchOffset;

        var lookAt = SurfacePoint(currentFocus);
        var desired = DesiredPitch(currentDistance, currentPitchOffset);
        terrainLift = ClearPitch(lookAt, currentDistance, currentYaw, desired, true) - desired;
        PlaceCamera(lookAt, currentDistance, currentYaw, desired + terrainLift);
    }

    // ═════════════════════════ 내부 계산 ═════════════════════════

    /// <summary>
    /// 현재 상태로 카메라를 배치한다. (매 프레임 LateUpdate 에서 호출)
    ///
    /// 1. 바라보는 지점을 실제 지면(산 위면 산 표면) 높이에 놓는다.
    /// 2. 원하는 각도(거리별 자동 각도 + 사용자 보정)에서 지형에 걸리면 더 내려다보도록 각도를 올린다. 기준은 두 가지다.
    ///    - 반드시 지킬 기준: 카메라 자체가 지면에서 minClearance 이상 떠 있을 것. 즉시 적용한다.
    ///      지면이 연속적이라 이 기준이 요구하는 각도도 거의 연속적으로 변해 화면이 크게 튀지 않는다.
    ///    - 바라는 기준: 바라보는 지점까지의 시선 전체가 지면에 가리지 않을 것. (산 절벽이 화면을 가득 채우지 않게)
    ///      산을 넘는 순간 필요한 각도가 크게 뛰므로, terrainRiseTime 동안 부드럽게 올린다.
    ///    지형을 벗어나면 terrainReleaseTime 동안 천천히 내린다.
    ///    사용자 보정값 자체는 바꾸지 않으므로 지형을 벗어나면 원래 각도로 돌아온다.
    /// </summary>
    void UpdateCameraPose(float deltaTime)
    {
        var lookAt = SurfacePoint(currentFocus);
        var desired = DesiredPitch(currentDistance, currentPitchOffset);
        var hardLift = ClearPitch(lookAt, currentDistance, currentYaw, desired, false) - desired;
        var softLift = ClearPitch(lookAt, currentDistance, currentYaw, desired, true) - desired;

        var smoothing = softLift > terrainLift ? terrainRiseTime : terrainReleaseTime;
        var t = smoothing > 0f ? 1f - Mathf.Exp(-deltaTime / smoothing) : 1f;
        terrainLift = Mathf.Max(Mathf.Lerp(terrainLift, softLift, t), hardLift);

        // terrainLift 는 hardLift 이상이므로 카메라 자체는 항상 지면 위에 있다.
        PlaceCamera(lookAt, currentDistance, currentYaw, Mathf.Min(desired + terrainLift, maxPitch));
    }

    /// <summary>
    /// 거리에 맞는 자동 내려다보는 각도. 가까울수록 낮은 각도(지평선 쪽)가 된다.
    /// </summary>
    float AutoPitchFor(float cameraDistance)
    {
        // 거리 범위가 수백 m ~ 수십 km 로 넓으므로 로그 비율로 보간한다.
        // (선형이면 수 km 이하에서 각도가 거의 변하지 않는다. 확대·축소도 배율로 움직이므로 로그가 체감과 맞는다)
        var t = Mathf.InverseLerp(Mathf.Log(minDistance), Mathf.Log(maxDistance), Mathf.Log(Mathf.Max(1f, cameraDistance)));
        return Mathf.Lerp(pitchAtMinDistance, pitchAtMaxDistance, t);
    }

    /// <summary>
    /// 원하는 각도 = 거리별 자동 각도 + 사용자 보정, 한계 범위로 제한.
    /// (보정값을 정한 뒤 확대·축소로 자동 각도가 바뀌어도 한계를 넘지 않게)
    /// </summary>
    float DesiredPitch(float cameraDistance, float cameraPitchOffset)
    {
        return Mathf.Clamp(AutoPitchFor(cameraDistance) + cameraPitchOffset, minPitch, maxPitch);
    }

    /// <summary>
    /// 목표 상태(보간 전)의 카메라 위치·회전. 마우스 광선 계산에 쓴다.
    /// 지형 회피 각도는 보간 없이 바로 적용한 값이며, 실제 카메라도 결국 이 자세로 수렴한다.
    /// </summary>
    void ComputeTargetPose(out Vector3 position, out Quaternion rotation)
    {
        var lookAt = SurfacePoint(focus);
        var pitch = ClearPitch(lookAt, distance, yaw, DesiredPitch(distance, pitchOffset), true);
        PoseFrom(lookAt, distance, yaw, pitch, out position, out rotation);
    }

    /// <summary>
    /// 바라보는 지점에서 시선 반대 방향으로 거리만큼 물러난 카메라 위치와 회전을 구한다.
    /// </summary>
    static void PoseFrom(Vector3 lookAt, float cameraDistance, float cameraYaw, float pitch,
        out Vector3 position, out Quaternion rotation)
    {
        rotation = Quaternion.Euler(pitch, cameraYaw, 0f);
        position = lookAt - rotation * Vector3.forward * cameraDistance;
    }

    /// <summary>
    /// 카메라를 주어진 자세로 놓는다.
    /// </summary>
    void PlaceCamera(Vector3 lookAt, float cameraDistance, float cameraYaw, float pitch)
    {
        PoseFrom(lookAt, cameraDistance, cameraYaw, pitch, out var position, out var rotation);
        transform.SetPositionAndRotation(position, rotation);

        // 가까운 클리핑 거리를 카메라 거리에 비례시켜, 수십 km 밖에서도 깊이 정밀도가 떨어지지 않게 한다.
        // (가까이 있을 때는 작게 해서 바로 앞 지면이 잘리지 않게)
        Camera.nearClipPlane = Mathf.Max(MinNearClip, cameraDistance * NearClipFraction);
    }

    /// <summary>
    /// 바라보는 지점(XZ)을 지면 높이로 올린 점. 지면 정보가 없으면 높이 0.
    /// </summary>
    Vector3 SurfacePoint(Vector3 focusPoint)
    {
        var height = Ground != null ? Ground.SampleHeight(focusPoint) : 0f;
        return new Vector3(focusPoint.x, height, focusPoint.z);
    }

    /// <summary>
    /// 원하는 각도에서 시작해, 지형에 걸리지 않는 가장 낮은 각도를 찾는다.
    /// 먼저 PitchSearchStep 간격으로 훑어 처음 통과하는 구간을 찾고, 그 구간을 반씩 좁혀 정밀하게 맞춘다.
    /// 끝까지 걸리면 maxPitch(거의 바로 위)를 쓴다. 바로 위에서는 카메라가 바라보는 지점보다 거리만큼 높으므로
    /// 거리(최소 minDistance)가 지면 간격보다 크면 항상 통과한다.
    /// </summary>
    /// <param name="checkWholeSight">true 면 바라보는 지점까지의 시선 전체를, false 면 카메라 위치만 검사한다.</param>
    float ClearPitch(Vector3 lookAt, float cameraDistance, float cameraYaw, float desiredPitch, bool checkWholeSight)
    {
        if (Ground == null || IsClear(lookAt, cameraDistance, cameraYaw, desiredPitch, checkWholeSight))
            return desiredPitch;

        var blocked = desiredPitch;
        var clear = maxPitch;
        for (var pitch = desiredPitch + PitchSearchStep; pitch < maxPitch; pitch += PitchSearchStep)
        {
            if (IsClear(lookAt, cameraDistance, cameraYaw, pitch, checkWholeSight))
            {
                clear = pitch;
                break;
            }

            blocked = pitch;
        }

        // blocked(걸림) ~ clear(통과) 구간을 반씩 좁힌다.
        for (var i = 0; i < PitchRefineIterations; i++)
        {
            var middle = (blocked + clear) * 0.5f;
            if (IsClear(lookAt, cameraDistance, cameraYaw, middle, checkWholeSight))
                clear = middle;
            else
                blocked = middle;
        }

        return clear;
    }

    /// <summary>
    /// 주어진 각도에서 지형에 걸리지 않는지 검사한다.
    /// - 카메라 위치만 검사할 때: 카메라가 주변 덮개 높이보다 minClearance 이상 떠 있는지.
    /// - 시선 전체를 검사할 때: 카메라 → 바라보는 지점 시선을 따라 요구 간격(카메라에서 minClearance, 바라보는 지점에서 0)
    ///   이상 떠 있는지. 카메라 가까이(nearCheckLength)는 덮개 높이로 엄격하게, 그 뒤는 실제 지면 높이로 검사한다.
    /// </summary>
    /// <param name="checkWholeSight">true 면 시선 전체, false 면 카메라 위치만 검사</param>
    bool IsClear(Vector3 lookAt, float cameraDistance, float cameraYaw, float pitch, bool checkWholeSight)
    {
        PoseFrom(lookAt, cameraDistance, cameraYaw, pitch, out var cameraPosition, out var rotation);
        if (!checkWholeSight)
            return cameraPosition.y >= EnvelopeHeight(cameraPosition) + minClearance;

        var forward = rotation * Vector3.forward;
        var length = cameraDistance * SightCheckEnd;
        var steps = Mathf.Clamp(Mathf.CeilToInt(length / SightCheckSpacing), 1, MaxSightSamples);
        for (var i = 0; i <= steps; i++)
        {
            var along = length * i / steps;
            var point = cameraPosition + forward * along;
            var ground = along <= nearCheckLength ? EnvelopeHeight(point) : Ground.SampleHeight(point);
            var required = ground + minClearance * (1f - along / cameraDistance);
            if (point.y < required)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 지점 주변(ClearanceProbeRadius) 지면 중 가장 높은 곳의 높이. 봉우리 사이 빈틈을 메운 덮개처럼 동작한다.
    /// </summary>
    float EnvelopeHeight(Vector3 point)
    {
        var highest = Ground.SampleHeight(point);
        foreach (var direction in ProbeDirections)
            highest = Mathf.Max(highest, Ground.SampleHeight(point + direction * ClearanceProbeRadius));
        return highest;
    }

    /// <summary>
    /// 목표 시점에서 화면 좌표를 지나는 광선. (원근 카메라 공식으로 직접 계산해 카메라 Transform 을 건드리지 않는다)
    /// </summary>
    Ray TargetRay(Vector2 screenPosition)
    {
        var rect = Camera.pixelRect;
        var halfHeight = Mathf.Tan(Camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        var halfWidth = halfHeight * Camera.aspect;

        // 화면 좌표 → -1 ~ 1 정규화 좌표 → 카메라 공간 방향
        var nx = (screenPosition.x - rect.x) / rect.width * 2f - 1f;
        var ny = (screenPosition.y - rect.y) / rect.height * 2f - 1f;
        var localDirection = new Vector3(nx * halfWidth, ny * halfHeight, 1f).normalized;

        ComputeTargetPose(out var origin, out var rotation);
        return new Ray(origin, rotation * localDirection);
    }

    /// <summary>
    /// 바라보는 지점을 이동 범위 안으로 제한한다. (높이는 0 으로 고정)
    /// </summary>
    Vector3 ClampFocus(Vector3 value)
    {
        return new Vector3(
            Mathf.Clamp(value.x, panBounds.xMin, panBounds.xMax),
            0f,
            Mathf.Clamp(value.z, panBounds.yMin, panBounds.yMax));
    }
}
