using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using TowerBreakers.Core.DTO;

namespace TowerBreakers.Core.Service
{
    #region 뷰 (View)
    /// <summary>
    /// [설명]: 카메라 쉐이크와 스프라이트 이펙트 오브젝트 풀링을 관리하는 클래스입니다.
    /// </summary>
    public class EffectManager : MonoBehaviour, IEffectService
    {
        #region 에디터 설정
        [Header("참조")]
        [SerializeField, Tooltip("흔들림을 적용할 카메라 트랜스폼")]
        private Transform m_mainCamera;

        [SerializeField, Tooltip("이펙트 구성 정보 DTO")]
        private EffectConfigDTO m_config;

        [Header("테스트 설정")]
        [SerializeField] private float m_testZoomFOV = 40f;
        [SerializeField] private float m_testZoomOrthoSize = 2.5f; // [개선]: 축소 방지를 위해 기본값 하향
        [SerializeField] private float m_testZoomDuration = 0.5f;
        #endregion

        #region 내부 필드
        // 이펙트 ID별로 큐(Queue)를 사용하여 간단한 오브젝트 풀 구현
        private Dictionary<string, Queue<TowerBreakers.Core.View.EffectView>> m_effectPools 
            = new Dictionary<string, Queue<TowerBreakers.Core.View.EffectView>>();
        
        // ID별 프리팹 매핑 캐시
        private Dictionary<string, GameObject> m_prefabCache = new Dictionary<string, GameObject>();
        
        private Vector3 m_originalCameraPos;
        private float m_originalCameraValue; // Awake 시점의 원본값
        private float m_preZoomValue;       // 줌 시작 직전의 값
        private Camera m_cameraComponent;
        #endregion

        #region 유니티 생명주기
        private void Awake()
        {
            if (m_mainCamera != null)
            {
                m_originalCameraPos = m_mainCamera.localPosition;
                m_cameraComponent = m_mainCamera.GetComponent<Camera>();
                if (m_cameraComponent != null)
                {
                    m_originalCameraValue = m_cameraComponent.orthographic ? m_cameraComponent.orthographicSize : m_cameraComponent.fieldOfView;
                    string mode = m_cameraComponent.orthographic ? "Orthographic" : "Perspective";
                    Debug.Log($"[EffectManager] 카메라 모드: {mode}, 초기값: {m_originalCameraValue}");
                }
                else
                {
                    Debug.LogError("[EffectManager] 메인 카메라에 Camera 컴포넌트가 없습니다!");
                }
            }
            else
            {
                Debug.LogError("[EffectManager] 인스펙터에 m_mainCamera가 설정되지 않았습니다!");
            }

            InitializePools();
        }
        #endregion

        #region 초기화 및 바인딩 로직
        private void InitializePools()
        {
            if (m_config == null) return;

            foreach (var effectData in m_config.Effects)
            {
                if (string.IsNullOrEmpty(effectData.EffectId) || effectData.Prefab == null) continue;

                m_prefabCache[effectData.EffectId] = effectData.Prefab;
                m_effectPools[effectData.EffectId] = new Queue<TowerBreakers.Core.View.EffectView>();

                // 초기 풀 사이즈만큼 미리 생성
                for (int i = 0; i < effectData.InitialPoolSize; i++)
                {
                    CreateNewEffect(effectData.EffectId);
                }
            }
        }

        private TowerBreakers.Core.View.EffectView CreateNewEffect(string effectId)
        {
            if (!m_prefabCache.ContainsKey(effectId)) return null;

            GameObject obj = Instantiate(m_prefabCache[effectId], transform);
            var effectView = obj.GetComponent<TowerBreakers.Core.View.EffectView>();
            
            if (effectView == null)
            {
                effectView = obj.AddComponent<TowerBreakers.Core.View.EffectView>();
            }

            obj.SetActive(false);
            m_effectPools[effectId].Enqueue(effectView);
            return effectView;
        }
        #endregion

        #region 공개 메서드 (IEffectService 구현)
        /// <summary>
        /// [설명]: 카메라를 지정된 강도와 시간 동안 흔듭니다.
        /// </summary>
        public void PlayCameraShake(float duration, float strength)
        {
            if (m_mainCamera == null) return;

            // 기존 쉐이크 중단 및 위치 초기화
            m_mainCamera.DOKill();
            m_mainCamera.localPosition = m_originalCameraPos;

            // 새로운 쉐이크 시작
            m_mainCamera.DOShakePosition(duration, strength).SetUpdate(true)
                .OnComplete(() => m_mainCamera.localPosition = m_originalCameraPos);
        }

        /// <summary>
        /// [설명]: 기본 설정값으로 카메라 쉐이크를 재생합니다.
        /// </summary>
        public void PlayDefaultCameraShake()
        {
            if (m_config != null)
            {
                PlayCameraShake(m_config.DefaultShakeDuration, m_config.DefaultShakeStrength);
            }
        }

