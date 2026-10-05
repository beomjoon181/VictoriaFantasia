using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// 인게임 씬(Assets/Scenes/InGame.unity)의 HUD 를 생성하는 에디터 도구.
/// 화면 구성:
/// - 왼쪽 위   : 국기 + GDP / 식자율 / 평균 교육수준 / 생활수준 / 인구 수 / 악명
/// - 오른쪽 위 : 게임 내 날짜(393년 1월 1일 시작) + 일시 정지 / 속도 1~5 버튼
/// - 왼쪽 중앙 : 정부 / 정치 / 건물 / 외교 / 시장 / 군사 / 기술 버튼
/// - 아래 중앙 : 구역 / 건설 / 통계 원형 버튼
/// - 중앙      : 메뉴 버튼으로 여는 화면 창 (현재는 "준비 중" 안내)
/// - 배경      : 3D 대륙 지도 (WorldMapBuilder 가 생성. 마왕국 = 메마른 땅, 나머지 = 평야, 원근 카메라로 비스듬히 내려다봄)
/// </summary>
public static class InGameBuilder
{
    // ───────────── 경로 ─────────────
    const string ScenePath = "Assets/Scenes/InGame.unity";
    const string CircleSpritePath = "Assets/Art/UI/Circle.png";
    const string CountryFolder = "Assets/Data/Countries";
    const string FallbackDifficultyPath = "Assets/Data/Difficulties/Normal.asset";
    const string FallbackCountryPath = "Assets/Data/Countries/ArkeniaEmpire.asset";
    const string FundsRulesPath = "Assets/Data/FundsRules.asset";

    // ───────────── 왼쪽 위 바 레이아웃 (1920x1080 기준) ─────────────
    // 오른쪽 위 시간 패널(폭 460)과 겹치지 않도록 바 전체 폭을 1400 이하로 유지한다.
    const float HeaderWidth = 1400f;
    const float StatStartX = 180f;   // 국기 오른쪽에서 지표 칸이 시작되는 x
    const float StatStep = 132f;     // 지표 칸 간격
    const float FundStep = 206f;     // 자금 칸 간격 (게이지가 있어 더 넓음)

    // ───────────── 색상 팔레트 ─────────────
    static readonly Color SeaColor = new Color(0.09f, 0.17f, 0.25f);
    // 패널은 불투명해야 한다. (반투명이면 Outline 테두리 복사본이 비쳐 패널이 갈색으로 보임)
    static readonly Color PanelColor = new Color(0.1f, 0.1f, 0.13f, 1f);
    static readonly Color GoldColor = new Color(0.78f, 0.62f, 0.32f);
    static readonly Color CaptionColor = new Color(0.9f, 0.78f, 0.5f);
    static readonly Color ButtonColor = new Color(0.16f, 0.17f, 0.22f);

    /// <summary>
    /// 메뉴 실행 진입점: 국가 기본 데이터 보강 → 원형 스프라이트 준비 → InGame 씬 생성.
    /// </summary>
    [MenuItem("Tools/Build InGame Scene")]
    public static void Build()
    {
        // 씬 전환 전에 에셋 변경을 디스크에 저장한다. (미저장 에셋이 씬 전환 중 언로드되는 것 방지)
        ApplyDefaultCountryData();
        EnsureFundsRules();
        EditorSpriteGenerator.EnsureCircleSprite(CircleSpritePath);
        AssetDatabase.SaveAssets();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var cam = Camera.main;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = SeaColor; // 지도 바깥은 바다색으로 보인다

        // 3D 대륙 지도(지형 + 바다 + 국경선)를 깔고 카메라·태양광을 지도에 맞춘다. (HUD 는 그 위 오버레이 캔버스)
        WorldMapBuilder.BuildMap(cam);

        var circle = AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath);
        var font = EditorUiFactory.DefaultFont;

