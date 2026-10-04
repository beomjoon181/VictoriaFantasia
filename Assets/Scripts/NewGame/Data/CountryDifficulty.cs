/// <summary>
/// 국가별 플레이 난이도 등급. 게임 난이도(DifficultyDefinition)와는 별개로,
/// 해당 국가로 플레이할 때 체감되는 어려움을 안내하기 위한 값이다.
/// </summary>
public enum CountryDifficulty
{
    /// <summary>쉬움</summary>
    Easy,

    /// <summary>보통</summary>
    Normal,

    /// <summary>어려움</summary>
    Hard,

    /// <summary>매우 어려움</summary>
    VeryHard,
}

/// <summary>
/// CountryDifficulty 값을 화면 표시용 문자열로 바꿔주는 확장 메서드 모음.
/// </summary>
public static class CountryDifficultyExtensions
{
    /// <summary>
    /// 국가 난이도를 한국어 표시 이름으로 변환한다.
    /// </summary>
    public static string ToDisplayName(this CountryDifficulty difficulty)
    {
        switch (difficulty)
        {
            case CountryDifficulty.Easy: return "쉬움";
            case CountryDifficulty.Normal: return "보통";
            case CountryDifficulty.Hard: return "어려움";
            case CountryDifficulty.VeryHard: return "매우 어려움";
            default: return difficulty.ToString();
        }
    }
}
