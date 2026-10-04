using UnityEngine;

/// <summary>
/// 게임 난이도 하나를 정의하는 데이터 에셋.
/// 새 난이도를 추가할 때 코드 수정 없이 에셋만 추가하면 되도록 데이터로 분리했다(OCP).
/// </summary>
[CreateAssetMenu(menuName = "Victoria Fantasia/Difficulty", fileName = "Difficulty")]
public class DifficultyDefinition : ScriptableObject
{
    [Tooltip("화면에 표시될 난이도 이름 (예: 쉬움)")]
    [SerializeField] string displayName;

    [Tooltip("게임 시작 시 플레이어가 보유하는 자금($)")]
    [SerializeField, Min(0)] long startingFunds;

    [Tooltip("AI 국가가 먼저 전쟁을 선포할 수 있는지 여부")]
    [SerializeField] bool aiCanDeclareWar;

    [Tooltip("AI 국가가 추가 보조금을 받는지 여부")]
    [SerializeField] bool aiReceivesSubsidy;

    /// <summary>화면 표시용 난이도 이름</summary>
    public string DisplayName => displayName;

    /// <summary>시작 자금($)</summary>
    public long StartingFunds => startingFunds;

    /// <summary>AI 가 먼저 전쟁을 선포할 수 있으면 true</summary>
    public bool AiCanDeclareWar => aiCanDeclareWar;

    /// <summary>AI 가 보조금을 받으면 true</summary>
    public bool AiReceivesSubsidy => aiReceivesSubsidy;
}
