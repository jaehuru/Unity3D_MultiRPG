using UnityEngine;


public class PlayerWorldSpaceUI : WorldSpaceCharacterUI
{
    [Header("Player Health Color Settings")]
    [SerializeField] private Color playerFullHealthColor = Color.green;
    [SerializeField] private Color playerLowHealthColor = Color.red;

    protected override Vector3 GetUIOffset()
    {
        return new Vector3(0, 2.0f, 0);
    }
    
    protected override void SetHealthFillColor(int currentHealth, int maxHealth)
    {
        if (healthFillImage != null)
        {
            healthFillImage.color = Color.Lerp(playerLowHealthColor, playerFullHealthColor, (float)currentHealth / maxHealth);
        }
    }

    /* 플레이어 UI에 특화된 초기화 로직이 있다면 아래와 같이 오버라이드 가능
    public override void Init(IWorldSpaceUIProvider provider)
    {
        base.Init(provider);
        // 여기에서 플레이어 전용 초기화 로직 추가 (예: 스태미너 바, 버프 아이콘)
    }
    */
}
