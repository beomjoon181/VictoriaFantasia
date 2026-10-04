using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// 로비 씬(Assets/Scenes/Lobby.unity)을 생성하는 에디터 도구
// UI 생성 헬퍼는 EditorUiFactory 를 공유한다.
public static class LobbyBuilder
{
    const string ScenePath = "Assets/Scenes/Lobby.unity";

    [MenuItem("Tools/Build Lobby Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var cam = Camera.main;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.09f, 0.13f);

        var font = EditorUiFactory.DefaultFont;

        // Canvas
        var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        var menu = canvasGo.AddComponent<LobbyMenu>();

        // 메인 패널
        var main = EditorUiFactory.CreatePanel("MainPanel", canvasGo.transform);
        EditorUiFactory.CreateText("Title", main, "VICTORIA FANTASIA", font, 96, new Vector2(0, 280), new Vector2(1400, 160));
        var list = EditorUiFactory.CreateVerticalList("Buttons", main, new Vector2(0, -80));
        var bNew = EditorUiFactory.CreateButton("NewGameButton", list, "새 게임", font);
        var bLoad = EditorUiFactory.CreateButton("LoadGameButton", list, "게임 불러오기", font);
        var bOpt = EditorUiFactory.CreateButton("OptionsButton", list, "게임 옵션", font);
        var bQuit = EditorUiFactory.CreateButton("QuitButton", list, "나가기", font);

        // 옵션 패널
        var opt = EditorUiFactory.CreatePanel("OptionsPanel", canvasGo.transform);
        EditorUiFactory.CreateText("Title", opt, "게임 옵션", font, 72, new Vector2(0, 280), new Vector2(1200, 120));
        EditorUiFactory.CreateText("Body", opt, "옵션 항목은 여기에 추가하세요.", font, 36, new Vector2(0, 40), new Vector2(1200, 80));
        var optList = EditorUiFactory.CreateVerticalList("Buttons", opt, new Vector2(0, -260));
        var bBack = EditorUiFactory.CreateButton("BackButton", optList, "뒤로", font);

        // 참조 연결
        var so = new SerializedObject(menu);
        so.FindProperty("mainPanel").objectReferenceValue = main.gameObject;
        so.FindProperty("optionsPanel").objectReferenceValue = opt.gameObject;
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(bNew.onClick, new UnityAction(menu.OnNewGame));
        UnityEventTools.AddPersistentListener(bLoad.onClick, new UnityAction(menu.OnLoadGame));
        UnityEventTools.AddPersistentListener(bOpt.onClick, new UnityAction(menu.OnOptions));
        UnityEventTools.AddPersistentListener(bQuit.onClick, new UnityAction(menu.OnQuit));
        UnityEventTools.AddPersistentListener(bBack.onClick, new UnityAction(menu.OnBackFromOptions));

        opt.gameObject.SetActive(false);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();
    }

    static void AddToBuildSettings()
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(s => s.path == ScenePath);
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        const string game = "Assets/Scenes/NewGame.unity";
        if (!scenes.Exists(s => s.path == game))
            scenes.Add(new EditorBuildSettingsScene(game, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
