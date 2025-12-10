/// <summary>
/// 적 캐릭터의 능력치를 저장
/// BaseCharacterStats에서 공통적인 능력치 로직을 상속
/// Enemy-specific stats 향후 여기에 추가
/// </summary>
public class EnemyStats : BaseCharacterStats
{
    // Enemy-specific stats can be added here.
    // For example:
    // public NetworkVariable<int> Bounty { get; } = new NetworkVariable<int>(10);
}
