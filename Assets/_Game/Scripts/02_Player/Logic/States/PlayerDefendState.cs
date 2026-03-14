using UnityEngine;
using TowerBreakers.Player.View;
using TowerBreakers.Enemy.Logic;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TowerBreakers.Core.Events;
using TowerBreakers.Core;
using TowerBreakers.Player.Data.SO;
using TowerBreakers.Player.Data.Models;

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
        private readonly PlayerData m_playerData;
        private readonly Tower.Logic.TowerManager m_towerManager;
        #endregion

        public PlayerDefendState(
            PlayerView view, 
            PlayerModel model,
            PlayerPushReceiver pushReceiver,
            PlayerStateMachine stateMachine, 
            Core.Events.IEventBus eventBus,
            PlayerData playerData,
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
            // [추가]: 방어 사거리 내에 물리적인 적이 있는지 선행 체크
            float defendRange = m_playerData != null ? m_playerData.DefendRange : 2.5f;
            bool hasEnemy = IsEnemyInRange(defendRange);

            if (!hasEnemy)
            {
                m_stateMachine.ChangeState<PlayerIdleState>();
                return;
            }

            // 1. 적군에 대한 패링 이벤트 전파
            float pushbackDistance = m_playerData != null ? m_playerData.DefendPushbackDistance : 3.0f;
            float stunDuration = m_playerData != null ? m_playerData.DefendStunDuration : 2.0f;

            if (m_eventBus != null)
            {
                int currentFloor = m_towerManager != null ? m_towerManager.CurrentFloorIndex : 0;
                m_eventBus.Publish(new OnDefendActionTriggered(stunDuration, pushbackDistance, defendRange, currentFloor, m_view.transform.position));
            }

            // 2. 플레이어의 현재 위치 및 벽 거리 계산
            if (m_model != null && m_pushReceiver != null)
            {
                float wallX = m_pushReceiver.LeftWallThreshold;
                float currentX = m_view.transform.position.x;
                float thresholdX = m_pushReceiver.BackflipThresholdX;
                
                if (currentX >= thresholdX)
                {
                    ExecuteBackflip(wallX);
                }
                else
                {
                    m_view.PlayAnimation(global::PlayerState.OTHER, 1);
                    
                    // [추가]: 후퇴 연출 중 무적 부여 (0.3s 이동 + 여유분)
                    m_model.SetInvincibility(0.4f);

                    // 경계 제한 해제 (연출 중 벽 압착 판정 방지)
                    m_pushReceiver.IsClampingEnabled = false;
                    
                    m_view.transform.DOMoveX(wallX, 0.3f)
                        .SetId("DefendPush") // 독립 제어를 위해 ID 부여
                        .SetEase(Ease.OutBack)
                        .OnUpdate(UpdatePositionFromDefend)
                        .OnComplete(() => 
                        {
                            // 상태 확인 후 안전하게 복귀
                            if (m_stateMachine != null && m_stateMachine.IsCurrentState<PlayerDefendState>())
                            {
                                m_stateMachine.ChangeState<PlayerIdleState>();
                            }
                        });
                }
            }
        }

        #region 내부 로직
        /// <summary>
        /// [설명]: DOTween을 사용하여 애니메이션 클립 없이 백플립(회전+점프) 연출을 실행합니다.
        /// 수정: 상태가 전환되더라도 공중에서 멈추지 않도록 트윈을 독립형으로 분리합니다.
        /// </summary>
        /// <param name="wallX">왼쪽 벽 한계 좌표</param>
        private void ExecuteBackflip(float wallX)
        {
            if (m_view == null || m_pushReceiver == null || m_playerData == null) return;

            // 1. 방향 및 목표 지점 계산
            float facingSign = Core.Utilities.DirectionHelper.GetFacingSign(m_view.transform);
            float currentY = m_view.transform.localEulerAngles.y;
            Vector3 startPos = m_view.transform.position;

            float targetX = wallX;

            // 2. 연출 초기 설정 (기존 백플립 트윈 제거 및 일반 트윈 정리)
            m_pushReceiver.IsClampingEnabled = false;
            DOTween.Kill("PlayerBackflip");
            m_view.transform.DOKill();
            
            // 회전값 초기화 (방향 유지)
            m_view.transform.localRotation = Quaternion.Euler(0, currentY, 0);

            float jumpPower = m_playerData.BackflipJumpPower;
            float duration = 0.75f; // 2회전을 위해 시간을 약간 늘림 (0.65 -> 0.75)

            // 무적 부여
            m_model.SetInvincibility(0.85f);

            // 3. DOTween 연출 실행 (SetTarget을 분리하여 transform.DOKill에 의해 죽지 않도록 독립형 구성)
            // ① 점프 이동
            m_view.transform.DOJump(new Vector3(targetX, startPos.y, 0), jumpPower, 1, duration)
                .SetId("PlayerBackflip")
                .SetTarget(m_model)
                .SetEase(Ease.OutQuad)
                .OnUpdate(UpdatePositionFromBackflip);

            // ② 공중 2회전 (720도)
            float rotateAmount = facingSign * 720f; 
            
            m_view.transform.DORotate(new Vector3(0, currentY, rotateAmount), duration, RotateMode.FastBeyond360)
                .SetId("PlayerBackflip")
                .SetTarget(m_model)
                .SetEase(Ease.InOutQuad)
                .OnComplete(() =>
                {
                    // 물리 및 상태 복구
                    if (m_view != null) m_view.transform.localRotation = Quaternion.Euler(0, currentY, 0);
                    if (m_pushReceiver != null) m_pushReceiver.IsClampingEnabled = true;
                    
                    if (m_model != null && m_view != null) m_model.Position = m_view.transform.position;
                    
                    if (m_stateMachine != null && m_stateMachine.IsCurrentState<PlayerDefendState>())
                    {
                        m_stateMachine.ChangeState<PlayerIdleState>();
                    }
                });
        }

        /// <summary>
        /// [설명]: 방어 사거리 내에 적이 존재하는지 물리적으로 검사합니다.
        /// </summary>
        /// <param name="defendRange">검사 반경</param>
        private bool IsEnemyInRange(float defendRange)
        {
            if (m_view == null) return false;
            
            int count = Physics2D.OverlapCircle(
                m_view.transform.position, 
                defendRange, 
                PhysicsQueryUtil.EnemyOnlyFilter, 
                PhysicsQueryUtil.SingleTargetBuffer);
                
            return count > 0;
        }

        private void UpdatePositionFromDefend()
        {
            m_model.Position = m_view.transform.position;
        }

        private void UpdatePositionFromBackflip()
        {
            if (m_model != null) m_model.Position = m_view.transform.position;
        }
        #endregion

        public void OnExit()
        {
            // [수정]: 전체 트윈 제거(m_view.transform.DOKill())를 제거하여 독립형 백플립 트윈이 죽지 않도록 방지
            // 대신 방어 시전으로 발동된 일반 후퇴 트윈만 제거
            DOTween.Kill("DefendPush");
            DOTween.Kill("PlayerBackflip");
            
            // 상태 복구
            if (m_pushReceiver != null)
            {
                m_pushReceiver.IsClampingEnabled = true;
            }
        }

        public void OnTick() { }
    }
}
