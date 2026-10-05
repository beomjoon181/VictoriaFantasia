using System;

/// <summary>
/// 국가 GDP 가 바뀔 때마다 개인 자금 / 국가 예산의 보유 상한을 다시 계산해 적용한다.
/// - 개인 자금 상한 = GDP × FundsRules 의 개인 비율 (기본 10%)
/// - 국가 예산 상한 = GDP × FundsRules 의 예산 비율 (기본 200%)
/// 상한 계산 규칙(FundsRules)과 자금 보관(CappedFund)을 연결하는 역할만 맡는다(SRP).
/// </summary>
public sealed class FundCapUpdater : IDisposable
{
    /// <summary>상한을 갱신할 대상 국가</summary>
    readonly PlayerNation nation;

    /// <summary>상한 계산 규칙</summary>
    readonly FundsRules rules;

    /// <summary>
    /// 현재 GDP 기준으로 상한을 즉시 적용하고, 이후 지표 변경을 구독한다.
    /// </summary>
    /// <param name="nation">플레이어 국가</param>
    /// <param name="rules">자금 규칙</param>
    public FundCapUpdater(PlayerNation nation, FundsRules rules)
    {
        this.nation = nation ?? throw new ArgumentNullException(nameof(nation));
        this.rules = rules != null ? rules : throw new ArgumentNullException(nameof(rules));

        Apply(nation.Stats);
        nation.StatsChanged += Apply;
    }

    /// <summary>
    /// 지표 변경 구독을 해제한다.
    /// </summary>
    public void Dispose()
    {
        nation.StatsChanged -= Apply;
    }

    /// <summary>
    /// 지표의 GDP 로 두 자금의 상한을 다시 계산해 적용한다.
    /// 상한이 줄어 보유액이 넘치면 CappedFund 가 상한까지 깎는다.
    /// </summary>
    void Apply(NationStats stats)
    {
        nation.PersonalFunds.SetCap(rules.PersonalFundsCapFor(stats.Gdp));
        nation.NationalBudget.SetCap(rules.NationalBudgetCapFor(stats.Gdp));
    }
}
