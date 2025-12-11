using UnityEngine;

/// <summary>
/// 공격 행위를 수행할 수 있는 객체가 구현해야 하는 인터페이스
/// 이 인터페이스를 통해 객체는 대상을 공격하는 등의 전투 행동을 시작
/// </summary>
public interface IAttacker
{

    void PerformAttack(GameObject target);
    
    // 향후 스킬 사용이나 다른 공격 유형을 위한 메서드를 추가
    // void PerformSkill(SkillType skill, GameObject target);
    // void PerformRangedAttack(GameObject target);
}
