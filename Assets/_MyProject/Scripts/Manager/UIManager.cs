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
                // 게임 씬 전체에 걸쳐 유지되어야 함
                DontDestroyOnLoad(gameObject);
            }
        }
        
        private void Start()
        {
            // 성능을 위해 메인 카메라를 캐싱합니다.
            _mainCamera = Camera.main;
        }

        private void OnDestroy()
        {
            // 로컬 플레이어 이벤트 구독 해제
            if (localPlayerHealth != null)
            {
                localPlayerHealth.OnHealthUpdated -= HandleHealthUpdated;
            }
        }

        private void LateUpdate()
        {
            // 월드 스페이스 UI 최적화: 모든 UI의 빌보드 효과를 한 곳에서 처리
            if (_mainCamera == null) 
            {
                // 씬 전환 등으로 카메라가 잠시 null일 수 있음
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
            
            // 여기서 시간 정지, 입력 비활성화 등의 로직을 추가할 수 있습니다.
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

        // ============================================
        //  PRIVATE HANDLERS
        // ============================================

        private void HandleHealthUpdated(float current, float max)
        {
            OnHealthUpdated?.Invoke(current, max);
        }
    }
}
