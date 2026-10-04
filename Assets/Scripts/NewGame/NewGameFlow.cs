using UnityEngine;

/// <summary>
/// NewGame 씬의 진행 흐름을 제어한다.
/// 난이도 → 성별 → 국가 순서로 패널을 전환하며 선택 결과를 모으고,
/// 국가까지 확정되면 GameSession 에 설정을 저장한 뒤 InGame 씬으로 이동한다.
/// 각 패널은 선택 이벤트만 알리고, 단계 전환 규칙은 이 클래스만 책임진다(SRP).
/// </summary>
public class NewGameFlow : MonoBehaviour
{
    [Header("패널")]
    [Tooltip("1단계: 난이도 선택 패널")]
    [SerializeField] DifficultyPanel difficultyPanel;

    [Tooltip("2단계: 성별 선택 패널")]
    [SerializeField] GenderPanel genderPanel;

    [Tooltip("3단계: 국가 선택 패널")]
    [SerializeField] CountryPanel countryPanel;

    [Header("씬 이름")]
    [Tooltip("난이도 화면에서 뒤로가기 시 돌아갈 로비 씬")]
    [SerializeField] string lobbySceneName = "Lobby";

    [Tooltip("국가 선택 완료 후 진입할 게임 씬")]
    [SerializeField] string inGameSceneName = "InGame";

    /// <summary>씬 전환 담당 객체 (인터페이스에만 의존)</summary>
    ISceneLoader sceneLoader;

    /// <summary>1단계에서 고른 난이도</summary>
    DifficultyDefinition selectedDifficulty;

    /// <summary>2단계에서 고른 성별</summary>
    Gender selectedGender;

    /// <summary>
    /// 외부에서 씬 로더를 주입할 수 있게 한다(테스트, 로딩 화면 등).
    /// 주입하지 않으면 Awake 에서 기본 구현(UnitySceneLoader)을 사용한다.
    /// </summary>
    public void SetSceneLoader(ISceneLoader loader)
    {
        sceneLoader = loader;
    }

    /// <summary>
    /// 기본 씬 로더를 준비하고 각 패널의 이벤트를 구독한다.
    /// 패널이 비활성 상태여도 C# 이벤트 구독은 가능하다.
    /// </summary>
    void Awake()
    {
        if (sceneLoader == null)
            sceneLoader = new UnitySceneLoader();

        difficultyPanel.Chosen += OnDifficultyChosen;
        difficultyPanel.BackRequested += OnBackFromDifficulty;

        genderPanel.Chosen += OnGenderChosen;
        genderPanel.BackRequested += ShowDifficultyStep;

        countryPanel.Chosen += OnCountryChosen;
        countryPanel.BackRequested += ShowGenderStep;
    }

    /// <summary>
    /// 이벤트 구독을 해제한다.
    /// </summary>
    void OnDestroy()
    {
        if (difficultyPanel != null)
        {
            difficultyPanel.Chosen -= OnDifficultyChosen;
            difficultyPanel.BackRequested -= OnBackFromDifficulty;
        }

        if (genderPanel != null)
        {
            genderPanel.Chosen -= OnGenderChosen;
            genderPanel.BackRequested -= ShowDifficultyStep;
        }

        if (countryPanel != null)
        {
            countryPanel.Chosen -= OnCountryChosen;
            countryPanel.BackRequested -= ShowGenderStep;
        }
    }

    /// <summary>
    /// 씬 시작 시 첫 단계(난이도 선택)를 보여준다.
    /// </summary>
    void Start()
    {
        ShowDifficultyStep();
    }

    /// <summary>
    /// 난이도 확정 → 저장 후 성별 단계로 이동.
    /// </summary>
    void OnDifficultyChosen(DifficultyDefinition difficulty)
    {
        selectedDifficulty = difficulty;
        ShowGenderStep();
    }

    /// <summary>
    /// 성별 확정 → 저장 후 국가 단계로 이동.
    /// </summary>
    void OnGenderChosen(Gender gender)
    {
        selectedGender = gender;
        ShowCountryStep();
    }

    /// <summary>
    /// 국가 확정 → 모든 선택을 GameSession 에 기록하고 InGame 씬으로 이동.
    /// </summary>
    void OnCountryChosen(CountryDefinition country)
    {
        var settings = new NewGameSettings(selectedDifficulty, selectedGender, country);
        GameSession.Begin(settings);
        Debug.Log($"새 게임 시작: {settings}");
        sceneLoader.Load(inGameSceneName);
    }

    /// <summary>
    /// 난이도 단계에서 뒤로가기 → 로비 씬으로 돌아간다.
    /// </summary>
    void OnBackFromDifficulty()
    {
        sceneLoader.Load(lobbySceneName);
    }

    /// <summary>난이도 선택 패널만 표시</summary>
    void ShowDifficultyStep()
    {
        genderPanel.Hide();
        countryPanel.Hide();
        difficultyPanel.Show();
    }

    /// <summary>성별 선택 패널만 표시</summary>
    void ShowGenderStep()
    {
        difficultyPanel.Hide();
        countryPanel.Hide();
        genderPanel.Show();
    }

    /// <summary>국가 선택 패널만 표시</summary>
    void ShowCountryStep()
    {
        difficultyPanel.Hide();
        genderPanel.Hide();
        countryPanel.Show();
    }
}