        var canvas = CreateCanvas();
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        var header = BuildNationHeader(canvas, font, out var personalFundsView, out var nationalBudgetView);
        var clockView = BuildClockPanel(canvas, font, circle);
        var leftButtons = BuildLeftMenu(canvas, font, circle);
        var bottomButtons = BuildBottomMenu(canvas, font, circle);
        var window = BuildMenuWindow(canvas, font, circle);
        BuildRouter(canvas, window, leftButtons, bottomButtons);
        var toast = BuildToast(canvas, font); // 창보다 나중에 만들어 항상 위에 그려지게 한다
        BuildSystems(header, clockView, personalFundsView, nationalBudgetView, toast);

        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    // ═════════════════════════ 국가 기본 데이터 ═════════════════════════

    /// <summary>
    /// 국가 에셋에 국기 색과 시작 지표가 비어 있으면(인구 0) 기본값을 채운다.
    /// 이미 값이 있는 에셋은 건드리지 않아 디자이너가 수정한 값을 보존한다.
    /// 아래 수치는 설정(로어)에 맞춘 임시 기획값이다.
    /// </summary>
    static void ApplyDefaultCountryData()
    {
        //                파일명                 국기 색                               GDP($)       식자율  교육  생활  인구
        ApplyCountry("DemonKingdom", new Color(0.36f, 0.12f, 0.42f), 38_000_000, 0.22f, 2.1f, 6.5f, 8_400_000);
        ApplyCountry("ArkeniaEmpire", new Color(0.72f, 0.13f, 0.16f), 180_000_000, 0.41f, 4.8f, 11.2f, 32_500_000);
        ApplyCountry("BelatKingdom", new Color(0.13f, 0.32f, 0.62f), 120_000_000, 0.48f, 5.6f, 13.4f, 14_200_000);
        ApplyCountry("CalderaFederation", new Color(0.16f, 0.5f, 0.3f), 52_000_000, 0.63f, 7.2f, 10.1f, 5_100_000);
        ApplyCountry("ErdinDuchy", new Color(0.78f, 0.57f, 0.16f), 34_000_000, 0.35f, 4.0f, 9.3f, 2_800_000);
    }

    /// <summary>
    /// 국가 에셋 하나에 기본 국기 색과 시작 지표를 채운다. (악명은 0에서 시작)
    /// </summary>
    static void ApplyCountry(string fileName, Color flagColor, double gdp, float literacy,
        float education, float standardOfLiving, long population)
    {
        var country = AssetDatabase.LoadAssetAtPath<CountryDefinition>($"{CountryFolder}/{fileName}.asset");
        if (country == null || country.StartingStats.Population > 0)
            return;

        var so = new SerializedObject(country);
        so.FindProperty("flagColor").colorValue = flagColor;
        so.FindProperty("startingStats.gdp").doubleValue = gdp;
        so.FindProperty("startingStats.literacy").floatValue = literacy;
        so.FindProperty("startingStats.averageEducation").floatValue = education;
        so.FindProperty("startingStats.standardOfLiving").floatValue = standardOfLiving;
        so.FindProperty("startingStats.population").longValue = population;
        so.FindProperty("startingStats.infamy").floatValue = 0f;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(country);
    }

