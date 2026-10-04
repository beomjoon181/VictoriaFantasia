using System;

/// <summary>
/// 씬을 넘어 유지되어야 하는 현재 게임 세션 정보를 보관한다.
/// NewGame 씬에서 확정한 설정을 InGame 씬에서 읽어가는 용도로 사용한다.
/// </summary>
public static class GameSession
{
    /// <summary>
    /// 현재 진행 중인 게임의 시작 설정. 새 게임을 시작하기 전에는 null 이다.
    /// </summary>
    public static NewGameSettings Current { get; private set; }

    /// <summary>
    /// 새 게임 설정으로 세션을 시작(덮어쓰기)한다.
    /// </summary>
    /// <param name="settings">NewGame 씬에서 확정된 설정. null 일 수 없다.</param>
    public static void Begin(NewGameSettings settings)
    {
        Current = settings ?? throw new ArgumentNullException(nameof(settings));
    }
}
