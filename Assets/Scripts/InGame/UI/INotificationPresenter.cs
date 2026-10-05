/// <summary>
/// 플레이어에게 짧은 안내 메시지를 보여주는 표시 수단의 추상화.
/// 메시지를 보내는 쪽은 토스트/팝업 등 구체적인 표시 방식을 몰라도 된다(DIP).
/// </summary>
public interface INotificationPresenter
{
    /// <summary>
    /// 안내 메시지를 화면에 띄운다.
    /// </summary>
    /// <param name="message">표시할 문구</param>
    void Show(string message);
}
