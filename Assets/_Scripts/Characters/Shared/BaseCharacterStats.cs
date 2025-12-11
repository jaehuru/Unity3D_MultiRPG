using Unity.Netcode;
using System;


public abstract class BaseCharacterStats : NetworkBehaviour, ICharacterStats
{
    public NetworkVariable<int> MaxHealth { get; } = new NetworkVariable<int>(100);
    public NetworkVariable<int> CurrentHealth { get; } = new NetworkVariable<int>();
    
    public event Action<int, int> OnHealthChanged;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            CurrentHealth.Value = MaxHealth.Value;
        }
        
        MaxHealth.OnValueChanged += HandleHealthChanged;
        CurrentHealth.OnValueChanged += HandleHealthChanged;
        
        HandleHealthChanged(0, CurrentHealth.Value);
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
    
    [ServerRpc]
    public void SetMaxHealthServerRpc(int newMaxHealth)
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
}