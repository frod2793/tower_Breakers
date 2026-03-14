using UnityEngine;
using TowerBreakers.Enemy.Data;
using TowerBreakers.Enemy.View;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 적이 플레이어를 향해 전진하며 밀어내는 상태입니다.
    /// </summary>
    public class EnemyPushState : IEnemyState
    {
        #region 내부 필드
        private readonly EnemyView m_view;
        private readonly EnemyData m_data;
        private readonly EnemyPushLogic m_pushLogic;
        private readonly EnemyController m_controller;
        #endregion

        #region 내부 변수
        private bool m_isMoving = false;
        #endregion

        public EnemyPushState(EnemyView view, EnemyData data, EnemyPushLogic pushLogic, EnemyController controller)
        {
            m_view = view;
            m_data = data;
            m_pushLogic = pushLogic;
            m_controller = controller;
        }

        public void OnEnter()
        {
            m_isMoving = false;
            // 초기 애니메이션 설정
            m_view.PlayAnimation(global::PlayerState.IDLE);
        }

        public void OnExit() { }

        public void OnTick()
        {
            // [성능/리팩토링]: 중복 전진 로직을 헬퍼로 통합하고 최적화 적용
            EnemyMovementHelper.ExecuteMovement(m_view, m_data, m_pushLogic, ref m_isMoving, m_controller);
        }
    }
}
