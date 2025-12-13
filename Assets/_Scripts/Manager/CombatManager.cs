using UnityEngine;
using Unity.Netcode;
using Jae.Common;
using Jae.DataTypes;

public class CombatManager : NetworkBehaviour
{
    public static CombatManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }
    
    public void ProcessAttack(ICombatant attacker, ICombatant target)
    {
        if (!IsServer)
        {
            Debug.LogWarning("ProcessAttack should only be called on the server.");
            return;
        }

        if (attacker == null || target == null)
        {
            Debug.LogError("Attacker or Target is null.");
            return;
        }
        
        if (!ValidateHit(attacker, target))
        {
            Debug.Log($"[CombatManager] Hit validation failed for {((attacker as Component)?.gameObject)?.name} attacking {((target as Component)?.gameObject)?.name}.");
            return;
        }

        // 1. Get stats from the attacker
        var attackerStats = attacker.GetStats();
        if (attackerStats == null)
        {
            Debug.LogError("Attacker has no IStatProvider.");
            return;
        }
        
        // 2. Calculate damage
        // TODO: Replace with a proper IDamageCalculator pipeline
        float baseDamage = attackerStats.GetStat(StatType.AttackDamage);

        // 3. Create a damage event
        var damageEvent = new DamageEvent
        {
            Amount = baseDamage,
            Type = DamageType.Physical, // Or attacker.GetDefaultDamageType()
            Attacker = (attacker as Component)?.gameObject,
            Target = (target as Component)?.gameObject
        };

        // 4. Apply damage to the target's health component
        var targetHealth = target.GetHealth();
        if (targetHealth == null)
        {
            Debug.LogError("Target has no IHealth component.");
            return;
        }
        
        targetHealth.ApplyDamage(damageEvent);

        Debug.Log($"[CombatManager] {damageEvent.Attacker.name} dealt {damageEvent.Amount} damage to {damageEvent.Target.name}.");
    }
    
    public bool ValidateHit(ICombatant attacker, ICombatant target)
    {
        if (!IsServer) return false;
        
        // TODO: 타격 유효성 검사 로직을 구현(e.g., 거리 확인, 시야 확보)
        float distance = Vector3.Distance((attacker as IActor).GetTransform().position, (target as IActor).GetTransform().position);
        float attackRange = attacker.GetAttackHandler()?.GetAttackType() == AttackType.Melee ? 2.0f : 20.0f;
        
        return distance <= attackRange;
    }
}
