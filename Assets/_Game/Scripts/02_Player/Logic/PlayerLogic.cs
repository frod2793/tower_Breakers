using System;
using UnityEngine;
using TowerBreakers.Player.DTO;
using TowerBreakers.Player.Stat;
using Cysharp.Threading.Tasks;

namespace TowerBreakers.Player.Logic
{
    #region 로직 클래스 (POCO)
    /// <summary>
    /// [설명]: 플레이어의 핵심 이동 및 전투 로직을 담당하는 순수 C# 클래스입니다.
    /// MonoBehaviour에 의존하지 않으며 트랜지션 및 상태 계산을 수행합니다.
    /// </summary>
    public class PlayerLogic
    {
        #region 내부 필드
        private readonly PlayerConfigDTO m_config;
        private readonly PlayerStateDTO m_state;
        private readonly IPlayerStatService m_statService;
        #endregion

        #region 프로퍼티
        public PlayerStateDTO State => m_state;
        public PlayerConfigDTO Config => m_config;

        public event Action OnDashStarted;
        public event Action OnParryStarted;
        public event Action OnAttackStarted;
        public event Action<GameObject> OnHit;
        public event Action OnDamaged;
        public event Action OnDeath;
        #endregion

        #region 초기화
        public PlayerLogic(PlayerConfigDTO config, IPlayerStatService statService)
        {
            m_config = config ?? new PlayerConfigDTO();
            m_statService = statService;
            m_state = new PlayerStateDTO();
        }
        #endregion

        #region 공개 메서드
        public void Update(float time, float deltaTime)
        {
            // 실제 이동은 View에서 진행하지만 상태 계산에 필요한 업데이트 수행
        }

        public bool TryDash(float time, float targetX)
        {
            if (time - m_state.LastDashTime < m_config.DashCooldown) return false;
            if (IsBusy()) return false;

            m_state.LastDashTime = time;
            m_state.IsDashing = true;
            m_state.Position = new Vector2(targetX, m_state.Position.y);
            OnDashStarted?.Invoke();
            return true;
        }

        public bool TryParry(float time)
        {
            if (time - m_state.LastParryTime < m_config.ParryCooldown) return false;
            if (IsBusy()) return false;

            m_state.LastParryTime = time;
            m_state.IsParrying = true;
            OnParryStarted?.Invoke();
            return true;
        }

        public bool TryAttack(float time)
        {
            if (time - m_state.LastAttackTime < m_config.AttackCooldown) return false;
            if (IsBusy()) return false;

            m_state.LastAttackTime = time;
            m_state.IsAttacking = true;
            OnAttackStarted?.Invoke();
            return true;
        }

        public void SetPosition(Vector2 position)
        {
            m_state.Position = position;
        }

        public void StartRetreat()
        {
            m_state.IsRetreating = true;
        }

        public void EndAction()
        {
            m_state.IsDashing = false;
            m_state.IsParrying = false;
            m_state.IsAttacking = false;
            m_state.IsRetreating = false;
        }

        public bool IsBusy()
        {
            return m_state.IsDashing || m_state.IsParrying || m_state.IsAttacking || m_state.IsRetreating;
        }

        public void ApplyExternalPush(Vector2 force)
        {
            Vector2 currentPos = m_state.Position;
            float newX = currentPos.x + force.x * Time.deltaTime;
            
            // Y값은 현재 위치 그대로 고정
            m_state.Position = new Vector2(newX, currentPos.y);

            // 왼쪽 벽 제한
            if (m_state.Position.x < m_config.LeftWallX)
            {
                m_state.Position = new Vector2(m_config.LeftWallX, currentPos.y);
            }
        }

        public void InitializeHealth(int health)
        {
            m_state.MaxHealth = health;
            m_state.Health = health;
        }

        public void TakeDamage(int damage)
        {
            m_state.Health = Math.Max(0, m_state.Health - damage);
            OnDamaged?.Invoke();
            
            if (m_state.Health <= 0)
            {
                Die();
            }
        }

        public void Die()
        {
            OnDeath?.Invoke();
        }
        #endregion
    }
    #endregion
}
