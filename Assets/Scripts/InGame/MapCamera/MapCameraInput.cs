using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 키보드·마우스 입력을 지도 카메라 명령으로 바꾼다. (입력 해석만 담당하고 시점 계산은 IMapCameraRig 에 위임)
///
/// 조작:
/// - W A S D / 방향키       : 지도 이동 (화면 기준 앞·뒤·좌·우, Shift 를 누르면 빠르게)
/// - 마우스 오른쪽 드래그    : 잡은 지점을 끌어 지도 이동
/// - 마우스 휠 누르고 이동   : 마우스와 반대 방향으로 카메라 회전
///                            (오른쪽으로 밀면 왼쪽으로, 위로 밀면 시선이 아래쪽으로)
/// - 마우스 휠              : 마우스가 가리키는 곳을 중심으로 확대 / 축소
/// - Q / E                  : 카메라를 왼쪽 / 오른쪽으로 회전 (바라보는 지점을 중심으로 돈다)
/// - R / F                  : 카메라 시선을 위쪽(지평선) / 아래쪽(땅)으로 회전
/// - Home                   : 처음 시점(대륙 전체, 회전 없음)으로 돌아가기
///
/// (Space, 1~5, +/- 는 게임 속도 조작에 쓰이므로 사용하지 않는다)
/// HUD 위에서 휠을 굴리거나 드래그를 시작하면 지도 카메라는 반응하지 않는다.
/// </summary>
public class MapCameraInput : MonoBehaviour
{
    [Tooltip("조작할 지도 카메라 리그")]
    [SerializeField] MapCameraRig rig;

    [Header("키보드 이동")]
    [Tooltip("초당 이동 거리 = 현재 카메라 거리 × 이 값 (멀리서 볼수록 빠르게 이동)")]
    [SerializeField] float keyboardPanSpeed = 0.7f;

    [Tooltip("Shift 를 누를 때 이동 속도 배율")]
    [SerializeField] float fastMultiplier = 2.5f;

    [Header("회전")]
    [Tooltip("Q / E 를 누르고 있을 때 초당 좌우 회전 각도 (도)")]
    [SerializeField] float yawSpeed = 90f;

    [Tooltip("R / F 를 누르고 있을 때 초당 위아래 회전 각도 (도)")]
    [SerializeField] float pitchSpeed = 45f;

    [Tooltip("마우스 휠을 누르고 좌우로 움직일 때 1픽셀당 좌우 회전 각도 (도)")]
    [SerializeField] float mouseYawSensitivity = 0.25f;

    [Tooltip("마우스 휠을 누르고 위아래로 움직일 때 1픽셀당 위아래 회전 각도 (도)")]
    [SerializeField] float mousePitchSensitivity = 0.15f;

    [Header("확대 / 축소")]
    [Tooltip("휠 한 칸에 줄어드는 거리 비율 (0.15 = 15% 가까워짐)")]
    [SerializeField, Range(0.01f, 0.5f)] float wheelZoomStep = 0.15f;

    [Header("드래그")]
    [Tooltip("마우스 오른쪽 버튼 드래그로 이동")]
    [SerializeField] bool dragWithRightButton = true;

    /// <summary>드래그를 시작할 때 잡은 지면 지점 (드래그 중이 아니면 null)</summary>
    Vector3? dragAnchor;

    /// <summary>마우스 휠을 눌러 회전 중인지 (HUD 위에서 누른 경우는 회전하지 않는다)</summary>
    bool isMouseRotating;

    /// <summary>인터페이스로 다루는 리그 참조</summary>
    IMapCameraRig Rig => rig;

    /// <summary>
    /// 매 프레임 입력을 읽어 리그에 명령한다. 리그는 LateUpdate 에서 결과를 반영한다.
    /// </summary>
    void Update()
    {
        if (rig == null)
            return;

        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        if (keyboard != null)
            HandleKeyboard(keyboard);
        if (mouse != null)
            HandleMouse(mouse);
    }

    // ═════════════════════════ 키보드 ═════════════════════════

    /// <summary>
    /// WASD·방향키 이동, Q/E·R/F 회전, Home 시점 초기화.
    /// </summary>
    void HandleKeyboard(Keyboard keyboard)
    {
        var deltaTime = Time.unscaledDeltaTime;

        if (keyboard.homeKey.wasPressedThisFrame)
            Rig.ResetView();

        HandleKeyboardPan(keyboard, deltaTime);
        HandleKeyboardRotation(keyboard, deltaTime);
    }

