using UnityEngine;
using TowerBreakers.Enemy.View;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 적이 스폰된 후 실제 전투(진격)가 시작되기 전까지 대기하는 상태입니다.
    /// 플레이어가 해당 층에 도달하여 activation 이벤트를 받기 전까지 유지됩니다.
    /// </summary>
    public class EnemyWaitingState : IEnemyState
    {
        #region 내부 필드
        private readonly EnemyView m_view;
        #endregion

        public EnemyWaitingState(EnemyView view)
        {
            m_view = view;
        }

        public void OnEnter()
        {
            if (m_view != null)
            {
                m_view.PlayAnimation(global::PlayerState.IDLE);
                Debug.Log($"[EnemyWaitingState] {m_view.name} 대기 시작");
            }
        }

        public void OnExit()
        {
            if (m_view != null)
            {
                Debug.Log($"[EnemyWaitingState] {m_view.name} 대기 종료 (진격 개시)");
            }
        }

        public void OnTick()
        {
            // 이동 없음: 정지 상태 유지
        }
    }
}
