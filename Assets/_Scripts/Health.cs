using Unity.Netcode;
using System;
using UnityEngine;

public class Health : NetworkBehaviour
{
    public NetworkVariable<int> MaxHealth { get; } = new NetworkVariable<int>(100);
    public NetworkVariable<int> CurrentHealth { get; } = new NetworkVariable<int>();
    
    [SerializeField] private GameObject damageNumberPrefab;
    
    public event Action<int, int> OnHealthChanged;

    private bool isDead;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            CurrentHealth.Value = MaxHealth.Value;
        }
        
        MaxHealth.OnValueChanged += HandleHealthChanged;
        CurrentHealth.OnValueChanged += HandleHealthChanged;
        
        HandleHealthChanged(0, 0);
    }

    public override void OnNetworkDespawn()
    {
        MaxHealth.OnValueChanged -= HandleHealthChanged;
        CurrentHealth.OnValueChanged -= HandleHealthChanged;
    }
    
    private void HandleHealthChanged(int previousValue, int newValue)
    {
        OnHealthChanged?.Invoke(CurrentHealth.Value, MaxHealth.Value);
    }
    
    public void SetMaxHealth(int newMaxHealth)
    {
        if (!IsServer) return;

        int oldMaxHealth = MaxHealth.Value;
        MaxHealth.Value = newMaxHealth;
        
        int healthDifference = newMaxHealth - oldMaxHealth;
        if (healthDifference > 0)
        {
            CurrentHealth.Value += healthDifference;
        }
        else
        {
            CurrentHealth.Value = Math.Min(CurrentHealth.Value, newMaxHealth);
        }
    }
    
    public void TakeDamage(int damage)
    {
        if (!IsServer) return;
        if (isDead) return;

        int oldHealth = CurrentHealth.Value;
        CurrentHealth.Value = Math.Max(0, CurrentHealth.Value - damage);
        int actualDamageTaken = oldHealth - CurrentHealth.Value;

        if (actualDamageTaken > 0)
        {
            ShowDamageFeedbackClientRpc(actualDamageTaken);
        }

        if (CurrentHealth.Value == 0)
        {
            isDead = true;
            //TODO: 사망 처리
            Debug.Log($"{gameObject.name} has died!");
        }
    }
    
    public void Heal(int amount)
    {
        if (!IsServer) return;
        if (isDead) return;

        CurrentHealth.Value = Math.Min(CurrentHealth.Value + amount, MaxHealth.Value);
    }

    [ClientRpc]
    private void ShowDamageFeedbackClientRpc(int damageAmount)
    {
        if (damageNumberPrefab != null)
        {
            Vector3 spawnPosition = transform.position + Vector3.up * 1.5f; 
            GameObject damageNumberGO = Instantiate(damageNumberPrefab, spawnPosition, Quaternion.identity);
            
            if (Camera.main != null)
            {
                damageNumberGO.transform.LookAt(Camera.main.transform);
                damageNumberGO.transform.forward *= -1; 
            }
            
            DamageNumber damageNumber = damageNumberGO.GetComponent<DamageNumber>();
            if (damageNumber != null)
            {
                damageNumber.SetDamage(damageAmount);
            }
            else
            {
                Debug.LogWarning("DamageNumber prefab is missing DamageNumber script.");
            }
        }
        else
        {
            Debug.LogWarning("DamageNumber prefab is not assigned in Health.cs.");
        }
    }
}