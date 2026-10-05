using System;
using System.Collections.Generic;

/// <summary>
/// 자금 출금이 잔액 부족으로 거부되면 "돈이 없습니다" 안내를 띄운다.
/// 자금(CappedFund)의 거부 이벤트와 안내 표시 수단(INotificationPresenter)을 연결하는 역할만 맡는다.
/// </summary>
public sealed class InsufficientFundsNotifier : IDisposable
{
    /// <summary>잔액 부족 시 표시할 문구</summary>
    public const string Message = "돈이 없습니다";

    /// <summary>안내를 띄울 표시 수단</summary>
    readonly INotificationPresenter presenter;

    /// <summary>구독 중인 자금 목록 (해제용)</summary>
    readonly List<CappedFund> funds = new List<CappedFund>();

    /// <summary>
    /// 지정한 자금들의 출금 거부를 감시한다.
    /// </summary>
    /// <param name="presenter">안내 표시 수단</param>
    /// <param name="watchedFunds">감시할 자금들 (개인 자금, 국가 예산 등)</param>
    public InsufficientFundsNotifier(INotificationPresenter presenter, params CappedFund[] watchedFunds)
    {
        this.presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));

        foreach (var fund in watchedFunds)
        {
            if (fund == null)
                continue;
            fund.WithdrawRejected += OnWithdrawRejected;
            funds.Add(fund);
        }
    }

    /// <summary>
    /// 모든 자금의 이벤트 구독을 해제한다.
    /// </summary>
    public void Dispose()
    {
        foreach (var fund in funds)
            fund.WithdrawRejected -= OnWithdrawRejected;
        funds.Clear();
    }

    /// <summary>
    /// 출금 거부 시 안내 문구를 띄운다.
    /// </summary>
    void OnWithdrawRejected(CappedFund fund, long requested)
    {
        presenter.Show(Message);
    }
}
