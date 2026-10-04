using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 오른쪽 위 시간 패널.
/// 게임 내 날짜와 진행 상태를 표시하고, 일시 정지 버튼과 속도 1~5 단계 버튼으로 시계를 조작한다.
/// 시계의 구체 구현이 아닌 IGameClock 에만 의존한다(DIP).
/// </summary>
public class GameClockView : MonoBehaviour
{
    [Header("표시")]
    [Tooltip("날짜를 표시할 텍스트 (예: 393년 1월 1일)")]
    [SerializeField] Text dateText;

    [Tooltip("진행 상태를 표시할 텍스트 (예: 일시 정지됨 / 속도 3)")]
    [SerializeField] Text statusText;

    [Header("버튼")]
    [Tooltip("일시 정지/진행 전환 버튼")]
    [SerializeField] Button pauseButton;

    [Tooltip("속도 1단계부터 순서대로 배치된 속도 버튼들")]
    [SerializeField] Button[] speedButtons;

    [Header("색상")]
    [Tooltip("현재 속도 이하 단계 버튼의 색 (게이지가 찬 느낌)")]
    [SerializeField] Color activeColor = new Color(0.85f, 0.66f, 0.3f);

    [Tooltip("현재 속도보다 높은 단계 버튼의 색")]
    [SerializeField] Color inactiveColor = new Color(0.2f, 0.22f, 0.28f);

    [Tooltip("일시 정지 중일 때 일시 정지 버튼 색")]
    [SerializeField] Color pausedColor = new Color(0.7f, 0.2f, 0.18f);

    /// <summary>연결된 시계</summary>
    IGameClock clock;

    /// <summary>속도 버튼에 등록한 리스너 (해제용으로 보관)</summary>
    UnityEngine.Events.UnityAction[] speedHandlers;

    /// <summary>
    /// 버튼 리스너를 등록한다. 각 속도 버튼은 자신의 단계로 속도를 바꾸고 진행을 재개한다.
    /// </summary>
    void Awake()
    {
        pauseButton.onClick.AddListener(OnPauseClicked);

        speedHandlers = new UnityEngine.Events.UnityAction[speedButtons.Length];
        for (var i = 0; i < speedButtons.Length; i++)
        {
            var level = i + 1; // 클로저 캡처용 지역 변수
            speedHandlers[i] = () => OnSpeedClicked(level);
            speedButtons[i].onClick.AddListener(speedHandlers[i]);
        }
    }

    /// <summary>
    /// 버튼 리스너와 시계 이벤트 구독을 해제한다.
    /// </summary>
    void OnDestroy()
    {
        pauseButton.onClick.RemoveListener(OnPauseClicked);
        for (var i = 0; i < speedButtons.Length && speedHandlers != null; i++)
            speedButtons[i].onClick.RemoveListener(speedHandlers[i]);

        Unbind();
    }

    /// <summary>
    /// 표시·조작할 시계를 연결하고 즉시 화면을 갱신한다.
    /// </summary>
    public void Bind(IGameClock target)
    {
        Unbind();
        clock = target;
        if (clock == null)
            return;

        clock.DateChanged += OnDateChanged;
        clock.StateChanged += RefreshState;
        OnDateChanged(clock.CurrentDate);
        RefreshState();
    }

    /// <summary>
    /// 연결된 시계의 이벤트 구독을 해제한다.
    /// </summary>
    void Unbind()
    {
        if (clock == null)
            return;

        clock.DateChanged -= OnDateChanged;
        clock.StateChanged -= RefreshState;
        clock = null;
    }

    /// <summary>
    /// 날짜 텍스트를 "393년 1월 1일" 형식으로 갱신한다.
    /// </summary>
    void OnDateChanged(DateTime date)
    {
        dateText.text = $"{date.Year}년 {date.Month}월 {date.Day}일";
    }

    /// <summary>
    /// 속도/일시 정지 상태에 맞춰 상태 문구와 버튼 색을 갱신한다.
    /// 현재 속도 이하의 버튼을 모두 강조해 게이지처럼 보이게 한다.
    /// </summary>
    void RefreshState()
    {
        statusText.text = clock.IsPaused ? "일시 정지됨" : $"속도 {clock.Speed}";
        SetButtonColor(pauseButton, clock.IsPaused ? pausedColor : inactiveColor);

        for (var i = 0; i < speedButtons.Length; i++)
            SetButtonColor(speedButtons[i], i < clock.Speed ? activeColor : inactiveColor);
    }

    /// <summary>
    /// 일시 정지 버튼: 정지 ↔ 진행 전환.
    /// </summary>
    void OnPauseClicked()
    {
        clock?.TogglePause();
    }

    /// <summary>
    /// 속도 버튼: 해당 단계로 바꾸고 진행을 재개한다.
    /// </summary>
    void OnSpeedClicked(int level)
    {
        if (clock == null)
            return;

        clock.SetSpeed(level);
        clock.SetPaused(false);
    }

    /// <summary>
    /// 버튼 배경(targetGraphic) 색을 바꾼다.
    /// </summary>
    static void SetButtonColor(Button button, Color color)
    {
        if (button != null && button.targetGraphic != null)
            button.targetGraphic.color = color;
    }
}
