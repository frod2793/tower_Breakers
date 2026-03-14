using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

namespace TowerBreakers.Effects
{
    /// <summary>
    /// [설명]: 풀링되는 이펙트 객체의 컴포넌트를 캐싱하는 래퍼 클래스입니다.
    /// </summary>
    public class PooledEffect
    {
        public GameObject GameObject;
        public Transform Transform;
        public Animator Animator;
        public ParticleSystem ParticleSystem;
        public float Duration;

        public PooledEffect(GameObject go)
        {
            GameObject = go;
            Transform = go.transform;
            Animator = go.GetComponent<Animator>();
            ParticleSystem = go.GetComponent<ParticleSystem>();
            
            // 재생 시간 미리 계산
            Duration = 1.0f; // 기본값
            if (Animator != null && Animator.runtimeAnimatorController != null)
            {
                var clips = Animator.runtimeAnimatorController.animationClips;
                if (clips.Length > 0) Duration = clips[0].length;
            }
            if (ParticleSystem != null)
            {
                Duration = Mathf.Max(Duration, ParticleSystem.main.duration + ParticleSystem.main.startLifetime.constantMax);
            }
        }
    }

    /// <summary>
    /// [설명]: 범용 이펙트 관리자입니다. Zero-GC 풀링과 컴포넌트 캐싱을 지원합니다.
    /// </summary>
    public class EffectManager : MonoBehaviour
    {
        #region 에디터 설정
        [SerializeField, Tooltip("이펙트 프리팹 데이터베이스 (ScriptableObject)")]
        private EffectDatabase m_database;

        [SerializeField, Tooltip("각 이펙트 타입별 기본 풀 크기 (데이터베이스 설정이 0일 때 사용)")]
        private int m_defaultPoolSize = 10;
        #endregion

        #region 내부 필드
        private readonly Dictionary<EffectType, Queue<PooledEffect>> m_effectPools = new Dictionary<EffectType, Queue<PooledEffect>>();
        private readonly Dictionary<EffectType, GameObject> m_effectPrefabs = new Dictionary<EffectType, GameObject>();
        private readonly List<PooledEffect> m_activeEffects = new List<PooledEffect>();
        
        private Camera m_mainCamera;
        private Vector3 m_originalCameraPos;
        private Tweener m_shakeTweener;
        #endregion

        #region 유니티 생명주기
        private void Awake()
        {
            InitializeFromDatabase();
            
            // 카메라 초기화
            m_mainCamera = Camera.main;
            if (m_mainCamera != null)
            {
                m_originalCameraPos = m_mainCamera.transform.localPosition;
            }
        }

        private void OnDestroy()
        {
            if (m_shakeTweener != null)
            {
                m_shakeTweener.Kill();
            }
        }
        #endregion

        #region 초기화 로직
        /// <summary>
        /// [설명]: 데이터베이스(SO)에 등록된 모든 이펙트를 순회하며 풀을 생성합니다.
        /// </summary>
        private void InitializeFromDatabase()
        {
            if (m_database == null)
            {
                Debug.LogWarning("[EffectManager] EffectDatabase가 설정되지 않았습니다.");
                return;
            }

            foreach (var data in m_database.Effects)
            {
                if (data.Prefab == null) continue;
                
                // 기존 RegisterEffect 로직 통합
                RegisterEffect(data.Type, data.Prefab, data.PoolSize);
            }
            
            Debug.Log($"[EffectManager] 데이터베이스로부터 {m_database.Effects.Count}개의 이펙트 초기화 완료");
        }
        #endregion

        #region 공개 메서드
        /// <summary>
        /// [설명]: 이펙트 타입을 등록하고 풀을 미리 생성합니다.
        /// </summary>
        public void RegisterEffect(EffectType type, GameObject prefab, int poolSize = 0)
        {
            if (prefab == null) return;
            m_effectPrefabs[type] = prefab;
            
            if (!m_effectPools.ContainsKey(type))
                m_effectPools[type] = new Queue<PooledEffect>();

            int size = poolSize > 0 ? poolSize : m_defaultPoolSize;
            for (int i = 0; i < size; i++)
            {
                CreateNewPooledObject(type, prefab);
            }
        }

