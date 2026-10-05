using UnityEngine;

/// <summary>
/// 개인 자금 / 국가 예산의 보유 상한 규칙과 개인 자금 초기값을 정의하는 데이터 에셋.
/// 보유 상한은 고정 금액이 아니라 현재 GDP 에 대한 비율로 정해진다.
/// (국가 예산의 초기값은 난이도의 시작 자금을 사용한다)
/// </summary>
[CreateAssetMenu(menuName = "Victoria Fantasia/Funds Rules", fileName = "FundsRules")]
public class FundsRules : ScriptableObject
{
    [Tooltip("게임 시작 시 플레이어 개인 자금($)")]
    [SerializeField, Min(0)] long personalStartingFunds = 500_000;

    [Tooltip("개인 자금 보유 상한 = GDP × 이 비율 (0.1 = GDP의 10%)")]
    [SerializeField, Min(0f)] float personalCapGdpRatio = 0.1f;

    [Tooltip("국가 예산 보유 상한 = GDP × 이 비율 (2 = GDP의 두 배)")]
    [SerializeField, Min(0f)] float nationalBudgetCapGdpRatio = 2f;

    /// <summary>게임 시작 시 개인 자금($)</summary>
    public long PersonalStartingFunds => personalStartingFunds;

    /// <summary>
    /// 주어진 GDP 에서의 개인 자금 보유 상한($)을 계산한다.
    /// </summary>
    public long PersonalFundsCapFor(double gdp)
    {
        return CapFor(gdp, personalCapGdpRatio);
    }

    /// <summary>
    /// 주어진 GDP 에서의 국가 예산 보유 상한($)을 계산한다.
    /// </summary>
    public long NationalBudgetCapFor(double gdp)
    {
        return CapFor(gdp, nationalBudgetCapGdpRatio);
    }

    /// <summary>
    /// GDP × 비율을 0 이상의 정수 금액으로 변환한다.
    /// </summary>
    static long CapFor(double gdp, float ratio)
    {
        return (long)System.Math.Max(0d, System.Math.Floor(gdp * ratio));
    }
}
