using Unity.Netcode;
using UnityEngine;
using System; // Action 사용을 위해 추가

[RequireComponent(typeof(BaseCharacterStats))]
public class DamageHandler : NetworkBehaviour, IDamageable
{
    [SerializeField] private GameObject floatingTextPrefab;
    
    private ICharacterStats characterStats;
    private readonly NetworkVariable<bool> isDead = new NetworkVariable<bool>(false); // 사망 상태 네트워크 동기화

    public event Action OnDied; // 캐릭터 사망 시 발생
    public event Action OnRespawned; // 캐릭터 리스폰 시 발생

    private void Awake()
    {
        characterStats = GetComponent<ICharacterStats>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        isDead.OnValueChanged += OnIsDeadChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        isDead.OnValueChanged -= OnIsDeadChanged;
    }

    private void OnIsDeadChanged(bool previousValue, bool newValue)
    {
        if (newValue == true)
        {
            // 클라이언트에서 사망 시각 효과 (애니메이션, 모델 변경 등)를 여기서 처리할 수 있습니다.
            Debug.Log($"{gameObject.name}의 isDead 상태가 {previousValue}에서 {newValue}로 변경되었습니다.");
            OnDied?.Invoke(); // 사망 이벤트 발생
        }
        else // newValue == false
        {
            Debug.Log($"{gameObject.name}의 isDead 상태가 {previousValue}에서 {newValue}로 변경되었습니다. (리스폰)");
            OnRespawned?.Invoke(); // 리스폰 이벤트 발생
        }
    }

    public void TakeDamage(int damage)
    {
        if (!IsServer) return; // 피해 처리 로직은 서버에서만 실행
        if (isDead.Value) return; // 이미 죽었으면 피해를 입지 않음

        int oldHealth = characterStats.CurrentHealth.Value;
        characterStats.CurrentHealth.Value = System.Math.Max(0, characterStats.CurrentHealth.Value - damage);
        int actualDamageTaken = oldHealth - characterStats.CurrentHealth.Value;

        if (actualDamageTaken > 0)
        {
            ShowDamageFeedbackClientRpc(actualDamageTaken);
        }

        if (characterStats.CurrentHealth.Value == 0 && !isDead.Value)
        {
            isDead.Value = true; // 사망 상태로 변경
            Debug.Log($"{gameObject.name} has died!");
            // OnDied 이벤트는 OnIsDeadChanged에서 호출됩니다.
        }
    }
    
    public void Heal(int amount)
    {
        if (!IsServer) return; // 치유 로직은 서버에서만 실행
        if (isDead.Value) return; // 죽었으면 치유되지 않음

        characterStats.CurrentHealth.Value = System.Math.Min(characterStats.CurrentHealth.Value + amount, characterStats.MaxHealth.Value);
    }

    /// <summary>
    /// 캐릭터의 사망 상태를 초기화합니다. (서버에서만 호출)
    /// </summary>
    public void ResetDeathState()
    {
        if (!IsServer) return;
        isDead.Value = false;
        // isDead.Value가 false가 되면 OnRespawned 이벤트가 OnIsDeadChanged에서 호출됩니다.
    }

    /// <summary>
    /// 캐릭터가 사망 상태인지 여부를 반환합니다.
    /// </summary>
    public bool IsDead()
    {
        return isDead.Value;
    }

    [ClientRpc]
    private void ShowDamageFeedbackClientRpc(int damageAmount)
    {
        if (floatingTextPrefab != null)
        {
            Vector3 spawnPosition = transform.position + Vector3.up * 1.5f; 
            GameObject floatingTextGO = Instantiate(floatingTextPrefab, spawnPosition, Quaternion.identity);
            
            if (Camera.main != null)
            {
                floatingTextGO.transform.LookAt(Camera.main.transform);
                floatingTextGO.transform.forward *= -1; 
            }
            
            FloatingText floatingText = floatingTextGO.GetComponent<FloatingText>();
            if (floatingText != null)
            {
                // TODO: 나중에 SetDamage 이외에 SetText, SetColor 등으로 확장
                floatingText.SetDamage(damageAmount);
            }
        }
    }
}
