using UnityEngine.SceneManagement;

/// <summary>
/// Unity 기본 SceneManager 를 사용해 씬을 동기 로드하는 ISceneLoader 기본 구현체.
/// </summary>
public sealed class UnitySceneLoader : ISceneLoader
{
    /// <summary>
    /// SceneManager.LoadScene 으로 씬을 단일 모드(기존 씬 교체)로 불러온다.
    /// </summary>
    /// <param name="sceneName">빌드 설정에 등록된 씬 이름</param>
    public void Load(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
