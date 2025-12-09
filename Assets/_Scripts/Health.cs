using Unity.Netcode;
using System;

public class Health : NetworkBehaviour
{
    // 네트워크 변수
    public NetworkVariable<int> MaxHealth { get; } = new NetworkVariable<int>(100);
    public NetworkVariable<int> CurrentHealth { get; } = new NetworkVariable<int>();
    
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

        CurrentHealth.Value = Math.Max(0, CurrentHealth.Value - damage);

        if (CurrentHealth.Value == 0)
        {
            isDead = true;
        }
    }
    
    public void Heal(int amount)
    {
        if (!IsServer) return;
        if (isDead) return;

        CurrentHealth.Value = Math.Min(CurrentHealth.Value + amount, MaxHealth.Value);
    }
}