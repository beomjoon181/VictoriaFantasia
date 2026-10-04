using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 특정 게임 화면(HudMenu)을 여는 HUD 버튼.
/// 클릭되면 자신이 담당하는 메뉴 값과 함께 Clicked 이벤트를 발생시키고,
/// 해당 화면이 열려 있는 동안 강조 색으로 표시된다.
/// </summary>
[RequireComponent(typeof(Button))]
public class HudMenuButton : MonoBehaviour
{
    [Tooltip("이 버튼이 여는 화면")]
    [SerializeField] HudMenu menu;

    [Tooltip("기본 배경색")]
    [SerializeField] Color normalColor = new Color(0.16f, 0.17f, 0.22f);

    [Tooltip("담당 화면이 열려 있을 때의 배경색")]
    [SerializeField] Color activeColor = new Color(0.6f, 0.45f, 0.2f);

    /// <summary>클릭을 감지할 버튼</summary>
    Button button;

    /// <summary>클릭 시 담당 메뉴 값과 함께 발생한다.</summary>
    public event Action<HudMenu> Clicked;

    /// <summary>이 버튼이 여는 화면</summary>
    public HudMenu Menu => menu;

    /// <summary>
    /// 버튼 클릭 리스너를 등록한다.
    /// </summary>
    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(HandleClick);
        SetActive(false);
    }

    /// <summary>
    /// 파괴 시 리스너를 해제한다.
    /// </summary>
    void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleClick);
    }

    /// <summary>
    /// 담당 화면이 열려 있는지에 따라 강조 색을 적용한다.
    /// </summary>
    public void SetActive(bool active)
    {
        if (button == null)
            button = GetComponent<Button>();
        if (button.targetGraphic != null)
            button.targetGraphic.color = active ? activeColor : normalColor;
    }

    /// <summary>
    /// Button.onClick 을 메뉴 값이 포함된 Clicked 이벤트로 전달한다.
    /// </summary>
    void HandleClick()
    {
        Clicked?.Invoke(menu);
    }
}