        /// <summary>
        /// [설명]: 지정된 위치와 회전값으로 스프라이트 이펙트를 재생합니다.
        /// </summary>
        public void PlaySpriteEffect(string effectId, Vector3 position, Quaternion rotation)
        {
            if (!m_effectPools.ContainsKey(effectId))
            {
                Debug.LogWarning($"[EffectManager] 등록되지 않은 이펙트 ID입니다: {effectId}");
                return;
            }

            TowerBreakers.Core.View.EffectView effectView;
            if (m_effectPools[effectId].Count > 0)
            {
                effectView = m_effectPools[effectId].Dequeue();
            }
            else
            {
                // 풀이 비어있으면 새로 생성 (동적 확장)
                effectView = CreateNewEffect(effectId);
                if (effectView != null) m_effectPools[effectId].Dequeue(); // 생성 직후 Enqueue되므로 다시 Dequeue
            }

            if (effectView != null)
            {
                effectView.transform.position = position;
                effectView.transform.rotation = rotation;
                effectView.gameObject.SetActive(true);
                effectView.Init(effectId, ReturnToPool);
            }
        }

        /// <summary>
        /// [설명]: 카메라의 시야각(FOV) 또는 오토그래픽 크기를 부드럽게 변경하여 줌 인/아웃 효과를 줍니다.
        /// </summary>
        public void PlayCameraZoom(float targetValue, float duration)
        {
            PlayCameraZoomOnTarget(null, targetValue, duration);
        }

        /// <summary>
        /// [설명]: 특정 대상을 추적하며 카메라 줌을 수행합니다.
        /// </summary>
        public void PlayCameraZoomOnTarget(Transform target, float targetValue, float duration)
        {
            if (m_cameraComponent == null)
            {
                Debug.LogWarning("[EffectManager] 카메라 컴포넌트가 없어 줌을 수행할 수 없습니다.");
                return;
            }

            m_cameraComponent.DOKill();
            if (m_mainCamera != null) m_mainCamera.DOKill();
            
            // [개선]: 줌 시작 직전의 현재 값을 저장하여 복구 시 사용
            m_preZoomValue = m_cameraComponent.orthographic ? m_cameraComponent.orthographicSize : m_cameraComponent.fieldOfView;

            // 1. 위치 이동 (대상을 중앙으로)
            if (target != null && m_mainCamera != null)
            {
                // Z축은 카메라의 원래 Z값을 유지
                Vector3 targetPos = new Vector3(target.position.x, target.position.y, m_originalCameraPos.z);
                m_mainCamera.DOMove(targetPos, duration).SetUpdate(true);
            }

            // 2. 줌 처리
            if (m_cameraComponent.orthographic)
            {
                Debug.Log($"[EffectManager] Orthographic 줌 시작: TargetSize={targetValue}");
                DOTween.To(() => m_cameraComponent.orthographicSize, x => m_cameraComponent.orthographicSize = x, targetValue, duration)
                    .SetTarget(m_cameraComponent).SetUpdate(true);
            }
            else
            {
                Debug.Log($"[EffectManager] Perspective 줌 시작: TargetFOV={targetValue}");
                m_cameraComponent.DOFieldOfView(targetValue, duration).SetUpdate(true);
            }
        }

        /// <summary>
        /// [설명]: 카메라의 위치와 FOV/Size를 원래 상태로 되돌립니다.
        /// </summary>
        public void ResetCamera(float duration)
        {
            if (m_mainCamera != null)
            {
                m_mainCamera.DOLocalMove(m_originalCameraPos, duration).SetUpdate(true);
            }

            if (m_cameraComponent != null)
            {
                // [개선]: Awake 시점의 원본이 아닌, 줌 직전의 값(m_preZoomValue)으로 복구 시도
                // (만약 preZoomValue가 설정된 적 없다면 원본값 사용)
                float recoveryValue = m_preZoomValue > 0.01f ? m_preZoomValue : m_originalCameraValue;

                if (m_cameraComponent.orthographic)
                {
                    DOTween.To(() => m_cameraComponent.orthographicSize, x => m_cameraComponent.orthographicSize = x, recoveryValue, duration)
                        .SetTarget(m_cameraComponent).SetUpdate(true);
                }
                else
                {
                    m_cameraComponent.DOFieldOfView(recoveryValue, duration).SetUpdate(true);
                }
            }
        }

        #region 테스트 도구
        [ContextMenu("카메라 줌 테스트 (인스펙터값 기준)")]
        private void ManualZoomTest()
        {
            float target = m_cameraComponent != null && m_cameraComponent.orthographic ? m_testZoomOrthoSize : m_testZoomFOV;
            PlayCameraZoom(target, m_testZoomDuration);
        }

        [ContextMenu("카메라 리셋 테스트")]
        private void ManualResetTest()
        {
            ResetCamera(0.2f);
        }
        #endregion
        #endregion

        #region 내부 로직
        private void ReturnToPool(TowerBreakers.Core.View.EffectView effectView)
        {
            if (effectView == null) return;
            
            if (m_effectPools.ContainsKey(effectView.EffectId))
            {
                m_effectPools[effectView.EffectId].Enqueue(effectView);
            }
        }
        #endregion
    }
    #endregion
}
