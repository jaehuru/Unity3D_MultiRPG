
namespace Jae.Common
{
    public enum StatType
    {
        Strength,
        Dexterity,
        Intelligence,
        Vitality,
        Health,
        MaxHealth,
        Mana,
        AttackDamage,
        AttackRange,
        MagicDamage,
        Defense,
        MagicResistance,
        CritChance,
        CritDamage,
        WalkSpeed,
        RunSpeed,
        SprintSpeed,
        AttackSpeed,
        CooldownReduction
    }

    public enum DamageType
    {
        Physical,
        Magic,
        True
    }

    public enum AttackType
    {
        Melee,
        Ranged,
        Skill
    }

    public enum CombatActionType
    {
        NormalAttack,
        Skill,
        Dash,
        Evade
    }

    public enum RarityType
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public enum AIState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Flee,
        Dead
    }

    public enum NPCState
    {
        Idle,
        Walking,
        Talking,
        QuestAvailable,
        QuestInProgress,
        QuestCompleted
    }
}
