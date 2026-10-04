using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD 버튼으로 여는 화면 창.
/// 현재는 각 화면의 실제 내용이 없으므로, 제목과 "준비 중" 안내만 표시하는 공용 창으로 사용한다.
/// 화면별 내용이 생기면 메뉴마다 전용 창으로 교체하면 된다.
/// </summary>
public class HudMenuWindow : MonoBehaviour
{
    [Tooltip("창 제목 텍스트")]
    [SerializeField] Text titleText;

    [Tooltip("창 본문 텍스트")]
    [SerializeField] Text bodyText;

    [Tooltip("창을 닫는 버튼")]
    [SerializeField] Button closeButton;

    /// <summary>닫기 버튼을 눌렀을 때 발생한다. (실제 닫기는 라우터가 결정)</summary>
    public event Action CloseRequested;

    /// <summary>
    /// 닫기 버튼 리스너를 등록한다.
    /// </summary>
    void Awake()
    {
        closeButton.onClick.AddListener(RaiseCloseRequested);
    }

    /// <summary>
    /// 파괴 시 리스너를 해제한다.
    /// </summary>
    void OnDestroy()
    {
        closeButton.onClick.RemoveListener(RaiseCloseRequested);
    }

    /// <summary>
    /// 지정한 메뉴의 제목으로 창을 연다.
    /// </summary>
    public void Show(HudMenu menu)
    {
        titleText.text = menu.ToDisplayName();
        bodyText.text = $"{menu.ToDisplayName()} 화면은 아직 준비 중입니다.";
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 창을 닫는다.
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 닫기 요청 이벤트를 발생시킨다.
    /// </summary>
    void RaiseCloseRequested()
    {
        CloseRequested?.Invoke();
    }
}
