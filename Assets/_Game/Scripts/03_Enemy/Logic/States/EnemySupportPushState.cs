using UnityEngine;
using TowerBreakers.Enemy.Data;
using TowerBreakers.Enemy.View;
using System;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 서포터 유닛을 위한 전진 상태입니다. 
    /// 일반 전진 로직을 수행하면서 일정 주기마다 지정된 특수 능력 상태로 전환합니다.
    /// </summary>
    public class EnemySupportPushState : IEnemyState
    {
        #region 내부 필드
        private readonly EnemyView m_view;
        private readonly EnemyData m_data;
        private readonly EnemyPushLogic m_pushLogic;
        private readonly EnemyStateMachine m_stateMachine;
        private readonly Type m_actionStateType;
        private readonly EnemyController m_controller;

        private float m_cooldownTimer;
        private bool m_isMoving = false;
        #endregion

        public EnemySupportPushState(EnemyView view, EnemyData data, EnemyPushLogic pushLogic, EnemyStateMachine stateMachine, Type actionStateType, EnemyController controller)
        {
            m_view = view;
            m_data = data;
            m_pushLogic = pushLogic;
            m_stateMachine = stateMachine;
            m_actionStateType = actionStateType;
            m_controller = controller;
        }

        public void OnEnter()
        {
            m_isMoving = false;
            m_view.PlayAnimation(global::PlayerState.IDLE);
            // 진입 시 쿨다운 타이머는 유지 (상태 전환 간 쿨다운 연속성 보장)
        }

        public void OnExit() { }

        public void OnTick()
        {
            // 1. 쿨다운 체크 및 상태 전환
            m_cooldownTimer += Time.deltaTime;
            if (m_cooldownTimer >= m_data.AbilityCooldown)
            {
                m_cooldownTimer = 0f;
                m_stateMachine.ChangeState(m_actionStateType);
                return;
            }

            // 2. [성능/리팩토링]: 중복 전진 로직을 헬퍼로 통합하고 최적화 적용
            EnemyMovementHelper.ExecuteMovement(m_view, m_data, m_pushLogic, ref m_isMoving, m_controller);
        }
    }
}
