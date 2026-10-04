/// <summary>
/// 성별 선택 버튼. 성별의 한국어 이름을 라벨로 표시한다.
/// </summary>
public class GenderOptionButton : OptionButton<Gender>
{
    /// <summary>
    /// 성별 표시 이름을 라벨 문구로 사용한다.
    /// </summary>
    protected override string BuildLabel(Gender gender)
    {
        return gender.ToDisplayName();
    }
}
