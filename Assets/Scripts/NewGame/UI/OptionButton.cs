using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 선택지 하나(난이도/성별/국가 등)를 나타내는 버튼의 공통 기반 클래스.
/// 버튼 클릭을 "값 T 가 클릭됨" 이벤트로 변환하고, 선택 강조 표시와 라벨 갱신을 담당한다.
/// 구체 타입은 라벨 문구(BuildLabel)만 정의하면 된다(OCP/템플릿 메서드).
/// Unity 는 제네릭 컴포넌트를 직접 붙일 수 없으므로 반드시 구체 하위 클래스를 사용한다.
/// </summary>
/// <typeparam name="T">이 버튼이 나타내는 선택 값의 타입</typeparam>
[RequireComponent(typeof(Button))]
public abstract class OptionButton<T> : MonoBehaviour
{
    [Tooltip("이 버튼이 나타내는 선택 값")]
    [SerializeField] T value;

    [Tooltip("선택 값에 맞춰 문구가 채워질 라벨")]
    [SerializeField] Text label;

    [Tooltip("선택되지 않았을 때의 배경색")]
    [SerializeField] Color normalColor = new Color(0.2f, 0.24f, 0.36f);

    [Tooltip("선택(강조)되었을 때의 배경색")]
    [SerializeField] Color selectedColor = new Color(0.55f, 0.42f, 0.18f);

    /// <summary>클릭을 감지할 버튼 컴포넌트</summary>
    Button button;

    /// <summary>강조 색을 바꿀 배경 이미지(버튼의 targetGraphic)</summary>
    Image background;

    /// <summary>버튼이 클릭되었을 때 이 버튼의 선택 값과 함께 발생한다.</summary>
    public event Action<T> Clicked;

    /// <summary>이 버튼이 나타내는 선택 값</summary>
    public T Value => value;

    /// <summary>
    /// 컴포넌트 초기화: 버튼 클릭 리스너를 등록하고 라벨 문구를 데이터에서 채운다.
    /// </summary>
    protected virtual void Awake()
    {
        button = GetComponent<Button>();
        background = button.targetGraphic as Image;
        button.onClick.AddListener(HandleClick);
        RefreshLabel();
        SetSelected(false);
    }

    /// <summary>
    /// 파괴 시 등록했던 리스너를 해제해 누수를 막는다.
    /// </summary>
    protected virtual void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleClick);
    }

    /// <summary>
    /// 선택 강조 여부를 배경색으로 표시한다.
    /// </summary>
    /// <param name="selected">true 면 강조색, false 면 기본색</param>
    public void SetSelected(bool selected)
    {
        if (background != null)
            background.color = selected ? selectedColor : normalColor;
    }

    /// <summary>
    /// 라벨 문구를 현재 선택 값 기준으로 다시 만든다.
    /// </summary>
    public void RefreshLabel()
    {
        if (label != null)
            label.text = BuildLabel(value);
    }

    /// <summary>
    /// 선택 값을 화면에 표시할 라벨 문구로 변환한다. (리치 텍스트 사용 가능)
    /// </summary>
    /// <param name="optionValue">이 버튼의 선택 값</param>
    protected abstract string BuildLabel(T optionValue);

    /// <summary>
    /// Button.onClick 을 받아 타입이 있는 Clicked 이벤트로 전달한다.
    /// </summary>
    void HandleClick()
    {
        Clicked?.Invoke(value);
    }
}
