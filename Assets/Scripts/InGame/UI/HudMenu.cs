/// <summary>
/// HUD 버튼으로 열 수 있는 게임 화면(메뉴)의 종류.
/// 왼쪽 중앙 7개 + 아래쪽 중앙 3개로 구성된다.
/// </summary>
public enum HudMenu
{
    // ── 왼쪽 중앙 ──
    /// <summary>정부</summary>
    Government,
    /// <summary>정치</summary>
    Politics,
    /// <summary>건물</summary>
    Buildings,
    /// <summary>외교</summary>
    Diplomacy,
    /// <summary>시장</summary>
    Market,
    /// <summary>군사</summary>
    Military,
    /// <summary>기술</summary>
    Technology,

    // ── 아래쪽 중앙 ──
    /// <summary>구역</summary>
    Regions,
    /// <summary>건설</summary>
    Construction,
    /// <summary>통계</summary>
    Statistics,
}

/// <summary>
/// HudMenu 값을 화면 표시용 문자열로 바꿔주는 확장 메서드 모음.
/// </summary>
public static class HudMenuExtensions
{
    /// <summary>
    /// 메뉴를 한국어 표시 이름으로 변환한다.
    /// </summary>
    public static string ToDisplayName(this HudMenu menu)
    {
        switch (menu)
        {
            case HudMenu.Government: return "정부";
            case HudMenu.Politics: return "정치";
            case HudMenu.Buildings: return "건물";
            case HudMenu.Diplomacy: return "외교";
            case HudMenu.Market: return "시장";
            case HudMenu.Military: return "군사";
            case HudMenu.Technology: return "기술";
            case HudMenu.Regions: return "구역";
            case HudMenu.Construction: return "건설";
            case HudMenu.Statistics: return "통계";
            default: return menu.ToString();
        }
    }
}
