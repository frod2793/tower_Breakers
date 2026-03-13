using UnityEngine;
using TowerBreakers.Player.View;
using TowerBreakers.Enemy.Logic;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TowerBreakers.Core.Events;
using TowerBreakers.Player.Data;

namespace TowerBreakers.Player.Logic
{
    /// <summary>
    /// [설명]: 플레이어의 방어(Defend) 상태입니다. 전방 적 행렬을 일시 기절시킵니다.
    /// </summary>
    public class PlayerDefendState : IPlayerState
    {
        #region 내부 필드
        private readonly PlayerView m_view;
        private readonly PlayerModel m_model;
        private readonly PlayerPushReceiver m_pushReceiver;
        private readonly PlayerStateMachine m_stateMachine;
        private readonly Core.Events.IEventBus m_eventBus;
        private readonly Player.Data.PlayerData m_playerData;
        private readonly Tower.Logic.TowerManager m_towerManager;
        #endregion

        public PlayerDefendState(
            PlayerView view, 
            PlayerModel model,
            PlayerPushReceiver pushReceiver,
            PlayerStateMachine stateMachine, 
            Core.Events.IEventBus eventBus,
            Player.Data.PlayerData playerData,
            Tower.Logic.TowerManager towerManager)
        {
            m_view = view;
            m_model = model;
            m_pushReceiver = pushReceiver;
            m_stateMachine = stateMachine;
            m_eventBus = eventBus;
            m_playerData = playerData;
            m_towerManager = towerManager;
        }

        public void OnEnter()
        {
            Debug.Log("[PlayerParry] 패링(기절+밀어내기+벽복귀) 실행");
            
            // 1. 적군에 대한 패링 이벤트 전파
            float pushbackDistance = m_playerData != null ? m_playerData.DefendPushbackDistance : 3.0f;
            if (m_eventBus != null)
            {
                int currentFloor = m_towerManager != null ? m_towerManager.CurrentFloorIndex : 0;
                float defendRange = m_playerData != null ? m_playerData.DefendRange : 2.5f;
                m_eventBus.Publish(new OnDefendActionTriggered(2.0f, pushbackDistance, defendRange, currentFloor));
            }

            // 2. 플레이어의 현재 위치 및 벽 거리 계산
            if (m_model != null && m_pushReceiver != null)
            {
                float wallX = m_pushReceiver.LeftWallThreshold;
                float currentX = m_model.Position.x;
                float thresholdX = m_pushReceiver.BackflipThresholdX;
                // [디버그]: 백플립 판정 수치 확인
                Debug.Log($"[PlayerParry] 위치 체크 - CurrentX: {currentX:F2}, ThresholdX: {thresholdX:F2}, 조건만족: {currentX >= thresholdX}");

                // 모든 트윈 중지 후 이동
                m_view.transform.DOKill();

                // 지정된 기점(thresholdX)을 기준으로 연출 분기
                if (currentX >= thresholdX)
                {
                    // [백플립 패링]
                    ExecuteBackflip(wallX);
                }
                else
                {
                    // [일반 슬라이딩 패링]
                    m_view.PlayAnimation(global::PlayerState.OTHER, 1); // 슬라이드 인덱스 1 시도
                    
                    // 벽까지 빠르게 밀려남 (0.3초)
                    m_view.transform.DOMoveX(wallX, 0.3f)
                        .SetEase(Ease.OutBack)
                        .OnUpdate(() => m_model.Position = m_view.transform.position)
                        .OnComplete(() => m_stateMachine.ChangeState<PlayerIdleState>());
                }
            }
        }

        #region 내부 로직
        /// <summary>
        /// [설명]: DOTween Sequence를 사용하여 백플립 연출을 실행합니다.
        /// </summary>
        /// <param name="targetX">착지할 목표 X 좌표 (벽 위치)</param>
        private void ExecuteBackflip(float targetX)
        {
            if (m_view == null || m_pushReceiver == null) return;

            // 1. 연출 설정
            m_view.PlayAnimation(global::PlayerState.OTHER, 2); // 백플립 애니메이션
            m_pushReceiver.IsClampingEnabled = false;          // 경계 제한 해제

            float startY = m_view.transform.position.y;
            float jumpHeight = 3.5f;   // 점프 높이
            float duration = 0.65f;    // 전체 연출 시간

            // 2. DOTween Sequence 생성
            Sequence backflipSeq = DOTween.Sequence();

            // ① 상승 및 이동 시작
            backflipSeq.Append(m_view.transform.DOMoveY(startY + jumpHeight, duration * 0.4f).SetEase(Ease.OutCubic));
            backflipSeq.Join(m_view.transform.DOMoveX(targetX, duration).SetEase(Ease.InOutQuad));

            // ② 정점 도달 후 하강
            backflipSeq.Append(m_view.transform.DOMoveY(startY, duration * 0.6f).SetEase(Ease.InCubic));

            // ③ 데이터 동기화 및 마무리
            backflipSeq.OnUpdate(() =>
            {
                if (m_model != null)
                {
                    m_model.Position = m_view.transform.position;
                }
            });

            backflipSeq.OnComplete(() =>
            {
                m_pushReceiver.IsClampingEnabled = true;
                m_stateMachine.ChangeState<PlayerIdleState>();
            });
        }

        private async Cysharp.Threading.Tasks.UniTaskVoid ReturnToIdleAfterDelay()
        {
            await Cysharp.Threading.Tasks.UniTask.Delay(500);
            m_stateMachine.ChangeState<PlayerIdleState>();
        }
        #endregion

        public void OnExit() { }

        public void OnTick() { }
    }
}
