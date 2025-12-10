using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(BaseCharacterStats))]
public class CombatHandler : NetworkBehaviour, ICombatant
{
    [SerializeField] private GameObject floatingTextPrefab;
    
    private ICharacterStats characterStats;
    private bool isDead;

    private void Awake()
    {
        characterStats = GetComponent<ICharacterStats>();
    }

    public void TakeDamage(int damage)
    {
        if (!IsServer) return;
        if (isDead) return;

        int oldHealth = characterStats.CurrentHealth.Value;
        characterStats.CurrentHealth.Value = System.Math.Max(0, characterStats.CurrentHealth.Value - damage);
        int actualDamageTaken = oldHealth - characterStats.CurrentHealth.Value;

        if (actualDamageTaken > 0)
        {
            ShowDamageFeedbackClientRpc(actualDamageTaken);
        }

        if (characterStats.CurrentHealth.Value == 0)
        {
            isDead = true;
            //TODO: 사망 처리 (애니메이션, 아이템 드랍, 비활성화 등)
            Debug.Log($"{gameObject.name} has died!");
        }
    }
    
    public void Heal(int amount)
    {
        if (!IsServer) return;
        if (isDead) return;

        characterStats.CurrentHealth.Value = System.Math.Min(characterStats.CurrentHealth.Value + amount, characterStats.MaxHealth.Value);
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
