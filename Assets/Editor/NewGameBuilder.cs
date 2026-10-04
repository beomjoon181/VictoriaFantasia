using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// 새 게임 설정 씬(Assets/Scenes/NewGame.unity)을 생성하는 에디터 도구.
/// 1) 난이도/국가 데이터 에셋을 준비하고 (이미 있으면 기존 에셋을 그대로 사용)
/// 2) InGame 씬이 없으면 빈 씬으로 만들고
/// 3) 난이도 → 성별 → 국가 패널로 구성된 NewGame 씬을 새로 만든 뒤
/// 4) 빌드 설정에 Lobby / NewGame / InGame 씬을 순서대로 등록한다.
/// </summary>
public static class NewGameBuilder
{
    // ───────────── 경로 상수 ─────────────
    const string ScenePath = "Assets/Scenes/NewGame.unity";
    const string LobbyScenePath = "Assets/Scenes/Lobby.unity";
    const string InGameScenePath = "Assets/Scenes/InGame.unity";
    const string DifficultyFolder = "Assets/Data/Difficulties";
    const string CountryFolder = "Assets/Data/Countries";

    /// <summary>
    /// 메뉴 실행 진입점: 데이터 → InGame 씬 → NewGame 씬 → 빌드 설정 순으로 생성한다.
    /// </summary>
    [MenuItem("Tools/Build New Game Scene")]
    public static void Build()
    {
        // 데이터 에셋을 만들고 즉시 디스크에 저장한다.
        // (저장 전에 씬을 전환하면 미사용 에셋 정리 과정에서 메모리상의 새 에셋이 파괴될 수 있다)
        EnsureDifficulties();
        EnsureCountries();
        AssetDatabase.SaveAssets();

        EnsureInGameScene();
        BuildNewGameScene();
        UpdateBuildSettings();
    }

    // ═════════════════════════ 데이터 에셋 ═════════════════════════

    /// <summary>
    /// 난이도 에셋 3종(쉬움/중간/어려움)을 준비한다. 이미 있으면 수정하지 않는다.
    /// </summary>
    static DifficultyDefinition[] EnsureDifficulties()
    {
        EnsureFolder(DifficultyFolder);
        return new[]
        {
            LoadOrCreateDifficulty("Easy", "쉬움", 2_000_000, aiCanDeclareWar: false, aiReceivesSubsidy: false),
            LoadOrCreateDifficulty("Normal", "중간", 2_000_000, aiCanDeclareWar: true, aiReceivesSubsidy: false),
            LoadOrCreateDifficulty("Hard", "어려움", 1_000_000, aiCanDeclareWar: true, aiReceivesSubsidy: true),
        };
    }

