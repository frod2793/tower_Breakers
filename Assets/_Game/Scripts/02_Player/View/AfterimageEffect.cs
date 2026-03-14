using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace TowerBreakers.Player.View
{
    /// <summary>
    /// [설명]: 대쉬 등 빠른 움직임 시 잔상 효과를 비동기로 생성하고 관리합니다.
    /// 성능 확보를 위해 렌더러를 캐싱하고 내부 오브젝트 풀을 사용합니다.
    /// </summary>
    public class AfterimageEffect : MonoBehaviour
    {
        #region 에디터 설정
        [Header("효과 설정")]
        [SerializeField, Tooltip("잔상에 적용할 색상")]
        private Color m_afterimageColor = new Color(1f, 1f, 1f, 0.4f);

        [SerializeField, Tooltip("잔상이 생성되는 간격 (초)")]
        private float m_spawnInterval = 0.05f;

        [SerializeField, Tooltip("잔상이 완전히 사라지는 데 걸리는 시간 (초)")]
        private float m_fadeDuration = 0.3f;

        [SerializeField, Tooltip("잔상의 Sorting Order를 강제 지정할지 여부")]
        private bool m_overrideSortingOrder = false;

        [SerializeField, Tooltip("강제 지정할 Sorting Order 시작값")]
        private int m_sortingOrderOverrideValue = 10;

        [SerializeField, Tooltip("시작 시 미리 생성할 풀 크기")]
        private int m_initialPoolSize = 10;
        #endregion

        #region 내부 필드
        private PlayerView m_view;
        private CancellationTokenSource m_effectCts;
        private readonly List<SpriteRenderer> m_sourceRenderers = new List<SpriteRenderer>(32);
        private readonly Queue<AfterimageSnapshot> m_pool = new Queue<AfterimageSnapshot>();
        private bool m_isInitialized = false;
        #endregion

        #region 초기화
        /// <summary>
        /// [설명]: PlayerView를 통해 복제할 원본 렌더러들을 캐싱합니다.
        /// </summary>
        public void Initialize(PlayerView view)
        {
            m_view = view;
            if (m_view != null)
            {
                m_view.GetComponentsInChildren(true, m_sourceRenderers);
                CreateInitialPool();
                m_isInitialized = true;
            }
        }

        private void CreateInitialPool()
        {
            for (int i = 0; i < m_initialPoolSize; i++)
            {
                // [리팩토링]: 플레이어 이동에 영향을 받지 않도록 최상위에 생성
                GameObject obj = new GameObject($"AfterimageSnapshot_Pooled_{i}");
                AfterimageSnapshot snapshot = obj.AddComponent<AfterimageSnapshot>();
                obj.SetActive(false);
                m_pool.Enqueue(snapshot);
            }
        }
        #endregion

        #region 공개 API
        /// <summary>
        /// [설명]: 지정된 시간 동안 비동기로 잔상 효과를 실행합니다.
        /// </summary>
        /// <param name="duration">지속 시간 (0 이하면 수동 중지할 때까지 무한 재생)</param>
        public void StartEffect(float duration = -1f)
        {
            if (!m_isInitialized && m_view != null) Initialize(m_view);
            if (!m_isInitialized) return;

            StopEffect();

            m_effectCts = new CancellationTokenSource();
            var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(m_effectCts.Token, this.GetCancellationTokenOnDestroy()).Token;
            
            EffectLoopAsync(duration, linkedToken).Forget();
        }

        /// <summary>
        /// [설명]: 진행 중인 잔상 효과 루프를 중지합니다.
        /// </summary>
        public void StopEffect()
        {
            if (m_effectCts != null)
            {
                m_effectCts.Cancel();
                m_effectCts.Dispose();
                m_effectCts = null;
            }
        }
        #endregion

        #region 내부 로직
        private async UniTaskVoid EffectLoopAsync(float duration, CancellationToken token)
        {
            float timer = 0f;
            bool isInfinite = duration <= 0f;
            
            try
            {
                while ((isInfinite || timer < duration) && !token.IsCancellationRequested)
                {
                    SpawnSnapshot(token);
                    
                    await UniTask.Delay(TimeSpan.FromSeconds(m_spawnInterval), cancellationToken: token);
                    if (!isInfinite) timer += m_spawnInterval;
                }
            }
            catch (OperationCanceledException) { /* 정상 취소 */ }
        }

        private void SpawnSnapshot(CancellationToken token)
        {
            if (m_sourceRenderers.Count == 0) return;

            AfterimageSnapshot snapshot;
            if (m_pool.Count > 0)
            {
                snapshot = m_pool.Dequeue();
                snapshot.gameObject.SetActive(true);
            }
            else
            {
                // [리팩토링]: 플레이어 이동에 영향을 받지 않도록 최상위 계층에 생성
                GameObject obj = new GameObject("AfterimageSnapshot");
                snapshot = obj.AddComponent<AfterimageSnapshot>();
            }

            // 스냅샷 위치 초기화 (자식 렌더러들이 world scale을 직접 복사하므로 루트는 1로 유지)
            snapshot.transform.SetPositionAndRotation(m_view.transform.position, m_view.transform.rotation);
            snapshot.transform.localScale = Vector3.one;

            snapshot.ActivateAsync(
                m_sourceRenderers, 
                m_afterimageColor, 
                m_fadeDuration, 
                m_overrideSortingOrder, 
                m_sortingOrderOverrideValue,
                ReturnToPool,
                token
            ).Forget();
        }

        private void ReturnToPool(AfterimageSnapshot snapshot)
        {
            if (snapshot == null) return;
            snapshot.gameObject.SetActive(false);
            m_pool.Enqueue(snapshot);
        }
        #endregion

        #region 유니티 생명주기
        private void OnDestroy()
        {
            StopEffect();
        }
        #endregion
    }
}
