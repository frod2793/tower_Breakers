using UnityEngine;
using DG.Tweening;
using TowerBreakers.Core.Events;
using Cysharp.Threading.Tasks;

namespace TowerBreakers.Tower.View
{
    /// <summary>
    /// [설명]: 층 클리어 및 다음 층 전환 시 시각적 연출을 담당하는 클래스입니다.
    /// DOTween을 활용하여 카메라 또는 배경 배경을 이동시킵니다.
    /// </summary>
    public class TowerTransitionPresenter : MonoBehaviour
    {
        #region 에디터 설정
        [SerializeField, Tooltip("한 층의 높이 (이동 거리)")]
        private float m_floorHeight = 20.0f;

        [SerializeField, Tooltip("이동 연출 시간")]
        private float m_transitionDuration = 1.0f;

        [SerializeField, Tooltip("연출 대상 (카메라 또는 월드 루트)")]
        private Transform m_targetTransform;
        #endregion

        #region 내부 필드
        private IEventBus m_eventBus;
        #endregion

        #region 초기화
        public void Initialize(IEventBus eventBus)
        {
            m_eventBus = eventBus;
            m_eventBus.Subscribe<OnFloorCleared>(PlayTransition);
        }
        #endregion

        #region 비즈니스 로직
        /// <summary>
        /// [설명]: 다음 층으로 넘어가는 연출을 실행합니다.
        /// </summary>
        private void PlayTransition(OnFloorCleared evt)
        {
            if (m_targetTransform == null) return;

            // 위 섹션으로 올라가는 느낌 (카메라는 위로, 또는 월드 루트가 아래로)
            // m_floorHeight만큼 위(Y+)로 이동
            m_targetTransform.DOMoveY(m_targetTransform.position.y + m_floorHeight, m_transitionDuration)
                .SetEase(Ease.InOutSine);
            
            Debug.Log($"[TowerTransitionPresenter] {evt.FloorIndex}층 전환 연출 시작 (수직 이동)");
        }
        #endregion

        #region 해제
        private void OnDestroy()
        {
            m_eventBus?.Unsubscribe<OnFloorCleared>(PlayTransition);
        }
        #endregion
    }
}
