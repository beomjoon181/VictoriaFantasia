using System;
using UnityEngine;

/// <summary>
/// 게임 내 날짜를 실제 시간에 따라 하루씩 진행시키는 시계.
/// 속도는 5단계이며, 단계별로 "하루가 지나는 데 걸리는 실제 시간(초)"을 설정할 수 있다.
/// 게임은 일시 정지 상태로 시작한다.
/// </summary>
public class GameClock : MonoBehaviour, IGameClock
{
    /// <summary>한 프레임에 진행할 수 있는 최대 일수 (프레임 지연 시 폭주 방지)</summary>
    const int MaxDaysPerFrame = 10;

    [Header("시작 날짜")]
    [Tooltip("게임 시작 연도")]
    [SerializeField, Min(1)] int startYear = 393;

    [Tooltip("게임 시작 월")]
    [SerializeField, Range(1, 12)] int startMonth = 1;

    [Tooltip("게임 시작 일")]
    [SerializeField, Range(1, 31)] int startDay = 1;

    [Header("속도")]
    [Tooltip("속도 단계별로 하루가 지나는 데 걸리는 실제 시간(초). 배열 길이가 곧 속도 단계 수다.")]
    [SerializeField] float[] secondsPerDay = { 2f, 1f, 0.5f, 0.2f, 0.05f };

    [Tooltip("게임 시작 시 일시 정지 상태로 둘지 여부")]
    [SerializeField] bool startPaused = true;

    /// <summary>현재 날짜</summary>
    DateTime currentDate;

    /// <summary>현재 속도 단계 (1부터 시작)</summary>
    int speed = 1;

    /// <summary>일시 정지 여부</summary>
    bool paused;

    /// <summary>다음 날로 넘어가기까지 누적된 실제 시간(초)</summary>
    float elapsed;

    /// <inheritdoc/>
    public DateTime CurrentDate => currentDate;

    /// <inheritdoc/>
    public int Speed => speed;

    /// <inheritdoc/>
    public int MaxSpeed => secondsPerDay.Length;

    /// <inheritdoc/>
    public bool IsPaused => paused;

    /// <inheritdoc/>
    public event Action<DateTime> DateChanged;

    /// <inheritdoc/>
    public event Action StateChanged;

    /// <summary>
    /// 시작 날짜와 일시 정지 상태를 초기화한다.
    /// </summary>
    void Awake()
    {
        currentDate = new DateTime(startYear, startMonth, Mathf.Min(startDay, DateTime.DaysInMonth(startYear, startMonth)));
        paused = startPaused;
    }

    /// <summary>
    /// 일시 정지가 아니면 경과 시간을 누적해, 현재 속도 기준 하루치가 쌓일 때마다 날짜를 넘긴다.
    /// </summary>
    void Update()
    {
        if (paused || MaxSpeed == 0)
            return;

        elapsed += Time.deltaTime;
        var dayLength = Mathf.Max(0.001f, secondsPerDay[speed - 1]);

        var advanced = 0;
        while (elapsed >= dayLength && advanced < MaxDaysPerFrame)
        {
            elapsed -= dayLength;
            advanced++;
            currentDate = currentDate.AddDays(1);
            DateChanged?.Invoke(currentDate);
        }

        // 프레임이 크게 밀려 상한에 걸렸다면 남은 누적분은 버린다.
        if (advanced >= MaxDaysPerFrame)
            elapsed = 0f;
    }

    /// <inheritdoc/>
    public void SetSpeed(int newSpeed)
    {
        newSpeed = Mathf.Clamp(newSpeed, 1, Mathf.Max(1, MaxSpeed));
        if (newSpeed == speed)
            return;

        speed = newSpeed;
        StateChanged?.Invoke();
    }

    /// <inheritdoc/>
    public void SetPaused(bool value)
    {
        if (value == paused)
            return;

        paused = value;
        StateChanged?.Invoke();
    }

    /// <inheritdoc/>
    public void TogglePause()
    {
        SetPaused(!paused);
    }
}
