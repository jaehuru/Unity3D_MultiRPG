using Jae.Common;
using UnityEngine;
using Unity.Netcode;
using Jae.Manager;

namespace Jae.Manager
{
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

        [ServerRpc(InvokePermission = RpcInvokePermission.Everyone)]
        public void PlayerAttackRequestServerRpc(ulong clientId)
        {
            if (!IsServer) return;

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) || client.PlayerObject == null)
            {
                Debug.LogError($"[CombatManager] Could not find PlayerObject for client {clientId}");
                return;
            }

            var attackerObject = client.PlayerObject;
            if (!attackerObject.TryGetComponent<ICombatant>(out var attacker))
            {
                Debug.LogError($"[CombatManager] Attacker {attackerObject.name} does not have an ICombatant interface.");
                return;
            }

            ICombatant target = null;
            var attackerTransform = (attacker as IActor).GetTransform();
            if (attackerTransform == null)
            {
                Debug.LogError($"[CombatManager] Attacker {attackerObject.name} does not have a valid transform via IActor.");
                return;
            }

            float attackRange =
                attacker.GetAttackHandler()?.GetAttackType() == AttackType.Melee ? 2.0f : 20.0f; // TODO: Get range from stats/weapon

            if (Physics.Raycast(attackerTransform.position + Vector3.up * 0.5f, attackerTransform.forward, out var hit, attackRange + 1f))
            {
                if (hit.collider.TryGetComponent<ICombatant>(out var potentialTarget))
                {
                    // Ensure the attacker is not targeting themselves
                    if (potentialTarget as Component != attacker as Component)
                    {
                        target = potentialTarget;
                    }
                }
            }

            if (target != null)
            {
                ProcessAttack(attacker, target);
            }
            else
            {
                // Optional: 공격이 아무것도 맞추지 못한 경우를 처리
                Debug.Log($"[CombatManager] {attackerObject.name}'s attack missed.");
            }
        }

        public void ProcessAIAttack(ICombatant attacker, ICombatant target)
        {
            if (!IsServer) return;
            ProcessAttack(attacker, target);
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
                Debug.Log(
                    $"[CombatManager] Hit validation failed for {((attacker as Component)?.gameObject)?.name} attacking {((target as Component)?.gameObject)?.name}.");
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
            // TODO:IDamageCalculator 파이프라인으로 교체
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

            // 5. Trigger visual effects on clients
            Vector3 targetPosition = (target as IActor).GetTransform().position;
            ShowDamageVFX_ClientRpc(targetPosition, (int)baseDamage);
        }

        [ClientRpc]
        private void ShowDamageVFX_ClientRpc(Vector3 position, int damage)
        {
            if (Jae.Manager.VFXManager.Instance != null)
            {
                // 여기서 위치를 조정, e.g., 목표물 중심 약간 위쪽으로 조정 가능
                Jae.Manager.VFXManager.Instance.ShowFloatingText(position + Vector3.up, damage);
            }
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
}
