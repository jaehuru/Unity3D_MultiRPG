using Unity.Netcode;
using System;

/// <summary>
/// 스탯 데이터 관련 인터페이스
/// </summary>
public interface ICharacterStats
{
    NetworkVariable<int> MaxHealth { get; }
    NetworkVariable<int> CurrentHealth { get; }
    
    
    event Action<int, int> OnHealthChanged;
}
