using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 키보드로 게임 속도를 조작한다. (입력 처리만 담당하고 시계 로직은 IGameClock 에 위임)
/// - Space        : 일시 정지 / 진행 전환
/// - 1 ~ 5        : 해당 속도 단계로 변경 후 진행
/// - + / -        : 속도 한 단계 올림 / 내림 (숫자패드 포함)
/// </summary>
public class GameClockKeyboardInput : MonoBehaviour
{
    [Tooltip("조작할 게임 시계")]
    [SerializeField] GameClock clock;

    /// <summary>숫자 키 1~5 (속도 단계 순서대로)</summary>
    static readonly Key[] SpeedKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5 };

    /// <summary>인터페이스로 다루는 시계 참조</summary>
    IGameClock Clock => clock;

    /// <summary>
    /// 매 프레임 키 입력을 확인해 시계에 명령을 전달한다.
    /// </summary>
    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || clock == null)
            return;

        if (keyboard.spaceKey.wasPressedThisFrame)
            Clock.TogglePause();

        for (var i = 0; i < SpeedKeys.Length && i < Clock.MaxSpeed; i++)
        {
            if (keyboard[SpeedKeys[i]].wasPressedThisFrame)
            {
                Clock.SetSpeed(i + 1);
                Clock.SetPaused(false);
            }
        }

        if (keyboard.equalsKey.wasPressedThisFrame || keyboard.numpadPlusKey.wasPressedThisFrame)
            Clock.SetSpeed(Clock.Speed + 1);

        if (keyboard.minusKey.wasPressedThisFrame || keyboard.numpadMinusKey.wasPressedThisFrame)
            Clock.SetSpeed(Clock.Speed - 1);
    }
}
