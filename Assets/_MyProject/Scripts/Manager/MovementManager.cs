using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Jae.Common;

namespace Jae.Manager
{
    public class MovementManager : NetworkBehaviour
    {
        public static MovementManager Instance { get; private set; }

        public const float TickRate = 1f / 60f;
        private float _tickTimer;
        public int CurrentTick { get; private set; }

        [Header("Movement Settings")]
        [SerializeField] private float rotationSpeed = 80f;
        [SerializeField] private float movementSmoothTime = 0.1f;

        [Header("Player Physics")]
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -15.0f;
        [SerializeField] private float jumpTimeout = 0.50f;
        [SerializeField] private float fallTimeout = 0.15f;

        [Header("Player Grounded")]
        [SerializeField] private float groundedOffset = -0.14f;
        [SerializeField] private float groundedRadius = 0.28f;
        [SerializeField] private LayerMask groundLayers;

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

        public override void OnNetworkSpawn()
        {
            // 이 매니저는 서버와 모든 클라이언트에서 활성화되어야 하므로 아무것도 하지 않습니다.
            // 내부 TickUpdate에서 IsClient, IsServer로 역할을 분리합니다.
        }
        
        private void Update()
        {
            _tickTimer += Time.deltaTime;

            while (_tickTimer >= TickRate)
            {
                _tickTimer -= TickRate;
                TickUpdate();
                CurrentTick++;
            }
        }

        private void TickUpdate()
        {
            // 로컬 플레이어가 있는 모든 클라이언트(Host 포함)에서 입력 처리 및 예측 실행
            if (IsClient)
            {
                if (NetworkManager.Singleton?.LocalClient?.PlayerObject != null &&
                    NetworkManager.Singleton.LocalClient.PlayerObject.TryGetComponent<PlayerCharacter>(out var pc))
                {
                    pc.ProcessLocalInput(CurrentTick);
                }
            }

            // 서버(전용 서버 또는 호스트)에서 권위있는 시뮬레이션 실행
            if (IsServer)
            {
                foreach (var client in NetworkManager.Singleton.ConnectedClients.Values)
                {
                    if (client.PlayerObject != null && client.PlayerObject.TryGetComponent<PlayerCharacter>(out var pc))
                    {
                        pc.ProcessServerMovement();
                    }
                }
            }
        }

        // 결정론적 이동 시뮬레이션 함수
        public void SimulateMovement(ref MovementState state, MovementInput input, IStatProvider statProvider)
        {
            // --- 1. 회전 처리 ---
            float yaw = input.LookDelta.x * rotationSpeed * TickRate;
            state.Rotation *= Quaternion.Euler(0f, yaw, 0f);

            // --- 2. 속도 및 방향 결정 ---
            float targetSpeed = GetTargetSpeed(input, statProvider);
            
            // MoveTowards를 사용하여 결정론적 가속/감속 처리
            state.HorizontalSpeed = Mathf.MoveTowards(state.HorizontalSpeed, targetSpeed, (targetSpeed > 0 ? 10f : 20f) * TickRate);

            Vector3 inputDirection = new Vector3(input.Move.x, 0.0f, input.Move.y).normalized;
            Vector3 moveDirection = state.Rotation * inputDirection;
            
            Vector3 currentVelocity = moveDirection * state.HorizontalSpeed;
            
            // --- 3. 중력 및 점프 처리 ---
            HandleJumpAndGravity(ref state, input, statProvider);

            currentVelocity.y = state.Velocity.y;
            state.Velocity = currentVelocity;
            
            // 위치 계산은 CharacterController가 담당하므로 여기서는 제거
        }

        private float GetTargetSpeed(MovementInput input, IStatProvider statProvider)
        {
            if (input.Move.magnitude < 0.1f) return 0f;

            if (input.Sprint) return statProvider.GetStat(StatType.SprintSpeed);
            if (input.Walk) return statProvider.GetStat(StatType.WalkSpeed);
            return statProvider.GetStat(StatType.RunSpeed);
        }

        private void HandleJumpAndGravity(ref MovementState state, MovementInput input, IStatProvider statProvider)
        {
            // Ground Check는 물리적 Transform 기준으로 수행해야 함
            Vector3 spherePosition = ((IActor)statProvider).GetTransform().position;
            spherePosition.y += groundedOffset; // 오프셋 적용 방식 변경 가능
            state.IsGrounded = Physics.CheckSphere(spherePosition, groundedRadius, groundLayers, QueryTriggerInteraction.Ignore);

            if (state.IsGrounded)
            {
                state.FallTimeoutDelta = fallTimeout;
                if (state.Velocity.y < 0.0f)
                {
                    state.Velocity.y = -2f;
                }

                if (input.Jump && state.JumpTimeoutDelta <= 0.0f)
                {
                    state.Velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }

                if (state.JumpTimeoutDelta >= 0.0f)
                {
                    state.JumpTimeoutDelta -= TickRate;
                }
            }
            else
            {
                state.JumpTimeoutDelta = jumpTimeout;
                if (state.FallTimeoutDelta >= 0.0f)
                {
                    state.FallTimeoutDelta -= TickRate;
                }
            }

            if (state.Velocity.y < 53.0f)
            {
                state.Velocity.y += gravity * TickRate;
            }
        }
    }
}