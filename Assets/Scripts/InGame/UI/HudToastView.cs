using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 상단 중앙에 잠깐 떠올랐다가 서서히 사라지는 안내 메시지(토스트).
/// 일시 정지 중에도 사라지도록 실제 시간(unscaled)을 기준으로 동작한다.
/// </summary>
public class HudToastView : MonoBehaviour, INotificationPresenter
{
    [Tooltip("투명도 조절용 CanvasGroup")]
    [SerializeField] CanvasGroup group;

    [Tooltip("메시지 문구 텍스트")]
    [SerializeField] Text messageText;

    [Tooltip("완전히 보이는 상태로 유지되는 시간(초)")]
    [SerializeField, Min(0f)] float displaySeconds = 1.6f;

    [Tooltip("사라지는 데 걸리는 시간(초)")]
    [SerializeField, Min(0.01f)] float fadeSeconds = 0.4f;

    /// <summary>완전히 사라질 때까지 남은 시간(초)</summary>
    float remaining;

    /// <summary>
    /// 시작 시 보이지 않게 하고, 클릭을 가로막지 않도록 한다.
    /// </summary>
    void Awake()
    {
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
    }

    /// <summary>
    /// 메시지를 띄운다. 이미 떠 있으면 문구를 바꾸고 표시 시간을 처음부터 다시 잰다.
    /// </summary>
    public void Show(string message)
    {
        messageText.text = message;
        remaining = displaySeconds + fadeSeconds;
        group.alpha = 1f;
    }

    /// <summary>
    /// 남은 시간을 줄이고, 마지막 fadeSeconds 동안 투명도를 0까지 낮춘다.
    /// </summary>
    void Update()
    {
        if (remaining <= 0f)
            return;

        remaining -= Time.unscaledDeltaTime;
        group.alpha = Mathf.Clamp01(remaining / fadeSeconds);
    }
}
