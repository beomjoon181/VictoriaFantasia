using System.Globalization;

/// <summary>
/// HUD 에 표시할 숫자를 짧은 문자열로 바꿔주는 헬퍼.
/// 예) 155,200,000 → "155.2M", 0.551 → "55.1%"
/// </summary>
public static class StatFormatter
{
    /// <summary>
    /// 큰 수를 K(천)/M(백만)/B(십억) 단위의 짧은 문자열로 변환한다.
    /// </summary>
    /// <param name="value">변환할 값</param>
    public static string Compact(double value)
    {
        var abs = System.Math.Abs(value);
        if (abs >= 1e9) return (value / 1e9).ToString("0.0", CultureInfo.InvariantCulture) + "B";
        if (abs >= 1e6) return (value / 1e6).ToString("0.0", CultureInfo.InvariantCulture) + "M";
        if (abs >= 1e3) return (value / 1e3).ToString("0.0", CultureInfo.InvariantCulture) + "K";
        return value.ToString("0", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 0~1 비율을 소수 첫째 자리까지의 백분율 문자열로 변환한다.
    /// </summary>
    /// <param name="ratio">0~1 범위의 비율</param>
    public static string Percent(float ratio)
    {
        return (ratio * 100f).ToString("0.0", CultureInfo.InvariantCulture) + "%";
    }

    /// <summary>
    /// 소수 첫째 자리까지 표시한다.
    /// </summary>
    public static string OneDecimal(float value)
    {
        return value.ToString("0.0", CultureInfo.InvariantCulture);
    }
}
