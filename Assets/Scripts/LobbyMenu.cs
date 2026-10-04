using UnityEngine;
using UnityEngine.SceneManagement;

// 로비 화면 버튼 동작
public class LobbyMenu : MonoBehaviour
{
    [SerializeField] string gameSceneName = "NewGame";
    [SerializeField] GameObject mainPanel;
    [SerializeField] GameObject optionsPanel;

    void Start()
    {
        ShowMain();
    }

    public void OnNewGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnLoadGame()
    {
        // TODO: 세이브 시스템 연결
        Debug.Log("게임 불러오기: 아직 구현되지 않았습니다.");
    }

    public void OnOptions()
    {
        mainPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    public void OnBackFromOptions()
    {
        ShowMain();
    }

    public void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void ShowMain()
    {
        mainPanel.SetActive(true);
        optionsPanel.SetActive(false);
    }
}