        /// <summary>
        /// [설명]: 특정 위치에 이펙트를 재생합니다.
        /// </summary>
        public void PlayEffect(EffectType type, Vector3 position, Quaternion? rotation = null)
        {
            PooledEffect effect = GetActiveEffect(type);
            if (effect == null) return;

            effect.Transform.position = position;
            if (rotation.HasValue) effect.Transform.rotation = rotation.Value;
            
            effect.GameObject.SetActive(true);

            if (effect.Animator != null) effect.Animator.Play(0, 0, 0f);
            if (effect.ParticleSystem != null) effect.ParticleSystem.Play();

            ReturnToPoolAfterDelay(effect, type).Forget();
        }

        /// <summary>
        /// [설명]: 카메라 쉐이크 연출을 실행합니다.
        /// </summary>
        public void ShakeCamera(float intensity, float duration)
        {
            if (m_mainCamera == null)
            {
                m_mainCamera = Camera.main;
                if (m_mainCamera == null) return;
            }

            // 기존 쉐이크 중지 후 초기화
            if (m_shakeTweener != null && m_shakeTweener.IsActive())
            {
                m_shakeTweener.Kill(true);
            }
            m_mainCamera.transform.localPosition = m_originalCameraPos;

            // DOTween을 이용한 쉐이크 (최적화된 콜백 사용)
            m_shakeTweener = m_mainCamera.transform.DOShakePosition(duration, intensity, 10, 90, false, true)
                .OnComplete(ResetCameraPosition);
        }

        /// <summary>
        /// [설명]: 카메라 위치를 원본으로 복구합니다.
        /// </summary>
        public void ResetCameraPosition()
        {
            if (m_mainCamera != null)
            {
                m_mainCamera.transform.localPosition = m_originalCameraPos;
            }
        }
        #endregion

        #region 내부 로직
        /// <summary>
        /// [설명]: 특정 이펙트 타입에 설정된 타격 연출 수치(쉐이크, 역경직)를 가져옵니다.
        /// </summary>
        public bool GetHitFeedbackSettings(EffectType type, out float intensity, out float duration, out float hitStop)
        {
            intensity = 0f;
            duration = 0f;
            hitStop = 0f;

            if (m_database == null) return false;

            var data = m_database.GetEffectData(type);
            if (data.HasValue)
            {
                intensity = data.Value.ShakeIntensity;
                duration = data.Value.ShakeDuration;
                hitStop = data.Value.HitStopDuration;
                return true;
            }

            return false;
        }

        private PooledEffect GetActiveEffect(EffectType type)
        {
            if (m_effectPools.TryGetValue(type, out var pool) && pool.Count > 0)
            {
                return pool.Dequeue();
            }

            if (m_effectPrefabs.TryGetValue(type, out var prefab))
            {
                return CreateNewPooledObject(type, prefab, false);
            }

            return null;
        }

        private PooledEffect CreateNewPooledObject(EffectType type, GameObject prefab, bool enqueue = true)
        {
            var go = Instantiate(prefab, transform);
            var pooled = new PooledEffect(go);
            
            if (enqueue)
            {
                go.SetActive(false);
                m_effectPools[type].Enqueue(pooled);
            }
            return pooled;
        }

        private async Cysharp.Threading.Tasks.UniTaskVoid ReturnToPoolAfterDelay(PooledEffect effect, EffectType type)
        {
            await Cysharp.Threading.Tasks.UniTask.Delay((int)(effect.Duration * 1000));
            
            if (effect.GameObject != null)
            {
                effect.GameObject.SetActive(false);
                if (m_effectPools.TryGetValue(type, out var pool))
                {
                    pool.Enqueue(effect);
                }
            }
        }
        #endregion
    }
}
