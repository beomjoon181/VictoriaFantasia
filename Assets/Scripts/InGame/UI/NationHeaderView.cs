using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 왼쪽 위 국가 정보 바.
/// 국기와 GDP, 식자율, 평균 교육수준, 생활수준, 인구 수, 악명을 표시한다.
/// 표시만 담당하며, 값은 PlayerNation 의 StatsChanged 이벤트로 받아 갱신한다.
/// </summary>
public class NationHeaderView : MonoBehaviour
{
    [Header("국기")]
    [Tooltip("국기 이미지(또는 국기 색)를 표시할 Image")]
    [SerializeField] Image flagImage;

    [Tooltip("국기 이미지가 없을 때 국가 이름 첫 글자를 표시할 텍스트")]
    [SerializeField] Text flagInitial;

    [Header("지표 값 텍스트")]
    [SerializeField] Text gdpText;
    [SerializeField] Text literacyText;
    [SerializeField] Text educationText;
    [SerializeField] Text standardOfLivingText;
    [SerializeField] Text populationText;
    [SerializeField] Text infamyText;

    /// <summary>현재 표시 중인 국가 (구독 해제를 위해 보관)</summary>
    PlayerNation nation;

    /// <summary>
    /// 표시할 국가를 연결하고 즉시 화면을 갱신한다. 이전에 연결된 국가가 있으면 구독을 해제한다.
    /// </summary>
    /// <param name="target">표시할 플레이어 국가</param>
    public void Bind(PlayerNation target)
    {
        Unbind();
        nation = target;
        if (nation == null)
            return;

        nation.StatsChanged += Refresh;
        ShowFlag(nation.Country);
        Refresh(nation.Stats);
    }

    /// <summary>
    /// 파괴 시 이벤트 구독을 해제한다.
    /// </summary>
    void OnDestroy()
    {
        Unbind();
    }

    /// <summary>
    /// 연결된 국가의 이벤트 구독을 해제한다.
    /// </summary>
    void Unbind()
    {
        if (nation != null)
            nation.StatsChanged -= Refresh;
        nation = null;
    }

    /// <summary>
    /// 국기 이미지가 있으면 이미지를, 없으면 국기 색과 국가 이름 첫 글자를 표시한다.
    /// </summary>
    void ShowFlag(CountryDefinition country)
    {
        var hasSprite = country.Flag != null;
        flagImage.sprite = country.Flag;
        flagImage.color = hasSprite ? Color.white : country.FlagColor;

        flagInitial.gameObject.SetActive(!hasSprite);
        flagInitial.text = string.IsNullOrEmpty(country.DisplayName) ? "?" : country.DisplayName.Substring(0, 1);
    }

    /// <summary>
    /// 지표 스냅샷을 각 텍스트에 반영한다.
    /// </summary>
    void Refresh(NationStats stats)
    {
        gdpText.text = "$" + StatFormatter.Compact(stats.Gdp);
        literacyText.text = StatFormatter.Percent(stats.Literacy);
        educationText.text = StatFormatter.OneDecimal(stats.AverageEducation);
        standardOfLivingText.text = StatFormatter.OneDecimal(stats.StandardOfLiving);
        populationText.text = StatFormatter.Compact(stats.Population);
        infamyText.text = StatFormatter.OneDecimal(stats.Infamy);
    }
}
