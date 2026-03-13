using UnityEngine;
using DG.Tweening;

namespace TowerBreakers.Enemy.View
{
    /// <summary>
    /// [설명]: SPUM 캐릭터를 활용한 적 뷰 클래스입니다.
    /// </summary>
    public class EnemyView : MonoBehaviour
    {
        #region 에디터 설정
        [SerializeField, Tooltip("SPUM 프리팹 참조")]
        private SPUM_Prefabs m_spumPrefabs;
        #endregion

        #region 내부 변수
        private SpriteRenderer[] m_renderers;
        private Animator m_cachedAnimator;
        #endregion

        #region 초기화
        public void Initialize()
        {
            if (m_spumPrefabs != null)
            {
                // Animator 참조가 없으면 자식에서 자동 탐색
                if (m_spumPrefabs._anim == null)
                {
                    m_spumPrefabs._anim = m_spumPrefabs.GetComponentInChildren<Animator>();
                }

                if (m_spumPrefabs._anim != null)
                {
                    // 애니메이션 상태 및 컨트롤러 초기화 (풀링 재사용 시 필수)
                    m_spumPrefabs.OverrideControllerInit();
                    m_spumPrefabs.PlayAnimation(global::PlayerState.IDLE, 0);
                }
                else
                {
                    Debug.LogError($"[EnemyView] {gameObject.name}: SPUM Animator를 찾을 수 없습니다.");
                }

                // [추가]: 애니메이터 캐싱
                m_cachedAnimator = m_spumPrefabs._anim;
            }

            // [추가]: 자식들의 모든 렌더러를 캐싱하여 피격 효과에 사용
            m_renderers = GetComponentsInChildren<SpriteRenderer>(true);
            ResetColor();
        }

        public void PlayAnimation(global::PlayerState state, int index = 0)
        {
            if (m_spumPrefabs == null) return;

            // StateAnimationPairs가 비어있으면 초기화가 안 된 상태이므로 재초기화 시도
            if (m_spumPrefabs.StateAnimationPairs == null || m_spumPrefabs.StateAnimationPairs.Count == 0)
            {
                Debug.LogWarning($"[EnemyView] StateAnimationPairs 미초기화 감지 — OverrideControllerInit 재시도");
                m_spumPrefabs.OverrideControllerInit();
            }

            string key = state.ToString();
            if (!m_spumPrefabs.StateAnimationPairs.ContainsKey(key))
            {
                Debug.LogWarning($"[EnemyView] 애니메이션 키 '{key}' 없음 — 스킵");
                return;
            }

        m_spumPrefabs.PlayAnimation(state, index);
        }

        /// <summary>
        /// [설명]: 캐싱된 애니메이터를 반환합니다. (성능 최적화용)
        /// </summary>
        public Animator CachedAnimator => m_cachedAnimator;
        #endregion

        #region 피격 효과 (Visual Feedback)
        /// <summary>
        /// [설명]: 적 캐릭터 전체가 붉은색으로 깜빡이는 피격 시각 효과를 재생합니다.
        /// </summary>
        public void PlayHitEffect()
        {
            if (m_renderers == null || m_renderers.Length == 0) return;

            // 모든 렌더러에 대해 빨간색으로 변경 후 원래 색(White)으로 복귀하는 트윈 적용
            foreach (var r in m_renderers)
            {
                if (r != null)
                {
                    // 현재 진행 중인 색상 트윈을 덮어씀
                    r.DOColor(Color.red, 0.1f)
                     .OnComplete(() => r.DOColor(Color.white, 0.1f));
                }
            }
        }

        /// <summary>
        /// [설명]: 풀링 재사용 또는 상태 초기화 시 색상을 원상태로 되돌립니다.
        /// </summary>
        public void ResetColor()
        {
            if (m_renderers == null) return;
            
            foreach (var r in m_renderers)
            {
                if (r != null)
                {
                    r.DOKill(); // 기존 트윈 강제 종료
                    r.color = Color.white;
                }
            }
        }
        #endregion
    }
}
