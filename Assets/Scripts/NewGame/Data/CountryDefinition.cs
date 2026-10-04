using UnityEngine;

/// <summary>
/// 플레이 가능한 국가 하나를 정의하는 데이터 에셋.
/// 이름, 국가 난이도, 배경 설명을 담으며, 국가 추가 시 에셋만 늘리면 된다(OCP).
/// </summary>
[CreateAssetMenu(menuName = "Victoria Fantasia/Country", fileName = "Country")]
public class CountryDefinition : ScriptableObject
{
    [Tooltip("화면에 표시될 국가 이름 (예: 아르케니아 제국)")]
    [SerializeField] string displayName;

    [Tooltip("이 국가로 플레이할 때의 체감 난이도")]
    [SerializeField] CountryDifficulty difficulty;

    [Tooltip("국가 선택 화면 중앙에 표시될 배경 설명")]
    [SerializeField, TextArea(5, 20)] string description;

    [Header("국기")]
    [Tooltip("국기 이미지. 비워두면 국기 색 + 국가 이름 첫 글자로 대신 표시한다.")]
    [SerializeField] Sprite flag;

    [Tooltip("국기 이미지가 없을 때 사용할 대표 색")]
    [SerializeField] Color flagColor = Color.gray;

    [Header("게임 시작 시 국가 지표")]
    [Tooltip("InGame 시작 시점의 GDP, 식자율, 교육, 생활수준, 인구, 악명")]
    [SerializeField] NationStats startingStats;

    /// <summary>화면 표시용 국가 이름</summary>
    public string DisplayName => displayName;

    /// <summary>국가 체감 난이도</summary>
    public CountryDifficulty Difficulty => difficulty;

    /// <summary>국가 배경 설명</summary>
    public string Description => description;

    /// <summary>국기 이미지 (없으면 null)</summary>
    public Sprite Flag => flag;

    /// <summary>국기 대표 색 (국기 이미지가 없을 때 사용)</summary>
    public Color FlagColor => flagColor;

    /// <summary>게임 시작 시 국가 지표</summary>
    public NationStats StartingStats => startingStats;
}
