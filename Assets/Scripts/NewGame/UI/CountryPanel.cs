using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 국가 선택 패널.
/// 국가 버튼을 누르면 즉시 확정하지 않고 화면 중앙에 설명과 '선택' 버튼을 보여주며,
/// '선택' 버튼을 눌렀을 때 비로소 해당 국가로 선택을 확정한다.
/// </summary>
public class CountryPanel : ChoicePanel<CountryDefinition>
{
    [Tooltip("국가 상세 정보(이름/난이도/설명/선택 버튼)를 담는 영역. 국가를 고르기 전에는 숨겨진다.")]
    [SerializeField] GameObject detailRoot;

    [Tooltip("국가를 고르기 전 중앙에 표시할 안내 문구")]
    [SerializeField] GameObject hint;

    [Tooltip("선택한 국가 이름을 표시할 텍스트")]
    [SerializeField] Text nameText;

    [Tooltip("선택한 국가의 난이도를 표시할 텍스트")]
    [SerializeField] Text difficultyText;

    [Tooltip("선택한 국가의 설명을 표시할 텍스트")]
    [SerializeField] Text descriptionText;

    [Tooltip("미리보기 중인 국가로 선택을 확정하는 버튼")]
    [SerializeField] Button confirmButton;

    /// <summary>현재 중앙에 미리보기로 표시 중인 국가 (없으면 null)</summary>
    CountryDefinition previewed;

    /// <summary>
    /// 기본 초기화 후 '선택' 버튼 리스너를 등록한다.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        if (confirmButton != null)
            confirmButton.onClick.AddListener(Confirm);
    }

    /// <summary>
    /// 기본 정리 후 '선택' 버튼 리스너를 해제한다.
    /// </summary>
    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(Confirm);
    }

    /// <summary>
    /// 패널을 표시할 때마다 이전 미리보기를 지우고 안내 문구 상태로 되돌린다.
    /// </summary>
    public override void Show()
    {
        base.Show();
        ClearPreview();
    }

    /// <summary>
    /// 국가 버튼 클릭 시 선택을 확정하지 않고 중앙 상세 영역에 미리보기를 띄운다.
    /// </summary>
    /// <param name="country">클릭된 국가</param>
    protected override void OnOptionClicked(CountryDefinition country)
    {
        if (country == null)
            return;

        previewed = country;
        HighlightOnly(country);

        nameText.text = country.DisplayName;
        difficultyText.text = $"난이도 {country.Difficulty.ToDisplayName()}";
        descriptionText.text = country.Description;

        SetDetailVisible(true);
    }

    /// <summary>
    /// '선택' 버튼: 미리보기 중인 국가로 선택을 확정한다.
    /// </summary>
    void Confirm()
    {
        if (previewed != null)
            RaiseChosen(previewed);
    }

    /// <summary>
    /// 미리보기 상태를 초기화한다(강조 해제, 상세 영역 숨김).
    /// </summary>
    void ClearPreview()
    {
        previewed = null;
        ClearHighlights();
        SetDetailVisible(false);
    }

    /// <summary>
    /// 상세 영역과 안내 문구 중 하나만 보이도록 전환한다.
    /// </summary>
    /// <param name="visible">true 면 상세 영역 표시, false 면 안내 문구 표시</param>
    void SetDetailVisible(bool visible)
    {
        if (detailRoot != null)
            detailRoot.SetActive(visible);
        if (hint != null)
            hint.SetActive(!visible);
    }
}