    /// <summary>
    /// WASD·방향키 이동. 카메라가 돌아가 있어도 W 는 항상 화면 위쪽(카메라가 보는 방향)으로 간다.
    /// </summary>
    void HandleKeyboardPan(Keyboard keyboard, float deltaTime)
    {
        // 화면 기준 입력: x = 오른쪽, y = 앞쪽
        var direction = Vector2.zero;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) direction.y += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) direction.y -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) direction.x += 1f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) direction.x -= 1f;

        if (direction == Vector2.zero)
            return;

        var speed = Rig.Distance * keyboardPanSpeed * (keyboard.shiftKey.isPressed ? fastMultiplier : 1f);
        var move = direction.normalized * (speed * deltaTime); // 대각선이 더 빨라지지 않게 정규화

        // 화면 기준 방향을 카메라의 좌우 회전만큼 돌려 지면(XZ) 방향으로 바꾼다.
        var yawRotation = Quaternion.Euler(0f, Rig.Yaw, 0f);
        Rig.Pan(yawRotation * new Vector3(move.x, 0f, move.y));
    }

    /// <summary>
    /// Q/E 좌우 회전, R/F 위아래 회전.
    /// </summary>
    void HandleKeyboardRotation(Keyboard keyboard, float deltaTime)
    {
        // Q = 왼쪽(반시계), E = 오른쪽(시계)
        var yawInput = 0f;
        if (keyboard.qKey.isPressed) yawInput -= 1f;
        if (keyboard.eKey.isPressed) yawInput += 1f;

        // R = 위쪽(지평선 쪽, 각도 감소), F = 아래쪽(땅 쪽, 각도 증가)
        var pitchInput = 0f;
        if (keyboard.rKey.isPressed) pitchInput -= 1f;
        if (keyboard.fKey.isPressed) pitchInput += 1f;

        if (yawInput != 0f || pitchInput != 0f)
            Rig.Rotate(yawInput * yawSpeed * deltaTime, pitchInput * pitchSpeed * deltaTime);
    }

    // ═════════════════════════ 마우스 ═════════════════════════

    /// <summary>
    /// 휠 확대·축소, 휠 누르고 회전, 오른쪽 드래그 이동.
    /// </summary>
    void HandleMouse(Mouse mouse)
    {
        var pointer = mouse.position.ReadValue();
        HandleWheel(mouse, pointer);
        HandleMouseRotation(mouse);
        HandleDrag(mouse, pointer);
    }

    /// <summary>
    /// 휠(가운데 버튼)을 누른 채 움직이면 마우스와 반대 방향으로 카메라를 돌린다.
    /// - 마우스 오른쪽 이동 → 카메라 왼쪽 회전 (좌우 각도 감소)
    /// - 마우스 위쪽 이동   → 카메라 시선 아래쪽 회전 (내려다보는 각도 증가)
    /// </summary>
    void HandleMouseRotation(Mouse mouse)
    {
        // HUD 위에서 누르기 시작한 경우는 무시한다.
        if (mouse.middleButton.wasPressedThisFrame)
            isMouseRotating = !IsPointerOverUi();
        if (!mouse.middleButton.isPressed)
            isMouseRotating = false;

        if (!isMouseRotating)
            return;

        // delta = 이번 프레임에 마우스가 움직인 픽셀 수 (x 오른쪽 +, y 위쪽 +)
        var delta = mouse.delta.ReadValue();
        if (delta == Vector2.zero)
            return;

        Rig.Rotate(-delta.x * mouseYawSensitivity, delta.y * mousePitchSensitivity);
    }

    /// <summary>
    /// 휠: 마우스가 가리키는 곳을 고정한 채 확대 / 축소. (HUD 위에서는 무시)
    /// </summary>
    void HandleWheel(Mouse mouse, Vector2 pointer)
    {
        var scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Approximately(scroll, 0f) || IsPointerOverUi())
            return;

        // 휠 위로 = 확대. 휠 한 칸 = WheelNotch 만큼의 값.
        var notches = scroll / WheelNotch;
        Rig.Zoom(Mathf.Pow(1f - wheelZoomStep, notches), pointer);
    }

    /// <summary>
    /// 드래그: 누른 순간 마우스 아래 지면 지점을 잡고, 그 지점이 계속 마우스 아래에 있도록 지도를 끈다.
    /// </summary>
    void HandleDrag(Mouse mouse, Vector2 pointer)
    {
        var pressedThisFrame = dragWithRightButton && mouse.rightButton.wasPressedThisFrame;
        var held = dragWithRightButton && mouse.rightButton.isPressed;

        // 드래그 시작 (HUD 위에서 누른 경우는 제외)
        if (pressedThisFrame && dragAnchor == null && !IsPointerOverUi() && Rig.TryGetGroundPoint(pointer, out var grabbed))
            dragAnchor = grabbed;

        if (!held)
        {
            dragAnchor = null;
            return;
        }

        // 잡은 지점과 지금 마우스 아래 지점의 차이만큼 즉시 이동 (보간하면 손에서 미끄러지는 느낌이 난다)
        if (dragAnchor.HasValue && Rig.TryGetGroundPoint(pointer, out var current))
            Rig.Pan(dragAnchor.Value - current, true);
    }

    /// <summary>
    /// 휠 한 칸의 값. Input System 이 플랫폼 간 값을 통일하는 설정이면 1, 아니면 Windows 기준 120.
    /// </summary>
    static float WheelNotch =>
        InputSystem.settings.scrollDeltaBehavior == InputSettings.ScrollDeltaBehavior.UniformAcrossAllPlatforms ? 1f : 120f;

    /// <summary>
    /// 마우스가 HUD(UI) 위에 있는지. EventSystem 이 없으면 UI 가 없다고 본다.
    /// </summary>
    static bool IsPointerOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
