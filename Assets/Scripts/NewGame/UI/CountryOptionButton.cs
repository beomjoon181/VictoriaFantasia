/// <summary>
/// 국가 목록의 버튼. 국가 이름과 국가 난이도를 표시한다.
/// </summary>
public class CountryOptionButton : OptionButton<CountryDefinition>
{
    /// <summary>
    /// "국가 이름 + (작은 글씨) 난이도" 형식의 라벨을 만든다.
    /// </summary>
    protected override string BuildLabel(CountryDefinition country)
    {
        if (country == null)
            return "(국가 미지정)";

        return $"{country.DisplayName}\n<size=24>난이도 {country.Difficulty.ToDisplayName()}</size>";
    }
}