    /// <summary>
    /// 국가 에셋 5종을 준비한다. 이미 있으면 수정하지 않는다(디자이너 편집 보존).
    /// </summary>
    static CountryDefinition[] EnsureCountries()
    {
        EnsureFolder(CountryFolder);
        return new[]
        {
            LoadOrCreateCountry("DemonKingdom", "마왕국", CountryDifficulty.VeryHard,
                "네 개의 주를 갖고 있는 마왕국의 땅은 본래부터 넉넉하지 않았다. 산맥 사이의 좁은 계곡에는 검은 돌과 재가 쌓였고, 해가 짧아 곡식이 제대로 자라지 않았다. 마족은 마력에 강했지만 굶주림에는 인간과 다르지 않았다. 전쟁 전에는 남쪽 인간 상인들과 몰래 곡물과 약초를 거래하며 부족한 것을 채웠지만, 전쟁이 시작되자 모든 길이 끊겼다. 창고에 남은 식량은 군대에 먼저 배급되었고, 도시의 시민들은 돌가루가 섞인 검은 빵 한 조각을 받기 위해 새벽부터 줄을 서야 했다. 국경과 가까운 마을에서는 아이들이 따뜻한 수프보다 장례의 북소리에 더 익숙해졌으며, 어떤 가족은 겨울을 넘기기 위해 대대로 전해오던 마도구마저 장작처럼 부숴 불을 피웠다."),
            LoadOrCreateCountry("ArkeniaEmpire", "아르케니아 제국", CountryDifficulty.Easy,
                "가장 강대한 나라는 다섯 개의 주를 거느린 아르케니아 제국이었다. 넓은 농토와 가장 많은 인구, 숙련된 상비군을 보유했으며 옛 연합군의 무기고 대부분도 제국의 손에 들어갔다. 겉으로 보기에는 누구도 제국을 넘볼 수 없었다. 그러나 제국의 영토는 길고 거대했으며, 그중 대부분이 마왕국과 국경을 맞대고 있었다. 북쪽의 다섯 성채에서는 하루도 봉화가 꺼지지 않았고, 국경 마을의 아이들은 글보다 대피로를 먼저 배웠다. 젊은 황제 레오니스는 영광스러운 정복을 꿈꾸었지만, 대신들은 비어 가는 국고를 걱정했고 장군들은 마왕국의 침묵이 패배가 아니라 준비라고 경고했다. 제국은 가장 강했으나, 동시에 가장 먼저 피를 흘릴 나라였다."),
            LoadOrCreateCountry("BelatKingdom", "벨라트 왕국", CountryDifficulty.Normal,
                "제국 서쪽에는 네 개의 주를 가진 벨라트 왕국이 자리했다. 군사력도 경제력도 제국에 미치지는 못했지만, 항구와 상업도시를 통해 대륙의 곡물과 철, 마력석이 모이는 곳이었다. 벨라트의 여왕 세레나는 검보다 계약서를 잘 다루는 통치자였다. 그녀는 제국에는 군량을, 공국에는 무기를 팔면서 어느 쪽도 지나치게 강해지지 않게 균형을 맞췄다. 왕국의 귀족들은 이를 영리한 외교라 칭송했다."),
            LoadOrCreateCountry("CalderaFederation", "칼데라 연방", CountryDifficulty.Easy,
                "마지막 네 번째 나라는 전쟁 영웅과 자유도시들이 세운 칼데라 연방이었다. 세력은 두 개의 주와 여러 자치도시로 흩어져 있었지만, 시민들이 대표를 뽑고 의회에서 법을 정한다는 낯선 제도를 채택했다. 왕도 황제도 없는 나라를 본 주변 군주들은 연방이 오래가지 못하리라 비웃었다. 그러나 연방에는 전쟁을 끝까지 버틴 노련한 장교들, 신분 때문에 고향에서 쫓겨난 학자들, 기회를 찾아온 상인들이 모여들었다. 그들은 가난했지만 변화가 빨랐고, 어느 나라보다 많은 정보를 사고팔았다."),
            LoadOrCreateCountry("ErdinDuchy", "에르딘 공국", CountryDifficulty.Hard,
                "전선에서 가장 멀리 떨어진 남쪽 산악지대에는 세 개의 주뿐인 에르딘 공국이 있었다. 땅은 좁고 인구도 적었지만, 험준한 산에는 풍부한 광물과 오래된 마도 공방들이 숨어 있었다. 다른 나라들은 에르딘을 안전한 후방의 작은 나라라 얕본다."),
        };
    }

    /// <summary>
    /// 난이도 에셋을 불러오거나, 없으면 지정한 값으로 새로 만든다.
    /// 런타임 클래스에 쓰기용 API 를 두지 않기 위해 SerializedObject 로 값을 채운다.
    /// </summary>
    static DifficultyDefinition LoadOrCreateDifficulty(string fileName, string displayName, long funds,
        bool aiCanDeclareWar, bool aiReceivesSubsidy)
    {
        var path = $"{DifficultyFolder}/{fileName}.asset";
        var asset = AssetDatabase.LoadAssetAtPath<DifficultyDefinition>(path);
        if (asset != null)
            return asset;

        asset = ScriptableObject.CreateInstance<DifficultyDefinition>();
        AssetDatabase.CreateAsset(asset, path);

        var so = new SerializedObject(asset);
        so.FindProperty("displayName").stringValue = displayName;
        so.FindProperty("startingFunds").longValue = funds;
        so.FindProperty("aiCanDeclareWar").boolValue = aiCanDeclareWar;
        so.FindProperty("aiReceivesSubsidy").boolValue = aiReceivesSubsidy;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        return asset;
    }

