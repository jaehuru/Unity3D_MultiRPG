using Unity.Netcode;
using UnityEngine;

namespace Jae.Common
{
    // 서버와 클라이언트가 공유하는 결정론적 이동 상태
    // 엔진의 물리 상태(e.g. CharacterController.velocity)에서 완전히 분리
    public struct MovementState
    {
        public int Tick;
        public Vector3 Position;
        public Vector3 Velocity;
        public float HorizontalSpeed;
        public Quaternion Rotation;

        // 물리 상태와 무관한 추가 상태
        public bool IsGrounded;
        public float JumpTimeoutDelta;
        public float FallTimeoutDelta;
    }

    // 클라이언트에서 서버로 전송되는 입력 데이터
    public struct MovementInput : INetworkSerializable
    {
        public int Tick;
        public Vector2 Move;
        public Vector2 LookDelta;
        public bool Jump;
        public bool Sprint;
        public bool Walk;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Tick);
            serializer.SerializeValue(ref Move);
            serializer.SerializeValue(ref LookDelta);
            serializer.SerializeValue(ref Jump);
            serializer.SerializeValue(ref Sprint);
            serializer.SerializeValue(ref Walk);
        }
    }
}
