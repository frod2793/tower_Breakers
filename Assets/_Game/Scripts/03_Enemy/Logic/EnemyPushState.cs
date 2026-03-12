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
        #endregion

        public EnemyPushState(EnemyView view, EnemyData data, EnemyPushLogic pushLogic)
        {
            m_view = view;
            m_data = data;
            m_pushLogic = pushLogic;
        }

        public void OnEnter()
        {
            // 애니메이션 재생 등
        }

        public void OnExit() { }

        public void OnTick()
        {
            // 기차 대열 유지를 위해 전방 확인 (간격 1.5 유지)
            if (!m_pushLogic.IsBlocked(1.5f))
            {
                // Y축 변화 없이 X축으로만 정교하게 이동
                Vector3 currentPos = m_view.transform.position;
                float nextX = currentPos.x - (m_data.MoveSpeed * Time.deltaTime);
                m_view.transform.position = new Vector3(nextX, currentPos.y, currentPos.z);
            }
            
            // 밀기 로직 실행 (접촉 시)
            m_pushLogic.TryPushPlayer();
        }
    }
}