    /// <summary>
    /// 국가 에셋을 불러오거나, 없으면 지정한 값으로 새로 만든다.
    /// </summary>
    static CountryDefinition LoadOrCreateCountry(string fileName, string displayName,
        CountryDifficulty difficulty, string description)
    {
        var path = $"{CountryFolder}/{fileName}.asset";
        var asset = AssetDatabase.LoadAssetAtPath<CountryDefinition>(path);
        if (asset != null)
            return asset;

        asset = ScriptableObject.CreateInstance<CountryDefinition>();
        AssetDatabase.CreateAsset(asset, path);

        var so = new SerializedObject(asset);
        so.FindProperty("displayName").stringValue = displayName;
        so.FindProperty("difficulty").enumValueIndex = (int)difficulty;
        so.FindProperty("description").stringValue = description;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        return asset;
    }

    /// <summary>
    /// "Assets/A/B" 형태의 폴더 경로가 없으면 단계별로 생성한다.
    /// </summary>
    static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        var parts = folderPath.Split('/');
        var current = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    // ═════════════════════════ 씬 생성 ═════════════════════════

    /// <summary>
    /// InGame 씬이 없으면 기본 오브젝트만 있는 빈 씬으로 만든다. 이미 있으면 건드리지 않는다.
    /// </summary>
    static void EnsureInGameScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(InGameScenePath) != null)
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, InGameScenePath);
    }

    /// <summary>
    /// NewGame 씬을 새로 만들어 저장한다. (기존 NewGame 씬은 덮어쓴다)
    /// </summary>
    static void BuildNewGameScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // 씬 전환 이후에 데이터 에셋을 디스크에서 다시 불러와 유효한 참조를 얻는다.
        var difficulties = EnsureDifficulties();
        var countries = EnsureCountries();

        // 로비와 같은 배경색
        var cam = Camera.main;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.09f, 0.13f);

        var font = EditorUiFactory.DefaultFont;
        var canvas = CreateCanvas();
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        var difficultyPanel = BuildDifficultyPanel(canvas, font, difficulties);
        var genderPanel = BuildGenderPanel(canvas, font);
        var countryPanel = BuildCountryPanel(canvas, font, countries);

        // 흐름 제어 컴포넌트에 패널 연결
        var flow = canvas.gameObject.AddComponent<NewGameFlow>();
        var so = new SerializedObject(flow);
        so.FindProperty("difficultyPanel").objectReferenceValue = difficultyPanel;
        so.FindProperty("genderPanel").objectReferenceValue = genderPanel;
        so.FindProperty("countryPanel").objectReferenceValue = countryPanel;
        so.ApplyModifiedPropertiesWithoutUndo();

        // 첫 화면은 NewGameFlow.Start 에서 결정하므로, 저장 시점에는 난이도 패널만 켜 둔다.
        genderPanel.gameObject.SetActive(false);
        countryPanel.gameObject.SetActive(false);

        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    /// <summary>
    /// 1920x1080 기준으로 스케일되는 Screen Space Overlay 캔버스를 만든다.
    /// </summary>
    static RectTransform CreateCanvas()
    {
        var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return (RectTransform)canvasGo.transform;
    }

    /// <summary>
    /// 난이도 선택 패널: 제목, 난이도 버튼 목록, 로비로 돌아가는 뒤로가기 버튼.
    /// </summary>
    static DifficultyPanel BuildDifficultyPanel(Transform canvas, Font font, DifficultyDefinition[] difficulties)
    {
        var root = EditorUiFactory.CreatePanel("DifficultyPanel", canvas);
        EditorUiFactory.CreateText("Title", root, "난이도 선택", font, 72, new Vector2(0, 400), new Vector2(1200, 120));

        var list = EditorUiFactory.CreateVerticalList("Options", root, new Vector2(0, -40), new Vector2(900, 540), 32);
        foreach (var difficulty in difficulties)
        {
            var button = EditorUiFactory.CreateButton($"{difficulty.name}Button", list, difficulty.DisplayName, font, 44, 150);
            BindOption<DifficultyOptionButton>(button, p => p.objectReferenceValue = difficulty);
        }

        var panel = root.gameObject.AddComponent<DifficultyPanel>();
        BindBackButton(panel, CreateBackButton(root, font));
        return panel;
    }

    /// <summary>
    /// 성별 선택 패널: 제목, 남성/여성 버튼, 난이도 화면으로 돌아가는 뒤로가기 버튼.
    /// </summary>
    static GenderPanel BuildGenderPanel(Transform canvas, Font font)
    {
        var root = EditorUiFactory.CreatePanel("GenderPanel", canvas);
        EditorUiFactory.CreateText("Title", root, "성별 선택", font, 72, new Vector2(0, 400), new Vector2(1200, 120));

        var list = EditorUiFactory.CreateVerticalList("Options", root, new Vector2(0, -40), new Vector2(600, 300), 32);
        foreach (Gender gender in System.Enum.GetValues(typeof(Gender)))
        {
            var button = EditorUiFactory.CreateButton($"{gender}Button", list, gender.ToDisplayName(), font, 44, 120);
            BindOption<GenderOptionButton>(button, p => p.enumValueIndex = (int)gender);
        }

        var panel = root.gameObject.AddComponent<GenderPanel>();
        BindBackButton(panel, CreateBackButton(root, font));
        return panel;
    }

    /// <summary>
    /// 국가 선택 패널: 왼쪽 국가 목록, 중앙 상세 영역(이름/난이도/설명/'선택' 버튼),
    /// 상세 영역이 비었을 때의 안내 문구, 성별 화면으로 돌아가는 뒤로가기 버튼.
    /// </summary>
    static CountryPanel BuildCountryPanel(Transform canvas, Font font, CountryDefinition[] countries)
    {
        var root = EditorUiFactory.CreatePanel("CountryPanel", canvas);
        EditorUiFactory.CreateText("Title", root, "국가 선택", font, 72, new Vector2(0, 440), new Vector2(1200, 120));

        // 왼쪽: 국가 목록
        var list = EditorUiFactory.CreateVerticalList("Options", root, new Vector2(-640, 20), new Vector2(460, 640), 20);
        foreach (var country in countries)
        {
            var button = EditorUiFactory.CreateButton($"{country.name}Button", list, country.DisplayName, font, 36, 110);
            BindOption<CountryOptionButton>(button, p => p.objectReferenceValue = country);
        }

        // 중앙: 상세 영역 (반투명 배경)
        var detail = EditorUiFactory.CreateRect("Detail", root, new Vector2(240, -20), new Vector2(1180, 760));
        detail.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.14f, 0.2f, 0.9f);

        var nameText = EditorUiFactory.CreateText("Name", detail, "", font, 60, new Vector2(0, 300), new Vector2(1080, 90));
        var difficultyText = EditorUiFactory.CreateText("Difficulty", detail, "", font, 32, new Vector2(0, 235), new Vector2(1080, 50));
        difficultyText.color = new Color(0.95f, 0.78f, 0.4f);

        var description = EditorUiFactory.CreateText("Description", detail, "", font, 30, new Vector2(0, -30),
            new Vector2(1060, 420), TextAnchor.UpperLeft, bold: false);
        description.lineSpacing = 1.25f;
        description.resizeTextForBestFit = true; // 설명이 길면 영역 안에 맞도록 글자 크기를 줄인다
        description.resizeTextMinSize = 18;
        description.resizeTextMaxSize = 30;

        var confirm = EditorUiFactory.CreateButton("SelectButton", detail, "선택", font, 40);
        EditorUiFactory.Place(confirm, new Vector2(0, -300), new Vector2(360, 90));

        // 중앙: 국가를 고르기 전 안내 문구
        var hint = EditorUiFactory.CreateText("Hint", root, "왼쪽 목록에서 국가를 선택하세요.", font, 40,
            new Vector2(240, -20), new Vector2(1180, 120), TextAnchor.MiddleCenter, bold: false);
        hint.color = new Color(1f, 1f, 1f, 0.6f);

        // 패널 컴포넌트와 참조 연결
        var panel = root.gameObject.AddComponent<CountryPanel>();
        var so = new SerializedObject(panel);
        so.FindProperty("detailRoot").objectReferenceValue = detail.gameObject;
        so.FindProperty("hint").objectReferenceValue = hint.gameObject;
        so.FindProperty("nameText").objectReferenceValue = nameText;
        so.FindProperty("difficultyText").objectReferenceValue = difficultyText;
        so.FindProperty("descriptionText").objectReferenceValue = description;
        so.FindProperty("confirmButton").objectReferenceValue = confirm;
        so.ApplyModifiedPropertiesWithoutUndo();

        BindBackButton(panel, CreateBackButton(root, font));
        detail.gameObject.SetActive(false);
        return panel;
    }

    /// <summary>
    /// 화면 왼쪽 아래에 놓이는 '뒤로' 버튼을 만든다.
    /// </summary>
    static Button CreateBackButton(Transform parent, Font font)
    {
        var back = EditorUiFactory.CreateButton("BackButton", parent, "뒤로", font);
        EditorUiFactory.Place(back, new Vector2(-700, -440), new Vector2(280, 84));
        return back;
    }

    /// <summary>
    /// 버튼에 OptionButton 하위 컴포넌트를 붙이고, 선택 값과 라벨 참조를 연결한다.
    /// </summary>
    /// <typeparam name="TOption">붙일 선택지 버튼 컴포넌트 타입</typeparam>
    /// <param name="button">대상 버튼</param>
    /// <param name="assignValue">"value" 직렬화 속성에 선택 값을 쓰는 동작</param>
    static void BindOption<TOption>(Button button, System.Action<SerializedProperty> assignValue)
        where TOption : MonoBehaviour
    {
        var option = button.gameObject.AddComponent<TOption>();
        var so = new SerializedObject(option);
        assignValue(so.FindProperty("value"));
        so.FindProperty("label").objectReferenceValue = EditorUiFactory.GetLabel(button);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// ChoicePanel 의 backButton 직렬화 필드에 뒤로가기 버튼을 연결한다.
    /// </summary>
    static void BindBackButton(MonoBehaviour panel, Button back)
    {
        var so = new SerializedObject(panel);
        so.FindProperty("backButton").objectReferenceValue = back;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ═════════════════════════ 빌드 설정 ═════════════════════════

    /// <summary>
    /// 빌드 설정 씬 목록의 앞쪽을 Lobby → NewGame → InGame 순서로 맞추고,
    /// 그 외에 이미 등록된 씬은 뒤에 그대로 유지한다.
    /// </summary>
    static void UpdateBuildSettings()
    {
        var ordered = new[] { LobbyScenePath, ScenePath, InGameScenePath };
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(s => System.Array.IndexOf(ordered, s.path) >= 0);

        for (var i = 0; i < ordered.Length; i++)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ordered[i]) != null)
                scenes.Insert(System.Math.Min(i, scenes.Count), new EditorBuildSettingsScene(ordered[i], true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
    }
}