    /// <summary>
    /// 자금 규칙 에셋이 없으면 기본값(개인 자금 500K, 상한 = GDP의 10% / 국가 예산 상한 = GDP의 두 배)으로 만든다.
    /// 이미 있으면 수정하지 않는다.
    /// </summary>
    static void EnsureFundsRules()
    {
        if (AssetDatabase.LoadAssetAtPath<FundsRules>(FundsRulesPath) != null)
            return;

        // 필드 기본값이 곧 기획 기본값이므로 인스턴스를 그대로 저장한다.
        AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<FundsRules>(), FundsRulesPath);
    }

    // ═════════════════════════ HUD 구성 ═════════════════════════

    /// <summary>
    /// 1920x1080 기준으로 스케일되는 Screen Space Overlay 캔버스를 만든다.
    /// </summary>
    static RectTransform CreateCanvas()
    {
        var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return (RectTransform)canvasGo.transform;
    }

    /// <summary>
    /// 왼쪽 위 국가 정보 바: 국기 + 지표 6칸(항목명 / 값) + 자금 2칸(개인 자금, 국가 예산; 값 + 게이지).
    /// </summary>
    /// <param name="canvas">HUD 캔버스</param>
    /// <param name="font">폰트</param>
    /// <param name="personalFundsView">생성된 개인 자금 칸</param>
    /// <param name="nationalBudgetView">생성된 국가 예산 칸</param>
    static NationHeaderView BuildNationHeader(Transform canvas, Font font,
        out CappedFundView personalFundsView, out CappedFundView nationalBudgetView)
    {
        var bar = EditorUiFactory.CreateImage("NationHeader", canvas, PanelColor, Vector2.zero, Vector2.zero);
        EditorUiFactory.Anchor(bar.rectTransform, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(HeaderWidth, 112));
        EditorUiFactory.AddBorder(bar, GoldColor);

        // 국기 (이미지가 없으면 국기 색 + 첫 글자)
        var flag = EditorUiFactory.CreateImage("Flag", bar.transform, Color.gray, Vector2.zero, Vector2.zero);
        EditorUiFactory.Anchor(flag.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -11), new Vector2(150, 90));
        flag.preserveAspect = true;
        EditorUiFactory.AddBorder(flag, GoldColor);
        var initial = EditorUiFactory.CreateText("Initial", flag.transform, "", font, 54, Vector2.zero, Vector2.zero);
        EditorUiFactory.StretchToParent(initial.rectTransform);
        initial.gameObject.AddComponent<Shadow>();

        // 지표 6칸
        string[] captions = { "GDP", "식자율", "평균 교육수준", "생활수준", "인구 수", "악명" };
        var values = new Text[captions.Length];
        for (var i = 0; i < captions.Length; i++)
            values[i] = CreateStatCell(bar.transform, font, captions[i], new Vector2(StatStartX + i * StatStep, 0));

        // 자금 2칸 (지표 칸 바로 오른쪽)
        var fundStartX = StatStartX + captions.Length * StatStep;
        personalFundsView = CreateFundCell(bar.transform, font, "PersonalFunds", "개인 자금", new Vector2(fundStartX, 0));
        nationalBudgetView = CreateFundCell(bar.transform, font, "NationalBudget", "국가 예산", new Vector2(fundStartX + FundStep, 0));

        var view = bar.gameObject.AddComponent<NationHeaderView>();
        var so = new SerializedObject(view);
        so.FindProperty("flagImage").objectReferenceValue = flag;
        so.FindProperty("flagInitial").objectReferenceValue = initial;
        so.FindProperty("gdpText").objectReferenceValue = values[0];
        so.FindProperty("literacyText").objectReferenceValue = values[1];
        so.FindProperty("educationText").objectReferenceValue = values[2];
        so.FindProperty("standardOfLivingText").objectReferenceValue = values[3];
        so.FindProperty("populationText").objectReferenceValue = values[4];
        so.FindProperty("infamyText").objectReferenceValue = values[5];
        so.ApplyModifiedPropertiesWithoutUndo();
        return view;
    }

    /// <summary>
    /// 지표 한 칸(위: 항목명, 아래: 값)을 만들고 값 텍스트를 반환한다.
    /// </summary>
    /// <param name="parent">국가 정보 바</param>
    /// <param name="font">폰트</param>
    /// <param name="caption">항목명</param>
    /// <param name="topLeft">바 왼쪽 위 기준 칸 위치</param>
    static Text CreateStatCell(Transform parent, Font font, string caption, Vector2 topLeft)
    {
        var width = StatStep - 4f;
        var cell = EditorUiFactory.CreateRect($"Stat_{caption}", parent, Vector2.zero, Vector2.zero);
        EditorUiFactory.Anchor(cell, new Vector2(0, 1), new Vector2(0, 1), topLeft, new Vector2(width, 112));
        CreateDivider(cell);

        var captionText = EditorUiFactory.CreateText("Caption", cell, caption, font, 18, new Vector2(0, 24), new Vector2(width, 30), bold: false);
        captionText.color = CaptionColor;

        var valueText = EditorUiFactory.CreateText("Value", cell, "-", font, 30, new Vector2(0, -16), new Vector2(width, 44));
        valueText.gameObject.AddComponent<Shadow>();
        return valueText;
    }

    /// <summary>
    /// 자금 한 칸(위: 항목명, 가운데: "$현재액 / 상한", 아래: 게이지)을 만들고 뷰를 반환한다.
    /// </summary>
    /// <param name="parent">국가 정보 바</param>
    /// <param name="font">폰트</param>
    /// <param name="id">오브젝트 이름용 식별자</param>
    /// <param name="caption">항목명</param>
    /// <param name="topLeft">바 왼쪽 위 기준 칸 위치</param>
    static CappedFundView CreateFundCell(Transform parent, Font font, string id, string caption, Vector2 topLeft)
    {
        var width = FundStep - 6f;
        var cell = EditorUiFactory.CreateRect($"Fund_{id}", parent, Vector2.zero, Vector2.zero);
        EditorUiFactory.Anchor(cell, new Vector2(0, 1), new Vector2(0, 1), topLeft, new Vector2(width, 112));
        CreateDivider(cell);

        var captionText = EditorUiFactory.CreateText("Caption", cell, caption, font, 18, new Vector2(0, 32), new Vector2(width, 28), bold: false);
        captionText.color = CaptionColor;

        var valueText = EditorUiFactory.CreateText("Value", cell, "-", font, 28, new Vector2(0, 0), new Vector2(width, 40));
        valueText.gameObject.AddComponent<Shadow>();

        // 게이지: 어두운 바탕 위에 채움 영역을 얹고, 채움 영역의 가로 앵커로 비율을 표현한다.
        var gaugeBack = EditorUiFactory.CreateImage("Gauge", cell, new Color(0.05f, 0.05f, 0.07f), new Vector2(0, -32), new Vector2(width - 24f, 12));
        EditorUiFactory.AddBorder(gaugeBack, new Color(GoldColor.r, GoldColor.g, GoldColor.b, 0.6f), 1f);
        var fill = EditorUiFactory.CreateImage("Fill", gaugeBack.transform, GoldColor, Vector2.zero, Vector2.zero);
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = new Vector2(0f, 1f);
        fill.rectTransform.pivot = new Vector2(0f, 0.5f);
        fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;

        var view = cell.gameObject.AddComponent<CappedFundView>();
        var so = new SerializedObject(view);
        so.FindProperty("valueText").objectReferenceValue = valueText;
        so.FindProperty("gaugeFill").objectReferenceValue = fill.rectTransform;
        so.FindProperty("gaugeFillImage").objectReferenceValue = fill;
        so.ApplyModifiedPropertiesWithoutUndo();
        return view;
    }

    /// <summary>
    /// 칸 왼쪽에 반투명 금색 세로 구분선을 그린다.
    /// </summary>
    static void CreateDivider(RectTransform cell)
    {
        var divider = EditorUiFactory.CreateImage("Divider", cell, new Color(GoldColor.r, GoldColor.g, GoldColor.b, 0.35f), Vector2.zero, Vector2.zero);
        EditorUiFactory.Anchor(divider.rectTransform, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-4, 0), new Vector2(2, 70));
    }

    /// <summary>
    /// 오른쪽 위 시간 패널: 날짜, 진행 상태, 일시 정지 + 속도 I~V 원형 버튼.
    /// </summary>
    static GameClockView BuildClockPanel(Transform canvas, Font font, Sprite circle)
    {
        var panel = EditorUiFactory.CreateImage("ClockPanel", canvas, PanelColor, Vector2.zero, Vector2.zero);
        EditorUiFactory.Anchor(panel.rectTransform, new Vector2(1, 1), new Vector2(1, 1), Vector2.zero, new Vector2(460, 150));
        EditorUiFactory.AddBorder(panel, GoldColor);

        var date = EditorUiFactory.CreateText("Date", panel.transform, "393년 1월 1일", font, 36, new Vector2(0, 44), new Vector2(430, 48));
        date.gameObject.AddComponent<Shadow>();
        var status = EditorUiFactory.CreateText("Status", panel.transform, "일시 정지됨", font, 20, new Vector2(0, 10), new Vector2(430, 26), bold: false);
        status.color = CaptionColor;

        // 일시 정지 + 속도 5단계 버튼 (가로 한 줄)
        string[] labels = { "||", "I", "II", "III", "IV", "V" };
        var buttons = new Button[labels.Length];
        for (var i = 0; i < labels.Length; i++)
        {
            buttons[i] = CreateCircleButton(i == 0 ? "PauseButton" : $"Speed{i}Button", panel.transform, labels[i], font, 20,
                circle, new Vector2(-170 + i * 68, -38), 54);
        }

        var speedButtons = new Button[labels.Length - 1];
        System.Array.Copy(buttons, 1, speedButtons, 0, speedButtons.Length);

        var view = panel.gameObject.AddComponent<GameClockView>();
        var so = new SerializedObject(view);
        so.FindProperty("dateText").objectReferenceValue = date;
        so.FindProperty("statusText").objectReferenceValue = status;
        so.FindProperty("pauseButton").objectReferenceValue = buttons[0];
        var array = so.FindProperty("speedButtons");
        array.arraySize = speedButtons.Length;
        for (var i = 0; i < speedButtons.Length; i++)
            array.GetArrayElementAtIndex(i).objectReferenceValue = speedButtons[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        return view;
    }

    /// <summary>
    /// 왼쪽 중앙 세로 메뉴: 정부, 정치, 건물, 외교, 시장, 군사, 기술.
    /// </summary>
    static HudMenuButton[] BuildLeftMenu(Transform canvas, Font font, Sprite circle)
    {
        HudMenu[] menus =
        {
            HudMenu.Government, HudMenu.Politics, HudMenu.Buildings, HudMenu.Diplomacy,
            HudMenu.Market, HudMenu.Military, HudMenu.Technology,
        };

        const float step = 84f;
        var height = menus.Length * step + 20f;
        var panel = EditorUiFactory.CreateImage("LeftMenu", canvas, PanelColor, Vector2.zero, Vector2.zero);
        EditorUiFactory.Anchor(panel.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, -20), new Vector2(96, height));
        EditorUiFactory.AddBorder(panel, GoldColor);

        var result = new HudMenuButton[menus.Length];
        for (var i = 0; i < menus.Length; i++)
        {
            var y = height * 0.5f - 10f - step * 0.5f - i * step;
            var button = CreateCircleButton($"{menus[i]}Button", panel.transform, menus[i].ToDisplayName(), font, 22,
                circle, new Vector2(0, y), 72);
            result[i] = AttachMenu(button, menus[i]);
        }

        return result;
    }

    /// <summary>
    /// 아래 중앙 원형 메뉴: 구역, 건설, 통계.
    /// </summary>
    static HudMenuButton[] BuildBottomMenu(Transform canvas, Font font, Sprite circle)
    {
        HudMenu[] menus = { HudMenu.Regions, HudMenu.Construction, HudMenu.Statistics };

        // 버튼 뒤의 받침대
        var bar = EditorUiFactory.CreateImage("BottomMenu", canvas, PanelColor, Vector2.zero, Vector2.zero);
        EditorUiFactory.Anchor(bar.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(520, 96));
        EditorUiFactory.AddBorder(bar, GoldColor);

        var result = new HudMenuButton[menus.Length];
        for (var i = 0; i < menus.Length; i++)
        {
            // 받침대 위로 살짝 솟아오르도록 배치
            var button = CreateCircleButton($"{menus[i]}Button", bar.transform, menus[i].ToDisplayName(), font, 28,
                circle, new Vector2(-160 + i * 160, 26), 124);
            result[i] = AttachMenu(button, menus[i]);
        }

        return result;
    }

    /// <summary>
    /// 메뉴 화면 창: 제목, 본문, 닫기(X) 버튼. 처음에는 숨겨져 있다.
    /// </summary>
    static HudMenuWindow BuildMenuWindow(Transform canvas, Font font, Sprite circle)
    {
        var panel = EditorUiFactory.CreateImage("MenuWindow", canvas, PanelColor, new Vector2(0, -10), new Vector2(1000, 620));
        EditorUiFactory.AddBorder(panel, GoldColor, 3f);

        var title = EditorUiFactory.CreateText("Title", panel.transform, "", font, 48, new Vector2(0, 250), new Vector2(800, 70));
        title.color = CaptionColor;
        var body = EditorUiFactory.CreateText("Body", panel.transform, "", font, 30, new Vector2(0, 0), new Vector2(900, 300), bold: false);
        var close = CreateCircleButton("CloseButton", panel.transform, "X", font, 26, circle, new Vector2(455, 265), 56);

        var window = panel.gameObject.AddComponent<HudMenuWindow>();
        var so = new SerializedObject(window);
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("bodyText").objectReferenceValue = body;
        so.FindProperty("closeButton").objectReferenceValue = close;
        so.ApplyModifiedPropertiesWithoutUndo();

        panel.gameObject.SetActive(false);
        return window;
    }

    /// <summary>
    /// 화면 상단 중앙(국가 정보 바 아래)의 안내 메시지(토스트)를 만든다.
    /// 평소에는 투명하며, 메시지가 오면 잠깐 나타났다 사라진다.
    /// </summary>
    static HudToastView BuildToast(Transform canvas, Font font)
    {
        var panel = EditorUiFactory.CreateImage("Toast", canvas, new Color(0.35f, 0.08f, 0.08f, 1f), Vector2.zero, Vector2.zero);
        EditorUiFactory.Anchor(panel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -170), new Vector2(480, 72));
        EditorUiFactory.AddBorder(panel, GoldColor);
        panel.raycastTarget = false; // 안내창이 클릭을 가로막지 않도록

        var message = EditorUiFactory.CreateText("Message", panel.transform, "", font, 32, Vector2.zero, Vector2.zero);
        EditorUiFactory.StretchToParent(message.rectTransform);
        message.raycastTarget = false;
        message.gameObject.AddComponent<Shadow>();

        var group = panel.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        var toast = panel.gameObject.AddComponent<HudToastView>();
        var so = new SerializedObject(toast);
        so.FindProperty("group").objectReferenceValue = group;
        so.FindProperty("messageText").objectReferenceValue = message;
        so.ApplyModifiedPropertiesWithoutUndo();
        return toast;
    }

    /// <summary>
    /// 메뉴 버튼들과 창을 연결하는 라우터를 HUD 캔버스에 붙인다.
    /// </summary>
    static void BuildRouter(Transform canvas, HudMenuWindow window, HudMenuButton[] left, HudMenuButton[] bottom)
    {
        var router = canvas.gameObject.AddComponent<HudMenuRouter>();
        var so = new SerializedObject(router);
        so.FindProperty("window").objectReferenceValue = window;
        var array = so.FindProperty("buttons");
        array.arraySize = left.Length + bottom.Length;
        for (var i = 0; i < left.Length; i++)
            array.GetArrayElementAtIndex(i).objectReferenceValue = left[i];
        for (var i = 0; i < bottom.Length; i++)
            array.GetArrayElementAtIndex(left.Length + i).objectReferenceValue = bottom[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// 게임 시스템 오브젝트(시계, 키보드 입력, 부트스트랩)를 만들고 참조를 연결한다.
    /// </summary>
    static void BuildSystems(NationHeaderView header, GameClockView clockView,
        CappedFundView personalFundsView, CappedFundView nationalBudgetView, HudToastView toast)
    {
        var systems = new GameObject("GameSystems");
        var clock = systems.AddComponent<GameClock>();
        var input = systems.AddComponent<GameClockKeyboardInput>();
        var bootstrap = systems.AddComponent<InGameBootstrap>();

        var inputSo = new SerializedObject(input);
        inputSo.FindProperty("clock").objectReferenceValue = clock;
        inputSo.ApplyModifiedPropertiesWithoutUndo();

        var so = new SerializedObject(bootstrap);
        so.FindProperty("clock").objectReferenceValue = clock;
        so.FindProperty("nationHeader").objectReferenceValue = header;
        so.FindProperty("clockView").objectReferenceValue = clockView;
        so.FindProperty("personalFundsView").objectReferenceValue = personalFundsView;
        so.FindProperty("nationalBudgetView").objectReferenceValue = nationalBudgetView;
        so.FindProperty("toastView").objectReferenceValue = toast;
        so.FindProperty("fundsRules").objectReferenceValue = AssetDatabase.LoadAssetAtPath<FundsRules>(FundsRulesPath);
        so.FindProperty("fallbackDifficulty").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DifficultyDefinition>(FallbackDifficultyPath);
        so.FindProperty("fallbackCountry").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CountryDefinition>(FallbackCountryPath);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ═════════════════════════ 공용 헬퍼 ═════════════════════════

    /// <summary>
    /// 금색 테두리가 있는 원형 버튼을 만든다.
    /// </summary>
    /// <param name="name">오브젝트 이름</param>
    /// <param name="parent">부모 Transform</param>
    /// <param name="label">버튼 문구</param>
    /// <param name="font">폰트</param>
    /// <param name="fontSize">문구 크기</param>
    /// <param name="circle">원형 스프라이트</param>
    /// <param name="pos">부모 중앙 기준 위치</param>
    /// <param name="diameter">지름</param>
    static Button CreateCircleButton(string name, Transform parent, string label, Font font, int fontSize,
        Sprite circle, Vector2 pos, float diameter)
    {
        // 테두리: 조금 더 큰 금색 원을 뒤에 깔아 링처럼 보이게 한다.
        var ring = EditorUiFactory.CreateImage(name, parent, GoldColor, pos, Vector2.one * diameter, circle);

        var face = EditorUiFactory.CreateImage("Face", ring.transform, ButtonColor, Vector2.zero, Vector2.one * (diameter - 6f), circle);
        var button = face.gameObject.AddComponent<Button>();
        button.targetGraphic = face;
        var colors = button.colors;
        colors.highlightedColor = new Color(1.35f, 1.35f, 1.35f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        button.colors = colors;

        var text = EditorUiFactory.CreateText("Label", face.transform, label, font, fontSize, Vector2.zero, Vector2.zero);
        EditorUiFactory.StretchToParent(text.rectTransform);
        text.gameObject.AddComponent<Shadow>();
        return button;
    }

    /// <summary>
    /// 버튼에 HudMenuButton 컴포넌트를 붙이고 담당 메뉴를 지정한다.
    /// </summary>
    static HudMenuButton AttachMenu(Button button, HudMenu menu)
    {
        var menuButton = button.gameObject.AddComponent<HudMenuButton>();
        var so = new SerializedObject(menuButton);
        so.FindProperty("menu").enumValueIndex = (int)menu;
        so.ApplyModifiedPropertiesWithoutUndo();
        return menuButton;
    }
}
