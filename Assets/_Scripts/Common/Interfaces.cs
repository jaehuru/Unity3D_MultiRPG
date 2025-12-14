using System;
using Jae.Common;
using UnityEngine;
using Unity.Netcode;

namespace Jae.Common
{
    // =====================================
    // 1. Core Interface Layer
    // =====================================

    public interface IActor
    {
        string GetId();
        Transform GetTransform();
    }

    public interface ISpawnContext
    {
        ISpawnPoint Point { get; }
        ISpawnPolicy SpawnPolicy { get; }
        IRespawnPolicy RespawnPolicy { get; }
    }
    
    public interface IStateActivable
    {
        void Activate();
        void Deactivate();
    }

    // =====================================
    // 2. Interaction Interfaces
    // =====================================

    public interface IInteractable
    {
        bool CanInteract(IInteractor interactor);
        void Interact(IInteractor interactor);
    }

    public interface IInteractor
    {
        void TryInteract(IInteractable target);
        IInteractable GetCurrentTarget();
    }

    public interface IInteractionPromptProvider
    {
        string GetPromptText();
        float GetPromptDistance();
    }

    // =====================================
    // 3. Combat Interfaces
    // =====================================

    public interface ICombatant
    {
        IHealth GetHealth();
        IStatProvider GetStats();
        IStatusController GetStatusController();
        IAttackHandler GetAttackHandler();
        ISkillCaster GetSkillCaster();
        IHitDetector GetHitDetector();
        ICombatCooldown GetCooldown();
        ICombatAnimationBinder GetAnimationBinder();
        void OnSpawned(ISpawnContext ctx);
    }

    public interface IHealth
    {
        float Current { get; }
        float Max { get; }
        void ApplyDamage(DamageEvent evt);
        void Heal(float amount);
        event System.Action<DamageEvent> OnDamaged;
        event System.Action OnDied;
    }

    public struct StatModifier { /* TODO: StatModifier 구조체 정의 */ }

    public interface IStatProvider
    {
        float GetStat(StatType stat);
        void SetStat(StatType stat, float value); // Added for stat modification
        void AddModifier(StatModifier m);
        void RemoveModifier(System.Guid id);
    }
    
    // Updated AttackContext
    public struct AttackContext
    {
        public NetworkObjectReference TargetNetworkObjectRef;
        // Add other relevant attack parameters here (e.g., skillId, attackType, etc.)
    }

    public interface IAttackHandler
    {
        bool CanNormalAttack();
        void NormalAttack();
        AttackType GetAttackType();
        DamageType GetDefaultDamageType();
    }

    public interface ICombatCooldown
    {
        bool IsOnCooldown(CombatActionType type);
        float GetCooldownRemaining(CombatActionType type);
        void StartCooldown(CombatActionType type, float duration);
    }
    
    public struct SkillContext { /* TODO: SkillContext 구조체 정의 */ }
    public enum SkillResult { Success, Fail_Cooldown, Fail_InsufficientMana, Fail_InvalidTarget, Fail_Other }

    public interface ISkillCaster
    {
        bool CanCast(int skillId);
        void Cast(int skillId, SkillContext ctx);
    }

    public interface ISkill
    {
        SkillResult Execute(ICombatant caster, SkillContext ctx);
    }
    
    public struct DamageContext { /* TODO: DamageContext 구조체 정의 */ }

    public interface IDamageCalculator
    {
        DamageResult Calculate(ICombatant attacker, ICombatant target, DamageContext ctx);
    }

    // Placeholder for HitValidationResult
    public enum HitValidationResult { Valid, Invalid_Distance, Invalid_Target, Invalid_Other }

    public interface IHitDetector
    {
        void Activate();
        void Deactivate();
        event System.Action<HitInfo> OnHitDetected;
    }

    public interface IHitResolver
    {
        HitValidationResult ValidateHit(HitInfo hit, ICombatant attacker);
    }

    public interface IDamagePipeline
    {
        DamageEvent BuildDamageEvent(ICombatant attacker, ICombatant target, DamageResult result);
    }

    public interface IStatusController
    {
        void AddStatus(IStatusEffect effect);
        void RemoveStatus(string statusId);
        bool HasStatus(string statusId);
    }

    public interface IStatusEffect
    {
        string GetId();
        void OnApply(ICombatant target);
        void OnUpdate(ICombatant target, float dt);
        void OnRemove(ICombatant target);
    }

    public interface ICombatAnimationBinder
    {
        void PlayAttackAnimation(AttackType type);
        void PlaySkillAnimation(int skillId);
        void PlayHitAnimation();
        void PlayDieAnimation();
    }

    // =====================================
    // 4. Movement / Physics / Nav Interfaces
    // =====================================

