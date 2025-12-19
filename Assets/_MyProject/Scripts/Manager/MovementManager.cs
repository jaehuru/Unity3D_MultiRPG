using System.Collections.Generic;
// Unity
using Unity.Netcode;
using UnityEngine;
// Project
using Jae.Common;

namespace Jae.Manager
{
    public class MovementManager : NetworkBehaviour
    {
        public static MovementManager Instance { get; private set; }

        [Header("Player Physics")]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;
        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;
        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;
        
        [Header("Player Grounded")]
        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;
        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;
        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;
        
        [Header("Movement Smoothing")]
        [Range(1f, 20f)]
        public float movementSmooth = 6f;


        // 각 플레이어의 이동 상태를 저장하는 내부 클래스
        private class PlayerMovementData
        {
            public float speed;
            public float verticalVelocity;
            public float jumpTimeoutDelta;
            public float fallTimeoutDelta;
            public bool isGrounded;
            // For Animation Smoothing
            public float animationSpeedX;
            public float animationSpeedY;
        }

        private Dictionary<ulong, PlayerMovementData> _playerMovementData;
        private Dictionary<ulong, CharacterController> _playerCharacterControllers;
        private Dictionary<ulong, PlayerCharacter> _playerCharacters;


        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                _playerMovementData = new Dictionary<ulong, PlayerMovementData>();
                _playerCharacterControllers = new Dictionary<ulong, CharacterController>();
                _playerCharacters = new Dictionary<ulong, PlayerCharacter>();
            }
        }
        
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _playerMovementData.Clear();
            _playerCharacterControllers.Clear();
            _playerCharacters.Clear();
        }

        public void ServerMove(ulong clientId, MovementSnapshot snap)
        {
            if (!IsServer) return;

            if (PlayerSessionManager.Instance.TryGetPlayerNetworkObject(clientId, out var playerNetworkObject))
            {
                // 데이터 및 컴포넌트 가져오기
                var movementData = GetOrCreateMovementData(clientId);
                var controller = GetOrCreateComponent<CharacterController>(clientId, playerNetworkObject, _playerCharacterControllers);
                var playerCharacter = GetOrCreateComponent<PlayerCharacter>(clientId, playerNetworkObject, _playerCharacters);
                if (controller == null || playerCharacter == null) return;
                
                // --- 1. 중력 및 점프 처리 ---
                HandleJumpAndGravity(playerCharacter, controller, movementData, snap);

                // --- 2. 회전 처리 (Look 입력 기반) ---
                // 마우스 움직임에 따라 캐릭터의 몸 전체를 회전
                float yaw = snap.LookDelta.x * snap.RotationSpeed * snap.DeltaTime;
                playerNetworkObject.transform.Rotate(0f, yaw, 0f);

                // --- 3. 이동 처리 (Strafe 방식) ---
                IStatProvider stat = playerNetworkObject.GetComponent<IStatProvider>();
                float walkSpeed = stat?.GetStat(StatType.WalkSpeed) ?? 2.0f;
                float runSpeed = stat?.GetStat(StatType.RunSpeed) ?? 5.0f;
                float sprintSpeed = stat?.GetStat(StatType.SprintSpeed) ?? 7.5f;

                float targetSpeed;
                if (snap.IsSprinting) targetSpeed = sprintSpeed;
                else if (snap.IsWalking) targetSpeed = walkSpeed;
                else targetSpeed = runSpeed;
                
                // 입력이 없으면 목표 속도를 0으로 설정
                if (snap.MoveInput.magnitude < 0.01f)
                {
                    targetSpeed = 0.0f;
                }
                
                // 가속/감속
                float currentHorizontalSpeed = new Vector3(controller.velocity.x, 0.0f, controller.velocity.z).magnitude;
                float speedOffset = 0.1f;
                
                if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
                {
                    movementData.speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed, snap.DeltaTime * movementSmooth);
                    movementData.speed = Mathf.Round(movementData.speed * 1000f) / 1000f;
                }
                else
                {
                    movementData.speed = targetSpeed;
                }
                
                // 이동 방향 계산 (카메라 기준이 아닌, 캐릭터가 현재 바라보는 방향 기준)
                Vector3 inputDirection = new Vector3(snap.MoveInput.x, 0.0f, snap.MoveInput.y);
                Vector3 moveDirection = playerNetworkObject.transform.right * inputDirection.x + playerNetworkObject.transform.forward * inputDirection.z;

                // 최종 이동
                controller.Move(moveDirection.normalized * (movementData.speed * snap.DeltaTime) + new Vector3(0.0f, movementData.verticalVelocity, 0.0f) * snap.DeltaTime);

                // --- 4. 애니메이션 파라미터 설정 ---
                float animationMultiplier;
                if (snap.IsSprinting) animationMultiplier = 2.0f;
                else if (snap.IsWalking) animationMultiplier = 0.5f;
                else animationMultiplier = 1.0f;

                float animTargetX = snap.MoveInput.x * animationMultiplier;
                float animTargetY = snap.MoveInput.y * animationMultiplier;
                
                movementData.animationSpeedX = Mathf.Lerp(movementData.animationSpeedX, animTargetX, snap.DeltaTime * movementSmooth);
                movementData.animationSpeedY = Mathf.Lerp(movementData.animationSpeedY, animTargetY, snap.DeltaTime * movementSmooth);
                
                playerCharacter.NetworkAnimationX.Value = movementData.animationSpeedX;
                playerCharacter.NetworkAnimationY.Value = movementData.animationSpeedY;
            }
        }
        
        private void HandleJumpAndGravity(PlayerCharacter playerCharacter, CharacterController controller, PlayerMovementData movementData, MovementSnapshot snap)
        {
            Vector3 spherePosition = new Vector3(playerCharacter.transform.position.x, playerCharacter.transform.position.y - GroundedOffset, playerCharacter.transform.position.z);
            movementData.isGrounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
            
            playerCharacter.NetworkAnimationGrounded.Value = movementData.isGrounded;

            if (movementData.isGrounded)
            {
                movementData.fallTimeoutDelta = FallTimeout;

                // 점프/낙하 애니메이션 상태를 리셋
                playerCharacter.NetworkAnimationFreeFall.Value = false;

                if (movementData.verticalVelocity < 0.0f)
                {
                    movementData.verticalVelocity = -2f;
                }

                if (snap.IsJumping && movementData.jumpTimeoutDelta <= 0.0f)
                {
                    movementData.verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                    // 모든 클라이언트에게 점프 애니메이션 트리거를 발동
                    playerCharacter.TriggerJumpAnimation_ClientRpc();
                }

                if (movementData.jumpTimeoutDelta >= 0.0f)
                {
                    movementData.jumpTimeoutDelta -= snap.DeltaTime;
                }
            }
            else
            {
                movementData.jumpTimeoutDelta = JumpTimeout;

                if (movementData.fallTimeoutDelta >= 0.0f)
                {
                    movementData.fallTimeoutDelta -= snap.DeltaTime;
                }
                else
                {
                    playerCharacter.NetworkAnimationFreeFall.Value = true;
                }
            }

            if (movementData.verticalVelocity < 53.0f)
            {
                movementData.verticalVelocity += Gravity * snap.DeltaTime;
            }
        }

        private PlayerMovementData GetOrCreateMovementData(ulong clientId)
        {
            if (!_playerMovementData.ContainsKey(clientId))
            {
                var data = new PlayerMovementData
                {
                    jumpTimeoutDelta = JumpTimeout,
                    fallTimeoutDelta = FallTimeout
                };
                _playerMovementData[clientId] = data;
            }
            return _playerMovementData[clientId];
        }

        private T GetOrCreateComponent<T>(ulong clientId, NetworkObject playerNetworkObject, Dictionary<ulong, T> dictionary) where T : Component
        {
            if (!dictionary.ContainsKey(clientId) || dictionary[clientId] == null)
            {
                if (playerNetworkObject.TryGetComponent<T>(out var component))
                {
                    dictionary[clientId] = component;
                }
                else
                {
                     Debug.LogError($"[MovementManager] Failed to get component {typeof(T).Name} on player object for client {clientId}");
                }
            }
            return dictionary.GetValueOrDefault(clientId);
        }
    }
}