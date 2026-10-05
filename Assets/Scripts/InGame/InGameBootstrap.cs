using UnityEngine;

/// <summary>
/// InGame 씬의 진입점(조립 루트).
/// GameSession 에서 새 게임 설정을 읽어 플레이어 국가 상태를 만들고,
/// HUD 뷰들에 국가와 시계를 연결한다. 각 뷰는 서로를 모르고 이 클래스만 조립을 책임진다.
/// </summary>
public class InGameBootstrap : MonoBehaviour
{
    [Header("시스템")]
    [Tooltip("게임 내 시간을 진행시키는 시계")]
    [SerializeField] GameClock clock;

    [Header("HUD")]
    [Tooltip("왼쪽 위 국가 정보 바")]
    [SerializeField] NationHeaderView nationHeader;

    [Tooltip("오른쪽 위 날짜/속도 패널")]
    [SerializeField] GameClockView clockView;

    [Tooltip("왼쪽 위 바의 개인 자금 칸")]
    [SerializeField] CappedFundView personalFundsView;

    [Tooltip("왼쪽 위 바의 국가 예산 칸")]
    [SerializeField] CappedFundView nationalBudgetView;

    [Tooltip("화면 상단 중앙 안내 메시지(토스트)")]
    [SerializeField] HudToastView toastView;

    [Header("규칙 데이터")]
    [Tooltip("개인 자금/국가 예산의 보유 상한(GDP 비율)과 개인 자금 초기값")]
    [SerializeField] FundsRules fundsRules;

    [Header("에디터 테스트용 기본값")]
    [Tooltip("NewGame 을 거치지 않고 InGame 씬을 바로 실행했을 때 사용할 난이도")]
    [SerializeField] DifficultyDefinition fallbackDifficulty;

    [Tooltip("NewGame 을 거치지 않고 InGame 씬을 바로 실행했을 때 사용할 국가")]
    [SerializeField] CountryDefinition fallbackCountry;

    [Tooltip("NewGame 을 거치지 않고 InGame 씬을 바로 실행했을 때 사용할 성별")]
    [SerializeField] Gender fallbackGender;

    /// <summary>플레이어가 조종하는 국가의 런타임 상태</summary>
    public PlayerNation PlayerNation { get; private set; }

    /// <summary>GDP 변화에 맞춰 자금 상한을 갱신하는 객체 (해제용 보관)</summary>
    FundCapUpdater fundCapUpdater;

    /// <summary>잔액 부족 시 안내를 띄우는 객체 (해제용 보관)</summary>
    InsufficientFundsNotifier insufficientFundsNotifier;

    /// <summary>
    /// 모든 컴포넌트의 Awake(시계의 시작 날짜 초기화 포함)가 끝난 뒤 조립한다.
    /// </summary>
    void Start()
    {
        var settings = ResolveSettings();
        var startingGdp = settings.Country.StartingStats.Gdp;
        PlayerNation = new PlayerNation(settings.Country,
            CreatePersonalFunds(startingGdp), CreateNationalBudget(settings, startingGdp));

        // 이후 GDP 가 바뀌면 상한도 따라 바뀐다.
        fundCapUpdater = new FundCapUpdater(PlayerNation, fundsRules);
        insufficientFundsNotifier = new InsufficientFundsNotifier(toastView,
            PlayerNation.PersonalFunds, PlayerNation.NationalBudget);

        nationHeader.Bind(PlayerNation);
        personalFundsView.Bind(PlayerNation.PersonalFunds);
        nationalBudgetView.Bind(PlayerNation.NationalBudget);
        clockView.Bind(clock);
    }

    /// <summary>
    /// 씬 종료 시 연결 객체들의 이벤트 구독을 해제한다.
    /// </summary>
    void OnDestroy()
    {
        fundCapUpdater?.Dispose();
        insufficientFundsNotifier?.Dispose();
    }

    /// <summary>
    /// 개인 자금: 규칙 데이터의 초기값, 시작 GDP 기준 상한(GDP의 10%)으로 만든다.
    /// </summary>
    CappedFund CreatePersonalFunds(double gdp)
    {
        return new CappedFund(fundsRules.PersonalStartingFunds, fundsRules.PersonalFundsCapFor(gdp));
    }

    /// <summary>
    /// 국가 예산: 난이도의 시작 자금을 초기값으로, 시작 GDP 기준 상한(GDP의 두 배)으로 만든다.
    /// </summary>
    CappedFund CreateNationalBudget(NewGameSettings settings, double gdp)
    {
        return new CappedFund(settings.Difficulty.StartingFunds, fundsRules.NationalBudgetCapFor(gdp));
    }

    /// <summary>
    /// 현재 세션 설정을 가져온다. NewGame 을 거치지 않아 세션이 비어 있으면
    /// 에디터 테스트용 기본값으로 세션을 시작한다.
    /// </summary>
    NewGameSettings ResolveSettings()
    {
        if (GameSession.Current != null)
            return GameSession.Current;

        Debug.LogWarning("GameSession 이 비어 있어 테스트용 기본 설정으로 시작합니다. (NewGame 씬을 거치지 않음)");
        var fallback = new NewGameSettings(fallbackDifficulty, fallbackGender, fallbackCountry);
        GameSession.Begin(fallback);
        return fallback;
    }
}
