using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 여러 선택지 중 하나를 고르는 패널의 공통 기반 클래스.
/// 자식에 있는 OptionButton&lt;T&gt; 들을 모아 클릭을 구독하고,
/// 선택이 확정되면 Chosen, 뒤로가기를 누르면 BackRequested 이벤트를 발생시킨다.
/// 패널은 "무엇이 골라졌는가"만 알리고, 그 다음 흐름은 NewGameFlow 가 결정한다(SRP).
/// </summary>
/// <typeparam name="T">선택 값의 타입</typeparam>
public abstract class ChoicePanel<T> : MonoBehaviour
{
    [Tooltip("이전 단계로 돌아가는 버튼 (없으면 비워둠)")]
    [SerializeField] Button backButton;

    /// <summary>패널 자식에서 수집한 선택지 버튼 목록</summary>
    readonly List<OptionButton<T>> options = new List<OptionButton<T>>();

    /// <summary>선택이 확정되었을 때 선택 값과 함께 발생한다.</summary>
    public event Action<T> Chosen;

    /// <summary>뒤로가기 버튼을 눌렀을 때 발생한다.</summary>
    public event Action BackRequested;

    /// <summary>하위 클래스에서 읽을 수 있는 선택지 버튼 목록</summary>
    protected IReadOnlyList<OptionButton<T>> Options => options;

    /// <summary>
    /// 자식 선택지 버튼을 수집해 클릭 이벤트를 구독하고, 뒤로가기 버튼을 연결한다.
    /// 패널이 처음 활성화될 때 한 번 호출된다.
    /// </summary>
    protected virtual void Awake()
    {
        GetComponentsInChildren(true, options);
        foreach (var option in options)
            option.Clicked += OnOptionClicked;

        if (backButton != null)
            backButton.onClick.AddListener(RaiseBackRequested);
    }

    /// <summary>
    /// 파괴 시 구독했던 이벤트를 모두 해제한다.
    /// </summary>
    protected virtual void OnDestroy()
    {
        foreach (var option in options)
        {
            if (option != null)
                option.Clicked -= OnOptionClicked;
        }

        if (backButton != null)
            backButton.onClick.RemoveListener(RaiseBackRequested);
    }

    /// <summary>
    /// 패널을 화면에 표시한다. 하위 클래스는 표시 시 초기화가 필요하면 재정의한다.
    /// </summary>
    public virtual void Show()
    {
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 패널을 화면에서 숨긴다.
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 선택지 버튼이 클릭되었을 때 호출된다.
    /// 기본 동작은 클릭 즉시 선택을 확정하는 것이며, 미리보기 등이 필요하면 재정의한다.
    /// </summary>
    /// <param name="value">클릭된 버튼의 선택 값</param>
    protected virtual void OnOptionClicked(T value)
    {
        RaiseChosen(value);
    }

    /// <summary>
    /// 선택 확정 이벤트를 발생시킨다.
    /// </summary>
    protected void RaiseChosen(T value)
    {
        Chosen?.Invoke(value);
    }

    /// <summary>
    /// 주어진 값을 가진 버튼만 강조하고 나머지는 강조를 해제한다.
    /// </summary>
    /// <param name="value">강조할 선택 값</param>
    protected void HighlightOnly(T value)
    {
        var comparer = EqualityComparer<T>.Default;
        foreach (var option in options)
            option.SetSelected(comparer.Equals(option.Value, value));
    }

    /// <summary>
    /// 모든 선택지 버튼의 강조를 해제한다.
    /// </summary>
    protected void ClearHighlights()
    {
        foreach (var option in options)
            option.SetSelected(false);
    }

    /// <summary>
    /// 뒤로가기 이벤트를 발생시킨다.
    /// </summary>
    void RaiseBackRequested()
    {
        BackRequested?.Invoke();
    }
}