    public interface IMovable
    {
        void Move(Vector3 direction, float deltaTime);
        void Teleport(Vector3 pos);
    }

    public interface IPhysicsObject
    {
        void ApplyImpulse(Vector3 impulse);
        void SetKinematic(bool v);
    }

    public interface INavigationAgent
    {
        void SetDestination(Vector3 pos);
        bool HasPath();
        void Stop();
    }

    // =====================================
    // 5. Animation Interfaces
    // =====================================

    public interface IAnimationStateProvider
    {
        string GetCurrentState();
        float GetStateNormalizedTime();
    }

    public interface IAnimationEventReceiver
    {
        void OnAnimationEvent(string eventId, object payload);
    }

    public interface IAnimatorSync
    {
        void SyncTrigger(string triggerName);
    }

    public interface IAnimPlayable
    {
        void Play(string state);
        void CrossFade(string state, float duration);
    }
    
    // =====================================
    // 6. Spawn / Respawn Interfaces
    // =====================================
    
    public enum SpawnFilter { None, Enemy, Player }
    public interface ISpawnPoint
    {
        Vector3 GetPosition();
        Quaternion GetRotation();
        SpawnFilter GetFilter();
    }
    
    public interface ISpawnable
    {
        void OnSpawn(ISpawnContext ctx);
        IRespawnPolicy GetRespawnPolicy();
    }
    public interface ISpawnPolicy
    {
        ISpawnPoint SelectSpawnPoint(ISpawnable target);
        ISpawnContext CreateContext(ISpawnPoint point);
    }
    public interface IRespawnPolicy
    {
        bool ShouldRespawn(ISpawnable target);
        System.TimeSpan GetRespawnDelay(ISpawnable target);
        ISpawnPoint SelectRespawnPoint(ISpawnable target);
    }
    
    // =====================================
    // 7. Item / Inventory / Loot Interfaces
    // =====================================
    
    public struct ItemData { /* TODO: ItemData 구조체 정의 */ }

    public interface IPickupable
    {
        bool CanPickup(IActor by);
        ItemData OnPickup(IActor by);
    }

    public interface IInventoryItem
    {
        string GetItemId();
        int GetStackCount();
        void SetCount(int c);
    }
    
    public enum UseResult { Success, Fail_Condition, Fail_Cooldown, Fail_Other }
    public struct UseContext { /* TODO: UseContext 구조체 정의 */ }

    public interface IUsableItem
    {
        UseResult Use(IActor user, UseContext ctx);
    }

    // Placeholder for EquipResult and UnequipResult
    public enum EquipResult { Success, Fail_Condition, Fail_SlotOccupied, Fail_Other }
    public enum UnequipResult { Success, Fail_Condition, Fail_Other }

    public interface IEquipableItem
    {
        EquipResult Equip(IActor user);
        UnequipResult Unequip(IActor user);
    }

    public interface IItemMetadataProvider
    {
        string GetDisplayName();
        string GetCategory();
        RarityType GetRarity();
    }
    
    public interface IInventoryHandler
    {
        bool CanAddItem(IInventoryItem item);
        void AddItem(IInventoryItem item);
        bool RemoveItem(string itemId, int count);
        IInventoryItem GetItem(string itemId);
        System.Collections.Generic.IEnumerable<IInventoryItem> GetAllItems();
        event System.Action<IInventoryItem> OnItemAdded;
        event System.Action<IInventoryItem, int> OnItemRemoved;
    }
    
    // =====================================
    // 8. NPC / AI Interfaces
    // =====================================

    public interface INPCInteractable
    {
        //  임시로직
        //InteractionResult Interact(IInteractor actor);
    }

    public interface IAIController
    {
        void TickAI(float dt);
        void SetState(AIState s);
    }

    public interface INPCStateProvider
    {
        NPCState GetState();
    }
    
    // =====================================
    // 9. Save / Load Interfaces
    // =====================================

    public interface ISaveable
    {
        // 임시 로직
        //SaveData SerializeForSave();
        //void DeserializeFromSave(SaveData data);
    }

    public interface IPlayerDataProvider
    {
        // 임시 로직
        //PlayerProfile GetProfile();
        //void ApplyProfile(PlayerProfile p);
    }
        
    public interface IProgressProvider
    {
        // 임시 로직
        //ProgressData GetProgress();
        //void ApplyProgress(ProgressData p)
    }

        
    // =====================================
    // 10. UI Interfaces
    // =====================================

    public interface IHUDUpdatable
    {
        event Action<float, float> OnHealthChanged;
        event Action<int> OnResourceChanged;
    }

    public interface IWorldSpaceUIProvider : IActor
    {
        string GetDisplayName();
        float GetHealthRatio();
        GameObject WorldSpaceUIPrefab { get; } // Add this line
    }
}