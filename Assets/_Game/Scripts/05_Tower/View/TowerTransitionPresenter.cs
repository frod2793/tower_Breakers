using UnityEngine;
using DG.Tweening;
using TowerBreakers.Core.Events;
using Cysharp.Threading.Tasks;
using TowerBreakers.Environment.Logic;
using TowerBreakers.Tower.Logic;
using VContainer;

namespace TowerBreakers.Tower.View
{
    /// <summary>
    /// [설명]: 층 클리어 및 다음 층 전환 시 시각적 연출을 담당하는 클래스입니다.
    /// DOTween을 활용하여 타워(월드)를 하강시켜 플레이어가 상승하는 느낌을 줍니다.
    /// </summary>
    public class TowerTransitionPresenter : MonoBehaviour
    {
        #region 에디터 설정
        [SerializeField, Tooltip("이동 연출 시간")]
        private float m_transitionDuration = 0.5f;

        [SerializeField, Tooltip("연출 대상 (카메라 또는 월드 루트)")]
        private Transform m_targetTransform;

        [Header("상승 연출 설정")]
        [SerializeField, Tooltip("상승 시작 시 흔들림 강도")]
        private float m_shakeStrength = 0.2f;

        [SerializeField, Tooltip("상승 시작 시 흔들림 진동 횟수")]
        private int m_shakeVibrato = 10;
        #endregion

        #region 내부 필드
        private IEventBus m_eventBus;
        private EnvironmentManager m_envManager;
        private TowerManager m_towerManager;
        #endregion

        #region 초기화
        [Inject]
        public void Initialize(IEventBus eventBus, EnvironmentManager envManager, TowerManager towerManager)
        {
            m_eventBus = eventBus;
            m_envManager = envManager;
            m_towerManager = towerManager;

            if (m_eventBus != null)
            {
                m_eventBus.Subscribe<OnFloorCleared>(PlayTransition);
            }
            else
            {
                Debug.LogError("[TowerTransitionPresenter] EventBus가 null입니다.");
            }
        }
        #endregion

        #region 비즈니스 로직
        /// <summary>
        /// [설명]: 다음 층으로 넘어가는 하강 연출을 실행합니다.
        /// OnFloorCleared.FloorIndex는 클리어된 층 인덱스이므로, 이동 대상은 다음 층(CurrentFloorIndex)입니다.
        /// </summary>
        /// <param name="evt">층 클리어 이벤트 데이터</param>
        private void PlayTransition(OnFloorCleared evt)
        {
            if (m_targetTransform == null)
            {
                Debug.LogWarning("[TowerTransitionPresenter] 연출 대상(m_targetTransform)이 지정되지 않았습니다.");
                return;
            }

            // [수정]: TowerManager의 현재 층 인덱스를 사용하여 정확한 카메라 위치 계산
            // NextFloor() 호출 후 이벤트가 발행되므로 CurrentFloorIndex는 이미 다음 층을 가리킴
            int nextFloorIndex = m_towerManager != null ? m_towerManager.CurrentFloorIndex : evt.FloorIndex + 1;

            // [수정]: 세그먼트 실제 높이를 EnvironmentManager에서 직접 참조
            float segmentHeight = m_envManager != null ? m_envManager.DefaultSegmentHeight : 15.0f;
            float targetY = -(nextFloorIndex * segmentHeight);

            // 기존 연출 중단 및 초기화
            m_targetTransform.DOKill();

            Sequence seq = DOTween.Sequence();

            // 1. 도약 충격 흔들림
            seq.Append(m_targetTransform.DOShakePosition(0.15f, m_shakeStrength, m_shakeVibrato));

            // 2. 부드러운 하강 (OutExpo)
            seq.Join(m_targetTransform.DOMoveY(targetY, m_transitionDuration)
                .SetEase(Ease.OutExpo));

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            seq.OnStart(() => Debug.Log($"[TowerTransitionPresenter] {evt.FloorIndex}층 클리어 → {nextFloorIndex}층 이동 시작 (targetY={targetY})"))
               .OnComplete(() => Debug.Log($"[TowerTransitionPresenter] {nextFloorIndex}층 이동 완료"));
            #endif
        }
        #endregion

        #region 유니티 생명주기
        private void OnDestroy()
        {
            if (m_eventBus != null)
            {
                m_eventBus.Unsubscribe<OnFloorCleared>(PlayTransition);
            }
        }
        #endregion
    }
}
