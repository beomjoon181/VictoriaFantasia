/// <summary>
/// 플레이어 캐릭터의 성별.
/// </summary>
public enum Gender
{
    /// <summary>남성</summary>
    Male,

    /// <summary>여성</summary>
    Female,
}

/// <summary>
/// Gender 값을 화면 표시용 문자열로 바꿔주는 확장 메서드 모음.
/// </summary>
public static class GenderExtensions
{
    /// <summary>
    /// 성별을 한국어 표시 이름으로 변환한다.
    /// </summary>
    public static string ToDisplayName(this Gender gender)
    {
        switch (gender)
        {
            case Gender.Male: return "남성";
            case Gender.Female: return "여성";
            default: return gender.ToString();
        }
    }
}
