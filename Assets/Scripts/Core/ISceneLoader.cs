/// <summary>
/// 씬 전환을 추상화한 인터페이스.
/// UI/흐름 제어 코드가 UnityEngine.SceneManagement 에 직접 의존하지 않도록 분리한다(DIP).
/// 테스트나 로딩 화면이 필요한 경우 다른 구현체로 교체할 수 있다.
/// </summary>
public interface ISceneLoader
{
    /// <summary>
    /// 지정한 이름의 씬을 불러온다.
    /// </summary>
    /// <param name="sceneName">빌드 설정에 등록된 씬 이름</param>
    void Load(string sceneName);
}
