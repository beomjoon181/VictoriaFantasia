using System;

/// <summary>
/// 보유 상한이 있는 자금(개인 자금, 국가 예산 등).
/// 금액은 항상 0 이상, 상한 이하로 유지되며, 값이 바뀌면 Changed 이벤트로 알린다.
/// 상한을 넘는 입금분은 버려진다.
/// </summary>
public sealed class CappedFund
{
    /// <summary>현재 보유액($)</summary>
    public long Amount { get; private set; }

    /// <summary>보유 상한($)</summary>
    public long Cap { get; private set; }

    /// <summary>상한 대비 보유 비율 (0~1). 게이지 표시에 사용한다.</summary>
    public float Ratio => Cap <= 0 ? 0f : (float)((double)Amount / Cap);

    /// <summary>상한까지 가득 찼으면 true</summary>
    public bool IsFull => Amount >= Cap;

    /// <summary>보유액이나 상한이 바뀔 때 자기 자신과 함께 발생한다.</summary>
    public event Action<CappedFund> Changed;

    /// <summary>
    /// 잔액보다 많은 금액을 출금하려다 거부되었을 때 (자금, 요청 금액)과 함께 발생한다.
    /// "돈이 없습니다" 같은 안내를 띄우는 데 사용한다.
    /// </summary>
    public event Action<CappedFund, long> WithdrawRejected;

    /// <summary>
    /// 초기 보유액과 상한으로 자금을 만든다. 초기 보유액은 상한 안으로 보정된다.
    /// </summary>
    /// <param name="amount">초기 보유액</param>
    /// <param name="cap">보유 상한 (0 이상)</param>
    public CappedFund(long amount, long cap)
    {
        if (cap < 0)
            throw new ArgumentOutOfRangeException(nameof(cap), "보유 상한은 0 이상이어야 합니다.");

        Cap = cap;
        Amount = Clamp(amount);
    }

    /// <summary>
    /// 입금한다. 상한을 넘는 부분은 버려진다.
    /// </summary>
    /// <param name="amount">입금액 (0 이상)</param>
    /// <returns>실제로 입금된 금액</returns>
    public long Deposit(long amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "입금액은 0 이상이어야 합니다.");

        var before = Amount;
        Amount = Clamp(Amount + amount);
        NotifyIfChanged(before);
        return Amount - before;
    }

    /// <summary>
    /// 잔액이 충분하면 출금하고 true 를 반환한다.
    /// 잔액이 부족하면 출금을 금지(보유액 변화 없음)하고 WithdrawRejected 를 알린 뒤 false 를 반환한다.
    /// </summary>
    /// <param name="amount">출금액 (0 이상)</param>
    public bool TryWithdraw(long amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "출금액은 0 이상이어야 합니다.");
        if (amount > Amount)
        {
            WithdrawRejected?.Invoke(this, amount);
            return false;
        }

        var before = Amount;
        Amount -= amount;
        NotifyIfChanged(before);
        return true;
    }

    /// <summary>
    /// 보유 상한을 바꾼다. 현재 보유액이 새 상한을 넘으면 상한으로 깎인다.
    /// </summary>
    /// <param name="cap">새 보유 상한 (0 이상)</param>
    public void SetCap(long cap)
    {
        if (cap < 0)
            throw new ArgumentOutOfRangeException(nameof(cap), "보유 상한은 0 이상이어야 합니다.");

        Cap = cap;
        Amount = Clamp(Amount);
        Changed?.Invoke(this);
    }

    /// <summary>
    /// 금액을 0 ~ 상한 범위로 보정한다.
    /// </summary>
    long Clamp(long value)
    {
        return Math.Max(0, Math.Min(value, Cap));
    }

    /// <summary>
    /// 보유액이 실제로 바뀐 경우에만 Changed 이벤트를 발생시킨다.
    /// </summary>
    void NotifyIfChanged(long before)
    {
        if (Amount != before)
            Changed?.Invoke(this);
    }
}
