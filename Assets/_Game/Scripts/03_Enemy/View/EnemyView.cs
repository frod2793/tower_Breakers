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
        #endregion

        #region 초기화
        public void Initialize()
        {
            if (m_spumPrefabs != null)
            {
                // 애니메이션 상태 및 컨트롤러 초기화 (풀링 재사용 시 필수)
                m_spumPrefabs.OverrideControllerInit();
                m_spumPrefabs.PlayAnimation(global::PlayerState.IDLE, 0);
            }

            // [추가]: 자식들의 모든 렌더러를 캐싱하여 피격 효과에 사용
            m_renderers = GetComponentsInChildren<SpriteRenderer>(true);
            ResetColor();
        }

        public void PlayAnimation(global::PlayerState state, int index = 0)
        {
            if (m_spumPrefabs != null)
            {
                m_spumPrefabs.PlayAnimation(state, index);
            }
        }
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
