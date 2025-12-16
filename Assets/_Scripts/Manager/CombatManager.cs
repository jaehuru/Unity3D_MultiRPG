// Unity
using UnityEngine;
using Unity.Netcode;
// Project
using Jae.Common;


namespace Jae.Manager
{
    public class CombatManager : NetworkBehaviour
    {
        public static CombatManager Instance { get; private set; }
        
        // --- 성능 최적화를 위한 필드 ---
        private VFXManager _vfxManager;
        private ClientRpcParams _rpcParams;
        private ulong[] _singleClientTarget = new ulong[1];

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            
            _vfxManager = VFXManager.Instance;
            
            _rpcParams = new ClientRpcParams();
        }

        [ServerRpc(InvokePermission = RpcInvokePermission.Everyone)]
        public void PlayerAttackRequestServerRpc(ulong clientId)
        {
            if (!IsServer) return;
            
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) || client.PlayerObject == null)
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

            float attackRange = attacker.GetStats()?.GetStat(StatType.AttackRange) ?? 1.0f;

            if (Physics.Raycast(attackerTransform.position + Vector3.up * 0.5f, attackerTransform.forward, out var hit, attackRange + 1f))
            {
                if (hit.collider.TryGetComponent<ICombatant>(out var potentialTarget) && potentialTarget as Component != attacker as Component)
                {
                    target = potentialTarget;
                }
            }

            if (target != null)
            {
                ProcessAttack(attacker, target);
            }
            else
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.Log($"[CombatManager] {attackerObject.name}'s attack missed.");
#endif
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
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.LogWarning("ProcessAttack should only be called on the server.");
#endif
                return;
            }

            if (attacker == null || target == null)
            {
                Debug.LogError("Attacker or Target is null.");
                return;
            }

            var attackerComp = attacker as Component;
            var targetComp = target as Component;
            if (attackerComp == null || targetComp == null)
            {
                Debug.LogError("Attacker or Target could not be cast to Component.");
                return;
            }

            if (!ValidateHit(attacker, target))
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.Log($"[CombatManager] Hit validation failed for {attackerComp.name} attacking {targetComp.name}.");
#endif
                return;
            }

            var attackerStats = attacker.GetStats();
            if (attackerStats == null)
            {
                Debug.LogError("Attacker has no IStatProvider.");
                return;
            }

            float baseDamage = attackerStats.GetStat(StatType.AttackDamage);

            var damageEvent = new DamageEvent
            {
                Amount = baseDamage,
                Type = DamageType.Physical,
                Attacker = attackerComp.gameObject,
                Target = targetComp.gameObject
            };

            var targetHealth = target.GetHealth();
            if (targetHealth == null)
            {
                Debug.LogError("Target has no IHealth component.");
                return;
            }

            targetHealth.ApplyDamage(damageEvent);

            if (targetComp.TryGetComponent<EnemyWorldSpaceUIController>(out var enemyUIController))
            {
                if (attackerComp is NetworkBehaviour attackerNetworkBehaviour)
                {
                    _singleClientTarget[0] = attackerNetworkBehaviour.OwnerClientId;
                    _rpcParams.Send.TargetClientIds = _singleClientTarget;
                    enemyUIController.ShowCombatUIForAttackerClientRpc(_rpcParams);
                }
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[CombatManager] {damageEvent.Attacker.name} dealt {damageEvent.Amount} damage to {damageEvent.Target.name}.");
#endif

            Vector3 targetPosition = (target as IActor).GetTransform().position;
            ShowDamageVFX_ClientRpc(targetPosition, (int)baseDamage);
        }

        [ClientRpc]
        private void ShowDamageVFX_ClientRpc(Vector3 position, int damage)
        {
            if (_vfxManager != null)
            {
                _vfxManager.ShowFloatingText(position + Vector3.up, damage);
            }
            else
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.LogError("[CombatManager] VFXManager is null. Cannot show floating text.");
#endif
            }
        }

        public bool ValidateHit(ICombatant attacker, ICombatant target)
        {
            if (!IsServer) return false;

            float distance = Vector3.Distance((attacker as IActor).GetTransform().position, (target as IActor).GetTransform().position);
            float attackRange = attacker.GetStats()?.GetStat(StatType.AttackDamage) ?? 1.0f;

            return distance <= attackRange;
        }
    }
}
