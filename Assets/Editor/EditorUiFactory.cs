using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 씬 빌더(에디터 도구)들이 공통으로 사용하는 uGUI 오브젝트 생성 헬퍼.
/// 로비/새 게임 씬이 같은 모양의 패널·텍스트·버튼을 쓰도록 한 곳에서 관리한다.
/// </summary>
public static class EditorUiFactory
{
    /// <summary>기본 버튼 배경색</summary>
    public static readonly Color ButtonColor = new Color(0.2f, 0.24f, 0.36f);

    /// <summary>
    /// 에디터 빌더에서 사용할 기본 폰트(Unity 내장 LegacyRuntime)를 가져온다.
    /// </summary>
    public static Font DefaultFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    /// <summary>
    /// 부모 영역 전체를 채우는 빈 패널(RectTransform)을 만든다.
    /// </summary>
    /// <param name="name">오브젝트 이름</param>
    /// <param name="parent">부모 Transform</param>
    public static RectTransform CreatePanel(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        StretchToParent(rt);
        return rt;
    }

    /// <summary>
    /// 중앙 기준 위치/크기를 가진 빈 영역(RectTransform)을 만든다.
    /// </summary>
    /// <param name="name">오브젝트 이름</param>
    /// <param name="parent">부모 Transform</param>
    /// <param name="pos">부모 중앙 기준 위치</param>
    /// <param name="size">영역 크기</param>
    public static RectTransform CreateRect(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    /// <summary>
    /// 텍스트 오브젝트를 만든다.
    /// </summary>
    /// <param name="name">오브젝트 이름</param>
    /// <param name="parent">부모 Transform</param>
    /// <param name="text">표시할 문구</param>
    /// <param name="font">사용할 폰트</param>
    /// <param name="size">글자 크기</param>
    /// <param name="pos">부모 중앙 기준 위치</param>
    /// <param name="box">텍스트 영역 크기</param>
    /// <param name="alignment">정렬 방식 (기본: 가운데)</param>
    /// <param name="bold">굵게 표시 여부 (기본: true)</param>
    public static Text CreateText(string name, Transform parent, string text, Font font, int size,
        Vector2 pos, Vector2 box, TextAnchor alignment = TextAnchor.MiddleCenter, bool bold = true)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;
        var t = go.GetComponent<Text>();
        t.text = text;
        t.font = font;
        t.fontSize = size;
        t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        t.alignment = alignment;
        t.color = Color.white;
        return t;
    }

    /// <summary>
    /// 자식을 세로로 정렬하는 리스트 영역(VerticalLayoutGroup)을 만든다.
    /// </summary>
    /// <param name="name">오브젝트 이름</param>
    /// <param name="parent">부모 Transform</param>
    /// <param name="pos">부모 중앙 기준 위치</param>
    /// <param name="size">리스트 영역 크기 (기본: 520x420)</param>
    /// <param name="spacing">항목 간격 (기본: 24)</param>
    public static RectTransform CreateVerticalList(string name, Transform parent, Vector2 pos,
        Vector2? size = null, float spacing = 24)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size ?? new Vector2(520, 420);
        var layout = go.GetComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return rt;
    }

    /// <summary>
    /// 라벨이 달린 버튼을 만든다. 레이아웃 그룹 안에서는 preferredHeight 가 높이로 쓰인다.
    /// 라벨은 "Label" 이라는 이름의 자식 Text 로 생성된다.
    /// </summary>
    /// <param name="name">오브젝트 이름</param>
    /// <param name="parent">부모 Transform</param>
    /// <param name="label">버튼 문구</param>
    /// <param name="font">사용할 폰트</param>
    /// <param name="fontSize">라벨 글자 크기 (기본: 40)</param>
    /// <param name="preferredHeight">레이아웃 그룹 안에서의 높이 (기본: 84)</param>
    public static Button CreateButton(string name, Transform parent, string label, Font font,
        int fontSize = 40, float preferredHeight = 84)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<LayoutElement>().preferredHeight = preferredHeight;
        go.GetComponent<Image>().color = ButtonColor;

        // 하이라이트/눌림 시 색 변화 (Image 색에 곱해짐)
        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(1.3f, 1.3f, 1.3f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        btn.colors = colors;

        var text = CreateText("Label", go.transform, label, font, fontSize, Vector2.zero, Vector2.zero);
        StretchToParent((RectTransform)text.transform);
        return btn;
    }

    /// <summary>
    /// 버튼의 라벨 Text 를 찾는다. (CreateButton 으로 만든 버튼 전용)
    /// </summary>
    public static Text GetLabel(Button button)
    {
        return button.transform.Find("Label").GetComponent<Text>();
    }

    /// <summary>
    /// 레이아웃 그룹 밖에 놓이는 버튼의 위치와 크기를 지정한다.
    /// </summary>
    public static void Place(Component target, Vector2 pos, Vector2 size)
    {
        var rt = (RectTransform)target.transform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    /// <summary>
    /// 단색(또는 스프라이트) Image 오브젝트를 만든다. 위치/크기는 부모 중앙 기준이다.
    /// </summary>
    /// <param name="name">오브젝트 이름</param>
    /// <param name="parent">부모 Transform</param>
    /// <param name="color">이미지 색</param>
    /// <param name="pos">부모 중앙 기준 위치</param>
    /// <param name="size">이미지 크기</param>
    /// <param name="sprite">사용할 스프라이트 (null 이면 단색 사각형)</param>
    public static Image CreateImage(string name, Transform parent, Color color, Vector2 pos, Vector2 size, Sprite sprite = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var image = go.GetComponent<Image>();
        image.color = color;
        image.sprite = sprite;
        return image;
    }

    /// <summary>
    /// 그래픽에 테두리(Outline 효과)를 추가한다.
    /// </summary>
    /// <param name="target">테두리를 그릴 그래픽 오브젝트</param>
    /// <param name="color">테두리 색</param>
    /// <param name="thickness">테두리 두께(px)</param>
    public static void AddBorder(Component target, Color color, float thickness = 2f)
    {
        var outline = target.gameObject.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(thickness, -thickness);
    }

    /// <summary>
    /// 앵커/피벗을 함께 지정해 화면 가장자리 기준으로 배치한다.
    /// 예) anchor=(0,1), pivot=(0,1) 이면 부모 왼쪽 위 모서리 기준 배치.
    /// </summary>
    /// <param name="rt">배치할 RectTransform</param>
    /// <param name="anchor">앵커 지점 (0~1)</param>
    /// <param name="pivot">자신의 기준점 (0~1)</param>
    /// <param name="pos">앵커 기준 위치</param>
    /// <param name="size">크기</param>
    public static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    /// <summary>
    /// RectTransform 이 부모 영역 전체를 채우도록 앵커와 오프셋을 설정한다.
    /// </summary>
    public static void StretchToParent(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
