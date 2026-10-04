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

    /// <summary>화면 표시용 국가 이름</summary>
    public string DisplayName => displayName;

    /// <summary>국가 체감 난이도</summary>
    public CountryDifficulty Difficulty => difficulty;

    /// <summary>국가 배경 설명</summary>
    public string Description => description;
}
