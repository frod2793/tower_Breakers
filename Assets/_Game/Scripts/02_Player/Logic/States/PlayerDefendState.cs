using UnityEngine;
using TowerBreakers.Player.View;
using TowerBreakers.Enemy.Logic;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TowerBreakers.Core.Events;
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

        // [최적화]: 사거리 내 적 체크를 위한 캐싱 필드
        private static readonly int s_enemyLayer = LayerMask.GetMask("Enemy");
        private static readonly Collider2D[] s_overlapBuffer = new Collider2D[1];
        private static readonly ContactFilter2D s_enemyFilter = CreateEnemyFilter();

        private static ContactFilter2D CreateEnemyFilter()
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(s_enemyLayer);
            filter.useLayerMask = true;
            return filter;
        }
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
                    
                    // 경계 제한 해제 (연출 중 벽 압착 판정 방지)
                    m_pushReceiver.IsClampingEnabled = false;
                    
                    m_view.transform.DOMoveX(wallX, 0.3f)
                        .SetEase(Ease.OutBack)
                        .OnUpdate(() => m_model.Position = m_view.transform.position)
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
        /// </summary>
        /// <param name="targetX">착지할 목표 X 좌표 (벽 위치)</param>
        private void ExecuteBackflip(float targetX)
        {
            if (m_view == null || m_pushReceiver == null) return;

            // [수정]: 현재 바라보는 방향(Y축 회전)을 저장하여 연출 중/후에도 유지되도록 합니다.
            float currentY = m_view.transform.localEulerAngles.y;

            // 1. 연출 초기 설정
            m_pushReceiver.IsClampingEnabled = false; // 경계 제한 해제
            m_view.transform.DOKill();                 // 기존 트윈 중지
            
            // 회전값 및 데이터 초기화 (Y축 방향은 유지)
            m_view.transform.localRotation = Quaternion.Euler(0, currentY, 0);

            float startY = m_view.transform.position.y;
            float jumpPower = 3.5f;    // 점프 높이(힘)
            float duration = 0.65f;     // 전체 연출 시간

            // 2. DOTween 연출 실행
            // ① 포물선 이동 (Jump): 벽까지 예쁘게 날아갑니다.
            m_view.transform.DOJump(new Vector3(targetX, startY, 0), jumpPower, 1, duration)
                .SetEase(Ease.OutQuad)
                .OnUpdate(() => 
                {
                    if (m_model != null) m_model.Position = m_view.transform.position;
                });

            // ② 공중 회전 (Rotate): 뒤로 360도 회전 (Backflip)
            // Y축 회전(방향)을 고정한 상태에서 Z축만 -360도 회전합니다.
            m_view.transform.DORotate(new Vector3(0, currentY, -360f), duration, RotateMode.FastBeyond360)
                .SetEase(Ease.InOutQuad)
                .OnComplete(() =>
                {
                    // 물리 및 상태 복구 (원래 바라보던 방향 유지)
                    m_view.transform.localRotation = Quaternion.Euler(0, currentY, 0);
                    m_pushReceiver.IsClampingEnabled = true;
                    
                    if (m_model != null) m_model.Position = m_view.transform.position;
                    
                    // 상태 확인 후 안전하게 복귀
                    if (m_stateMachine != null && m_stateMachine.IsCurrentState<PlayerDefendState>())
                    {
                        m_stateMachine.ChangeState<PlayerIdleState>();
                    }
                });


        }

        private async Cysharp.Threading.Tasks.UniTaskVoid ReturnToIdleAfterDelay()
        {
            await Cysharp.Threading.Tasks.UniTask.Delay(500);
            m_stateMachine.ChangeState<PlayerIdleState>();
        }

        /// <summary>
        /// [설명]: 방어 사거리 내에 적이 존재하는지 물리적으로 검사합니다.
        /// </summary>
        /// <param name="defendRange">검사 반경</param>
        private bool IsEnemyInRange(float defendRange)
        {
            if (m_view == null) return false;
            
            // [최적화]: Deprecated된 NonAlloc 대신 ContactFilter2D 기반의 OverlapCircle 사용 (GC 할당 없음)
            int count = Physics2D.OverlapCircle(
                m_view.transform.position, 
                defendRange, 
                s_enemyFilter, 
                s_overlapBuffer);
                
            return count > 0;
        }
        #endregion

        public void OnExit()
        {
            // 진행 중인 트윈 제거 (잔존 연출 방지)
            if (m_view != null)
            {
                m_view.transform.DOKill();
            }
            
            // 상태 복구
            if (m_pushReceiver != null)
            {
                m_pushReceiver.IsClampingEnabled = true;
            }
        }

        public void OnTick() { }
    }
}
