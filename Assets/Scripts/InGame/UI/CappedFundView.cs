using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보유 상한이 있는 자금 하나(개인 자금 또는 국가 예산)를 표시하는 HUD 칸.
/// "현재액 / 상한" 문구와 상한 대비 비율 게이지를 보여주며,
/// 상한에 도달하면 게이지 색을 바꿔 더 이상 쌓이지 않음을 알린다.
/// </summary>
public class CappedFundView : MonoBehaviour
{
    [Tooltip("\"$현재액 / 상한\" 을 표시할 텍스트")]
    [SerializeField] Text valueText;

    [Tooltip("게이지 채움 영역. 가로 앵커(anchorMax.x)를 비율로 조절해 채운다.")]
    [SerializeField] RectTransform gaugeFill;

    [Tooltip("게이지 채움 이미지 (색 변경용)")]
    [SerializeField] Image gaugeFillImage;

    [Tooltip("평상시 게이지 색")]
    [SerializeField] Color normalColor = new Color(0.85f, 0.66f, 0.3f);

    [Tooltip("상한에 도달했을 때 게이지 색")]
    [SerializeField] Color fullColor = new Color(0.85f, 0.32f, 0.24f);

    /// <summary>현재 표시 중인 자금 (구독 해제용)</summary>
    CappedFund fund;

    /// <summary>
    /// 표시할 자금을 연결하고 즉시 갱신한다. 이전 연결은 해제한다.
    /// </summary>
    /// <param name="target">표시할 자금</param>
    public void Bind(CappedFund target)
    {
        Unbind();
        fund = target;
        if (fund == null)
            return;

        fund.Changed += Refresh;
        Refresh(fund);
    }

    /// <summary>
    /// 파괴 시 이벤트 구독을 해제한다.
    /// </summary>
    void OnDestroy()
    {
        Unbind();
    }

    /// <summary>
    /// 연결된 자금의 이벤트 구독을 해제한다.
    /// </summary>
    void Unbind()
    {
        if (fund != null)
            fund.Changed -= Refresh;
        fund = null;
    }

    /// <summary>
    /// 금액 문구와 게이지 길이·색을 자금 상태에 맞춘다.
    /// 상한은 작은 글씨로 덧붙여 현재액이 먼저 눈에 띄게 한다.
    /// </summary>
    void Refresh(CappedFund current)
    {
        valueText.text = $"${StatFormatter.Compact(current.Amount)}<size=18> / {StatFormatter.Compact(current.Cap)}</size>";

        gaugeFill.anchorMax = new Vector2(Mathf.Clamp01(current.Ratio), 1f);
        gaugeFill.offsetMin = gaugeFill.offsetMax = Vector2.zero;
        gaugeFillImage.color = current.IsFull ? fullColor : normalColor;
    }
}
