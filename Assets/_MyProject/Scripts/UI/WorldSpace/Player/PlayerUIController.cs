
public class PlayerUIController : WorldSpaceUIController
{
    protected override bool ShouldLogicVisible()
    {
        return !IsLocalPlayer;
    }
}
