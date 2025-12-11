using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

/// <summary>
/// 월드 공간 UI를 제공하는 객체가 구현해야 하는 인터페이스
/// 이 인터페이스를 통해 WorldSpaceUIManager가 해당 객체의 UI를 관리
/// </summary>
public interface IWorldSpaceUIProvider
{
    /// <summary>
    /// 이 객체를 위한 월드 공간 UI 프리팹
    /// </summary>
    GameObject WorldSpaceUIPrefab { get; }

    /// <summary>
    /// 월드 공간 UI가 따라야 할 Transform
    /// 일반적으로 UI를 표시할 객체의 Transform
    /// </summary>
    Transform UIFollowTransform { get; }

    /// <summary>
    /// 이 객체의 캐릭터 스탯 (HP 등)을 제공
    /// </summary>
    ICharacterStats CharacterStats { get; }

    /// <summary>
    /// 이 객체의 이름을 제공 (네트워크 동기화용)
    /// 이름이 없는 경우 null일 수 있음
    /// </summary>
    NetworkVariable<FixedString32Bytes> CharacterName { get; }
}
