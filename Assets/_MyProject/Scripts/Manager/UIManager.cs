using System;
using System.Collections.Generic;
// Unity
using UnityEngine;
// Project
using Jae.Common;

namespace Jae.Manager
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        // --- Screen-Space HUD ---
        private IHealth localPlayerHealth;
        private bool isQuitMenuOpen = false;

        // --- World-Space UI Optimization ---
        private readonly List<Transform> _registeredWorldSpaceCanvases = new List<Transform>();
        private Camera _mainCamera;

        // ============================================
        //  EVENTS
        // ============================================
        // Screen-Space
        public event Action<float, float> OnHealthUpdated;
        public event Action<bool> OnQuitMenuToggled;

        // ============================================
        //  LIFECYCLE
        // ============================================
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }
        
        private void Start()
        {
            _mainCamera = Camera.main;
        }

        private void OnDestroy()
        {
            if (localPlayerHealth != null)
            {
                localPlayerHealth.OnHealthUpdated -= HandleHealthUpdated;
            }
        }

        private void LateUpdate()
        {
            if (_mainCamera == null) 
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return;
            }

            Quaternion cameraRotation = _mainCamera.transform.rotation;
            foreach (var canvasTransform in _registeredWorldSpaceCanvases)
            {
                if(canvasTransform != null)
                {
                   canvasTransform.rotation = cameraRotation;
                }
            }
        }

        // ============================================
        //  PUBLIC API - Screen-Space
        // ============================================

        public void RegisterLocalPlayer(ICombatant playerCombatant)
        {
            if (this.localPlayerHealth != null)
            {
                this.localPlayerHealth.OnHealthUpdated -= HandleHealthUpdated;
            }
            
            this.localPlayerHealth = playerCombatant.GetHealth();
            if (this.localPlayerHealth != null)
            {
                this.localPlayerHealth.OnHealthUpdated += HandleHealthUpdated;
                HandleHealthUpdated(this.localPlayerHealth.Current, this.localPlayerHealth.Max);
            }
        }

        public void ToggleQuitMenu()
        {
            isQuitMenuOpen = !isQuitMenuOpen;
            OnQuitMenuToggled?.Invoke(isQuitMenuOpen);
            
            // 여기서 시간 정지, 입력 비활성화 등의 로직을 추가
            // Time.timeScale = isQuitMenuOpen ? 0f : 1f;
        }

        // ============================================
        //  PUBLIC API - World-Space
        // ============================================

        public void RegisterWorldSpaceCanvas(Transform canvasTransform)
        {
            if (canvasTransform != null && !_registeredWorldSpaceCanvases.Contains(canvasTransform))
            {
                _registeredWorldSpaceCanvases.Add(canvasTransform);
            }
        }

        public void UnregisterWorldSpaceCanvas(Transform canvasTransform)
        {
            if (canvasTransform != null && _registeredWorldSpaceCanvases.Contains(canvasTransform))
            {
                _registeredWorldSpaceCanvases.Remove(canvasTransform);
            }
        }
        
        public void ClearRegisteredWorldSpaceCanvases()
        {
            _registeredWorldSpaceCanvases.Clear();
            Debug.Log("[UIManager] Cleared all registered world-space canvas references.");
        }

        // ============================================
        //  PRIVATE HANDLERS
        // ============================================

        private void HandleHealthUpdated(float current, float max)
        {
            OnHealthUpdated?.Invoke(current, max);
        }
    }
}
