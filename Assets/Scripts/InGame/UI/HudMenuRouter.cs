using UnityEngine;

/// <summary>
/// HUD 메뉴 버튼과 화면 창을 연결하는 중재자.
/// - 버튼을 누르면 해당 화면을 연다.
/// - 이미 열린 화면의 버튼을 다시 누르면 닫는다(토글).
/// - 다른 화면 버튼을 누르면 그 화면으로 전환한다.
/// 열린 화면에 해당하는 버튼은 강조 표시한다.
/// </summary>
public class HudMenuRouter : MonoBehaviour
{
    [Tooltip("메뉴 화면을 표시할 창")]
    [SerializeField] HudMenuWindow window;

    [Tooltip("이 라우터가 관리할 HUD 메뉴 버튼들 (왼쪽 중앙 + 아래쪽 중앙)")]
    [SerializeField] HudMenuButton[] buttons;

    /// <summary>현재 열린 화면 (없으면 null)</summary>
    HudMenu? openMenu;

    /// <summary>현재 열린 화면 (없으면 null)</summary>
    public HudMenu? OpenMenu => openMenu;

    /// <summary>
    /// 버튼과 창의 이벤트를 구독하고, 시작 시 창을 닫아 둔다.
    /// </summary>
    void Awake()
    {
        foreach (var button in buttons)
            button.Clicked += Toggle;

        window.CloseRequested += Close;
        window.Hide();
    }

    /// <summary>
    /// 이벤트 구독을 해제한다.
    /// </summary>
    void OnDestroy()
    {
        foreach (var button in buttons)
        {
            if (button != null)
                button.Clicked -= Toggle;
        }

        if (window != null)
            window.CloseRequested -= Close;
    }

    /// <summary>
    /// 같은 화면이 열려 있으면 닫고, 아니면 해당 화면을 연다.
    /// </summary>
    public void Toggle(HudMenu menu)
    {
        if (openMenu == menu)
            Close();
        else
            Open(menu);
    }

    /// <summary>
    /// 지정한 화면을 연다.
    /// </summary>
    public void Open(HudMenu menu)
    {
        openMenu = menu;
        window.Show(menu);
        RefreshButtons();
    }

    /// <summary>
    /// 열린 화면을 닫는다.
    /// </summary>
    public void Close()
    {
        openMenu = null;
        window.Hide();
        RefreshButtons();
    }

    /// <summary>
    /// 열린 화면에 해당하는 버튼만 강조한다.
    /// </summary>
    void RefreshButtons()
    {
        foreach (var button in buttons)
            button.SetActive(openMenu == button.Menu);
    }
}
