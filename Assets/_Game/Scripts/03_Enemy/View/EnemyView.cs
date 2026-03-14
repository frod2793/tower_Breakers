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
        private bool m_isInitialized = false;
        #endregion

        #region 초기화
        /// <summary>
        /// [설명]: 첫 생성 시 1회 호출되어 렌더러 캐싱 및 애니메이터 설정을 수행합니다.
        /// </summary>
        public void Initialize()
        {
            if (m_isInitialized) return;

            if (m_spumPrefabs != null)
            {
                // Animator 참조가 없으면 자식에서 자동 탐색
                if (m_spumPrefabs._anim == null)
                {
                    m_spumPrefabs._anim = m_spumPrefabs.GetComponentInChildren<Animator>();
                }

                if (m_spumPrefabs._anim != null)
                {
                    // 애니메이션 상태 및 컨트롤러 초기화 (최초 1회만 수행)
                    m_spumPrefabs.OverrideControllerInit();
                }
                else
                {
                    Debug.LogError($"[EnemyView] {gameObject.name}: SPUM Animator를 찾을 수 없습니다.");
                }

                m_cachedAnimator = m_spumPrefabs._anim;
            }

            m_renderers = GetComponentsInChildren<SpriteRenderer>(true);
            m_isInitialized = true;
            
            ResetState();
        }

        /// <summary>
        /// [설명]: 풀링에서 재사용될 때 호출되어 시각적 상태와 애니메이션을 초기화합니다.
        /// 무거운 OverrideControllerInit 호출을 방지합니다.
        /// </summary>
        public void ResetState()
        {
            ResetColor();
            
            if (m_spumPrefabs != null)
            {
                m_spumPrefabs.PlayAnimation(global::PlayerState.IDLE, 0);
            }
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
        /// [최적화]: 렌더러당 개별 트윈 생성 대신, 단 하나의 가상 트윈으로 모든 렌더러 일괄 업데이트
        /// </summary>
        public void PlayHitEffect()
        {
            if (m_renderers == null || m_renderers.Length == 0) return;

            // 기존 진행 중인 뷰 타겟 트윈 종료
            DOTween.Kill(this);

            // [최적화]: 가상 트윈을 활용하여 0.0(Red) -> 1.0(White)으로 변하는 값을 모든 렌더러에 적용
            // 루프 내부의 null 체크 비용을 줄이기 위해 초기 캐싱 시 필터링 수행 검토 가능하나, 
            // 여기서는 루프의 효율성에 집중합니다.
            DOVirtual.Float(0f, 1f, 0.15f, (value) =>
            {
                Color targetColor = Color.Lerp(Color.red, Color.white, value);
                int len = m_renderers.Length;
                for (int i = 0; i < len; i++)
                {
                    var renderer = m_renderers[i];
                    if (renderer != null)
                    {
                        renderer.color = targetColor;
                    }
                }
            })
            .SetTarget(this)
            .SetEase(Ease.OutSine);
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
