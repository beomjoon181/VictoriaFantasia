using System;

/// <summary>
/// 새 게임 시작 시 플레이어가 고른 선택 결과(난이도, 성별, 국가)를 묶은 불변 객체.
/// 생성 이후에는 값이 바뀌지 않으므로 여러 시스템이 안전하게 공유할 수 있다.
/// </summary>
public sealed class NewGameSettings
{
    /// <summary>선택한 게임 난이도(시작 자금, AI 행동 규칙 포함)</summary>
    public DifficultyDefinition Difficulty { get; }

    /// <summary>선택한 플레이어 성별</summary>
    public Gender Gender { get; }

    /// <summary>플레이할 국가</summary>
    public CountryDefinition Country { get; }

    /// <summary>
    /// 선택 결과로 설정 객체를 만든다. 난이도와 국가는 반드시 지정되어야 한다.
    /// </summary>
    public NewGameSettings(DifficultyDefinition difficulty, Gender gender, CountryDefinition country)
    {
        Difficulty = difficulty != null ? difficulty : throw new ArgumentNullException(nameof(difficulty));
        Gender = gender;
        Country = country != null ? country : throw new ArgumentNullException(nameof(country));
    }

    /// <summary>디버그 로그용 요약 문자열</summary>
    public override string ToString()
    {
        return $"난이도={Difficulty.DisplayName}, 성별={Gender.ToDisplayName()}, 국가={Country.DisplayName}";
    }
}
