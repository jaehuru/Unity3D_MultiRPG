using UnityEngine;


public class EnemyWorldSpaceUI : WorldSpaceCharacterUI
{
    protected override Vector3 GetUIOffset()
    {
        return new Vector3(0, 2.0f, 0);
    }
    
    protected override void SetHealthFillColor(int currentHealth, int maxHealth)
    {
        if (healthFillImage != null)
        {
            healthFillImage.color = Color.red;
        }
    }

    // TODO: 적 UI에 특화된 초기화 로직이 있다면 아래와 같이 오버라이드 가능
    /* 
    public override void Init(IWorldSpaceUIProvider provider)
     {
         base.Init(provider);
         // 여기에서 적 전용 초기화 로직 추가
     }
     */
}
