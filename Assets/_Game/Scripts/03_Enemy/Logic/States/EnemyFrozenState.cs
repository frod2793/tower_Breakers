using UnityEngine;
using TowerBreakers.Enemy.View;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 벽 압착 발생 시 적의 전진을 완전 정지시키는 동결 상태입니다.
    /// 플레이어의 도약(Leap) 또는 방어(Defend) 액션이 발생할 때까지 대기합니다.
    /// </summary>
    public class EnemyFrozenState : IEnemyState
    {
        #region 내부 필드
        private readonly EnemyView m_view;
        #endregion

        public EnemyFrozenState(EnemyView view)
        {
            m_view = view;
        }

        public void OnEnter()
        {
            // 정지 상태 애니메이션
            if (m_view != null)
            {
                m_view.PlayAnimation(global::PlayerState.IDLE);
                // [로그 제거]: 콘솔 노이즈 방지
            }
        }

        public void OnExit()
        {
            if (m_view != null)
            {
                // [로그 제거]: 콘솔 노이즈 방지
            }
        }

        public void OnTick()
        {
            // 의도적으로 비어있음: 이동 없음, 타이머 없음
            // 방어 이벤트에 의해 EnemyController에서 상태 전환됨
        }
    }
}
