using UnityEngine;
using TowerBreakers.Enemy.Data;
using TowerBreakers.Enemy.View;
using TowerBreakers.Player.Logic;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 플레이어에게 투사체를 발사하는 상태입니다.
    /// </summary>
    public class EnemyShootState : IEnemyState
    {
        #region 내부 필드
        private readonly EnemyView m_view;
        private readonly EnemyData m_data;
        private readonly EnemyStateMachine m_stateMachine;
        private readonly ProjectileFactory m_projectileFactory;
        private readonly PlayerPushReceiver m_playerTarget;

        private float m_timer;
        #endregion

        public EnemyShootState(EnemyView view, EnemyData data, EnemyStateMachine stateMachine, ProjectileFactory projectileFactory, PlayerPushReceiver playerTarget)
        {
            m_view = view;
            m_data = data;
            m_stateMachine = stateMachine;
            m_projectileFactory = projectileFactory;
            m_playerTarget = playerTarget;
        }

        public void OnEnter()
        {
            m_timer = 0f;
            
            // 발사 애니메이션
            m_view.PlayAnimation(global::PlayerState.ATTACK);
            
            // 투사체 생성
            if (m_projectileFactory != null && m_data.ProjectilePrefab != null)
            {
                // 적 위치에서 약간 앞에서 생성
                Vector3 spawnPos = m_view.transform.position + Vector3.left * 0.5f + Vector3.up * 0.5f;
                m_projectileFactory.Create(m_data.ProjectilePrefab, spawnPos, 5.0f, m_data.ProjectilePushDistance, m_playerTarget);
            }
            
            // Debug.Log($"[EnemyShootState] {m_view.name} 투사체 발사"); // [로그 제거]: 콘솔 노이즈 방지
        }

        public void OnExit() { }

        public void OnTick()
        {
            m_timer += Time.deltaTime;
            
            if (m_timer >= m_data.AbilityDuration)
            {
                m_stateMachine.ChangeState<EnemySupportPushState>();
            }
        }
    }
}
