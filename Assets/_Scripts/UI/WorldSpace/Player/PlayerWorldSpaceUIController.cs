
public class PlayerWorldSpaceUIController : WorldSpaceUIController
{
    protected override bool ShouldLogicVisible()
    {
        // 데디케이트 서버 환경을 고려하여 IsOwner 대신 IsLocalPlayer를 사용
        return !IsLocalPlayer;
    }
}
