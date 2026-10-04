using System;

/// <summary>
/// 게임 내 시간(날짜)과 진행 속도를 제어하는 시계의 추상화.
/// HUD 나 입력 처리 코드는 구체 구현(GameClock)이 아닌 이 인터페이스에만 의존한다(DIP).
/// </summary>
public interface IGameClock
{
    /// <summary>현재 게임 내 날짜</summary>
    DateTime CurrentDate { get; }

    /// <summary>현재 속도 단계 (1 ~ MaxSpeed)</summary>
    int Speed { get; }

    /// <summary>최대 속도 단계 수</summary>
    int MaxSpeed { get; }

    /// <summary>일시 정지 상태이면 true</summary>
    bool IsPaused { get; }

    /// <summary>날짜가 하루 넘어갈 때마다 새 날짜와 함께 발생한다.</summary>
    event Action<DateTime> DateChanged;

    /// <summary>속도 단계나 일시 정지 상태가 바뀔 때 발생한다.</summary>
    event Action StateChanged;

    /// <summary>
    /// 속도 단계를 바꾼다. 범위를 벗어난 값은 1 ~ MaxSpeed 로 보정된다.
    /// </summary>
    void SetSpeed(int speed);

    /// <summary>
    /// 일시 정지 여부를 설정한다.
    /// </summary>
    void SetPaused(bool paused);

    /// <summary>
    /// 일시 정지 ↔ 진행을 전환한다.
    /// </summary>
    void TogglePause();
}
