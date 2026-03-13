using UnityEngine;
using DG.Tweening;
using TowerBreakers.Core.Events;
using Cysharp.Threading.Tasks;
using TowerBreakers.Environment.Logic;
using VContainer;

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
        #endregion

        #region 초기화
        [Inject]
        public void Initialize(IEventBus eventBus, EnvironmentManager envManager)
        {
            m_eventBus = eventBus;
            m_envManager = envManager;

            string targetName = m_targetTransform != null ? m_targetTransform.name : "None";
            Debug.Log($"[TowerTransitionPresenter] Initialize 호출됨. Target: {targetName}, EventBus: {eventBus != null}, EnvManager: {envManager != null}");

            if (m_eventBus != null)
            {
                m_eventBus.Subscribe<OnFloorCleared>(PlayTransition);
                Debug.Log("[TowerTransitionPresenter] OnFloorCleared 이벤트 구독 완료");
            }
            else
            {
                Debug.LogError("[TowerTransitionPresenter] EventBus가 null입니다. 연출이 작동하지 않습니다.");
            }
        }
        #endregion

        #region 비즈니스 로직
        /// <summary>
        /// [설명]: 다음 층으로 넘어가는 연출을 실행합니다.
        /// </summary>
        private void PlayTransition(OnFloorCleared evt)
        {
            if (m_targetTransform == null)
            {
                Debug.LogError("[TowerTransitionPresenter] m_targetTransform이 설정되지 않았습니다! (인스펙터 확인 필요)");
                return;
            }

            // [동적 높이 설정]: EnvironmentManager 정보가 있으면 우선 사용
            float moveHeight = m_envManager != null ? m_envManager.DefaultSegmentHeight : m_floorHeight;

            // [지면 하강 방식]: 카메라가 올라가는 대신 지면(월드)이 내려가도록 음수 좌표 설정
            float targetY = -(evt.FloorIndex * moveHeight);

            Debug.Log($"[TowerTransitionPresenter] {evt.FloorIndex}층 지면 하강 연출 수신: TargetY={targetY:F2}, CurrentY={m_targetTransform.position.y:F2}");

            // 기존 트윈 제거 및 시퀀스 생성
            m_targetTransform.DOKill();
            Sequence seq = DOTween.Sequence();

            // 1. 지면 흔들림 (도약의 충격 표현)
            seq.Append(m_targetTransform.DOShakePosition(0.15f, m_shakeStrength, m_shakeVibrato));

            // 2. 수직 하강 (OutExpo 이징으로 강력한 상승감 부여)
            seq.Join(m_targetTransform.DOMoveY(targetY, m_transitionDuration)
                .SetEase(Ease.OutExpo)
                .OnStart(() => Debug.Log($"[TowerTransitionPresenter] {evt.FloorIndex}층 지면 하강 시작 (To: {targetY:F2})")));

            seq.OnComplete(() => {
                Debug.Log($"[TowerTransitionPresenter] {evt.FloorIndex}층 지면 하강 완료. 실제Y: {m_targetTransform.position.y:F2}");
            });
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
