using System;

/// <summary>
/// 플레이어가 조종하는 국가의 런타임 상태.
/// 국가 정의(정적 데이터), 현재 지표, 개인 자금과 국가 예산을 함께 보관하고,
/// 지표가 바뀌면 StatsChanged 이벤트로 HUD 등에 알린다.
/// (자금 변화는 각 CappedFund 의 Changed 이벤트로 알린다)
/// </summary>
public sealed class PlayerNation
{
    /// <summary>플레이 중인 국가의 정적 정의(이름, 국기 등)</summary>
    public CountryDefinition Country { get; }

    /// <summary>현재 국가 지표</summary>
    public NationStats Stats { get; private set; }

    /// <summary>플레이어(통치자) 개인 자금</summary>
    public CappedFund PersonalFunds { get; }

    /// <summary>국가 예산</summary>
    public CappedFund NationalBudget { get; }

    /// <summary>지표가 갱신될 때 새 지표와 함께 발생한다.</summary>
    public event Action<NationStats> StatsChanged;

    /// <summary>
    /// 국가 정의의 초기 지표와 주어진 자금으로 런타임 상태를 만든다.
    /// </summary>
    /// <param name="country">플레이할 국가. null 일 수 없다.</param>
    /// <param name="personalFunds">개인 자금. null 일 수 없다.</param>
    /// <param name="nationalBudget">국가 예산. null 일 수 없다.</param>
    public PlayerNation(CountryDefinition country, CappedFund personalFunds, CappedFund nationalBudget)
    {
        Country = country != null ? country : throw new ArgumentNullException(nameof(country));
        PersonalFunds = personalFunds ?? throw new ArgumentNullException(nameof(personalFunds));
        NationalBudget = nationalBudget ?? throw new ArgumentNullException(nameof(nationalBudget));
        Stats = country.StartingStats;
    }

    /// <summary>
    /// 지표를 새 스냅샷으로 교체하고 구독자에게 알린다.
    /// 경제/인구 시뮬레이션 시스템이 매 틱 이 메서드를 호출하는 것을 전제로 한다.
    /// </summary>
    public void UpdateStats(NationStats stats)
    {
        Stats = stats;
        StatsChanged?.Invoke(stats);
    }
}
