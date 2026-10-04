/// <summary>
/// 난이도 선택 버튼. 난이도 이름과 함께 시작 자금, AI 행동 규칙 요약을 표시한다.
/// </summary>
public class DifficultyOptionButton : OptionButton<DifficultyDefinition>
{
    /// <summary>
    /// "난이도 이름 + (작은 글씨) 시작 자금 · AI 규칙" 형식의 라벨을 만든다.
    /// </summary>
    protected override string BuildLabel(DifficultyDefinition difficulty)
    {
        if (difficulty == null)
            return "(난이도 미지정)";

        return $"{difficulty.DisplayName}\n<size=26>시작 자금 {difficulty.StartingFunds:N0}$ · {DescribeAi(difficulty)}</size>";
    }

    /// <summary>
    /// 난이도의 AI 관련 규칙을 한 줄 문장으로 요약한다.
    /// </summary>
    static string DescribeAi(DifficultyDefinition difficulty)
    {
        if (!difficulty.AiCanDeclareWar)
            return "AI가 먼저 전쟁을 선포하지 않음";

        return difficulty.AiReceivesSubsidy
            ? "AI가 보조금을 받고 전쟁을 선포할 수 있음"
            : "AI가 전쟁을 선포할 수 있음";
    }
}
