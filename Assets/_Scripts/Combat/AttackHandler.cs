using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(BaseCharacterStats))]
public class AttackHandler : NetworkBehaviour, IAttacker
{
    [Header("Attack Settings")] // 임시
    [SerializeField] private float attackRange;
    [SerializeField] private int attackDamage;

    private ICharacterStats _characterStats;

    private void Awake()
    {
        _characterStats = GetComponent<ICharacterStats>();
        if (_characterStats == null)
        {
            Debug.LogError($"AttackHandler on {gameObject.name}: BaseCharacterStats 컴포넌트를 찾을 수 없습니다. 공격력을 가져올 수 없습니다.");
        }
    }

    [ServerRpc]
    public void PerformAttackServerRpc(NetworkObjectReference targetNetworkObjectRef)
    {
        if (!IsServer) return;

        if (!targetNetworkObjectRef.TryGet(out NetworkObject targetNetworkObject))
        {
            Debug.LogWarning("PerformAttackServerRpc: 대상 NetworkObject를 찾을 수 없습니다.");
            return;
        }

        GameObject target = targetNetworkObject.gameObject;

        // 공격 범위 체크
        float currentAttackRange = attackRange;
        if (Vector3.Distance(transform.position, target.transform.position) > currentAttackRange)
        {
            Debug.Log($"{gameObject.name}의 공격이 {target.name}에 닿지 않았습니다. (거리: {Vector3.Distance(transform.position, target.transform.position):F2}m, 필요 거리: {currentAttackRange:F2}m)");
            return;
        }

        // 대상이 피해를 받을 수 있는지 확인
        if (target.TryGetComponent<IDamageable>(out IDamageable damageableTarget))
        {
            // TODO: 실제 공격력 계산 로직 (예: _characterStats.AttackDamage.Value 등)
            // 현재는 임시 플레이스홀더 데미지 사용
            int finalAttackDamage = attackDamage; 
            if (_characterStats != null)
            {
                // 예시: 스탯에서 공격력 가져오기
                // finalAttackDamage = _characterStats.AttackPower.Value; // BaseCharacterStats에 AttackPower가 있다면
            }

            damageableTarget.TakeDamage(finalAttackDamage);
            Debug.Log($"{gameObject.name}이(가) {target.name}에게 {finalAttackDamage}의 피해를 입혔습니다.");
        }
        else
        {
            Debug.Log($"{target.name}은(는) 피해를 입을 수 없는 대상입니다 (IDamageable 없음).");
        }
    }
    
    public void PerformAttack(GameObject target)
    {
        // IAttacker 인터페이스는 NetworkObjectReference를 직접 받을 수 없으므로,
        // 이 메서드는 클라이언트에서 서버 RPC를 호출하기 위한 래퍼 역할
        // 클라이언트에서 호출될 것이므로, 서버 RPC로 다시 전달
        if (!IsOwner) return;

        if (target.TryGetComponent<NetworkObject>(out NetworkObject targetNetworkObject))
        {
            PerformAttackServerRpc(targetNetworkObject);
        }
        else
        {
            Debug.LogError($"AttackHandler on {gameObject.name}: 공격 대상 {target.name}에 NetworkObject 컴포넌트가 없습니다. 서버 RPC를 호출할 수 없습니다.");
        }
    }

    // TODO: 향후 스킬 사용이나 다른 공격 유형을 위한 메서드를 추가
    // [ServerRpc]
    // public void PerformSkillServerRpc(SkillType skill, NetworkObjectReference targetNetworkObjectRef) { ... }
}