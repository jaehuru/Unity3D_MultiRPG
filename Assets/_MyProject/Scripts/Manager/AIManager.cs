using System.Collections.Generic;
// Unity
using Unity.Netcode;
using UnityEngine;
// Project
using Jae.Common;

namespace Jae.Manager
{
    public class AIManager : NetworkBehaviour
    {
        public static AIManager Instance { get; private set; }

        private readonly List<IAIController> _aiControllers = new List<IAIController>();

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
            base.OnNetworkSpawn();
            this.enabled = IsServer;
        }

        private void Update()
        {
            if (!IsServer) return;

            for (int i = _aiControllers.Count - 1; i >= 0; i--)
            {
                // TODO: Add pooling for safety if controllers can be destroyed mid-loop
                _aiControllers[i].TickAI(Time.deltaTime);
            }
        }

        public void Register(IAIController controller)
        {
            if (!IsServer || controller == null) return;
            if (!_aiControllers.Contains(controller))
            {
                _aiControllers.Add(controller);
            }
        }

        public void Unregister(IAIController controller)
        {
            if (!IsServer || controller == null) return;
            if (_aiControllers.Contains(controller))
            {
                _aiControllers.Remove(controller);
            }
        }
    }
}