using System;
// Unity
using UnityEngine;
using Unity.Netcode;

namespace Jae.Common
{
    public struct DamageEvent
    {
        public float Amount;
        public DamageType Type;
        public GameObject Attacker;
        public GameObject Target;
        public bool IsCritical;
    }
    
    public struct HitInfo
    {
        public GameObject Hitter;
        public GameObject HitTarget;
        public Vector3 HitPoint;
        public Vector3 HitNormal;
        public float DamageAmount;
        public DamageType DamageType;
    }
    
    public struct DamageResult
    {
        public float FinalDamage;
        public bool IsCritical;
        public bool IsDodged;
        public bool IsBlocked;
    }
    
    [Serializable]
    public class SaveData
    {
        public string SaveId;
        public DateTime SaveTime;
    }
    
    [Serializable]
    public class PlayerProfile
    {
        public string PlayerName;
        public string AccountId;
        public int Level;
        public float Experience;
        public Vector3 LastKnownPosition;
    }
    
    [Serializable]
    public class ProgressData
    {
        public int QuestProgress;
        public string LastQuestId;
        public int CompletedQuestsCount;
    }
    
    public struct MovementSnapshot : INetworkSerializable
    {
        public Vector2 MoveInput;
        public Vector2 LookDelta;
        public bool IsJumping;
        public bool IsSprinting;
        public bool IsWalking;
        public float RotationSpeed;
        public float DeltaTime;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref MoveInput);
            serializer.SerializeValue(ref LookDelta);
            serializer.SerializeValue(ref IsJumping);
            serializer.SerializeValue(ref IsSprinting);
            serializer.SerializeValue(ref IsWalking);
            serializer.SerializeValue(ref RotationSpeed);
            serializer.SerializeValue(ref DeltaTime);
        }
    }
}
