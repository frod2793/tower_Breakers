using System;
using UnityEngine;
using TowerBreakers.Player.DTO;
using TowerBreakers.Player.Stat;
using TowerBreakers.Player.Service;
using Cysharp.Threading.Tasks;
using TowerBreakers.Enemy.Service;
using TowerBreakers.Core.Events;

namespace TowerBreakers.Player.Logic
{
    #region 로직 클래스 (POCO)
    /// <summary>
    /// [설명]: 플레이어의 핵심 이동 및 전투 로직을 담당하는 순수 C# 클래스입니다.
    /// 모든 이동 연산은 이 클래스에서 완결되며, 뷰는 결과 좌표를 시각화하기만 합니다.
    /// </summary>
    public class PlayerLogic
    {
        #region 내부 필드
        private readonly PlayerConfigDTO m_config;
        private readonly PlayerStateDTO m_state;
        private readonly IPlayerStatService m_statService;
        private readonly IEnemyDetectionService m_enemyDetection;
        private readonly IEventBus m_eventBus;
        
        private float m_pushTimer = 0f;
        private float m_pendingPushVelocityX = 0f;
        private float m_forcedPushX = float.MaxValue; // [추가]: 이번 프레임에 강제된 최소 X 좌표
        #endregion

        #region 프로퍼티
        public PlayerStateDTO State => m_state;
        public PlayerConfigDTO Config => m_config;

        public event Action OnDashStarted;
        public event Action OnParryStarted;
        public event Action OnAttackStarted;
        public event Action OnDamaged;
        public event Action OnDeath;
        #endregion

        #region 초기화
        public PlayerLogic(
            PlayerConfigDTO config, 
            IPlayerStatService statService, 
            IEnemyDetectionService enemyDetection,
            IEventBus eventBus)
        {
            m_config = config ?? new PlayerConfigDTO();
            m_statService = statService;
            m_enemyDetection = enemyDetection;
            m_eventBus = eventBus;
            m_state = new PlayerStateDTO();
        }
        #endregion

        #region 공개 메서드 (이동 및 업데이트)
        /// <summary>
        /// [설명]: 매 프레임 로직 상태와 위치를 업데이트합니다.
        /// </summary>
        public void Update(float time, float deltaTime)
        {
            UpdateMovement(deltaTime);
            UpdateStates(deltaTime);
        }

        private void UpdateMovement(float deltaTime)
        {
            // 1. 특수 액션 이동 (대시, 퇴각)
            if (m_state.IsDashing || m_state.IsRetreating)
            {
                float speed = m_state.IsDashing ? m_config.DashSpeed : m_config.RetreatSpeed;
                m_state.Position = Vector2.MoveTowards(m_state.Position, m_state.TargetPosition, speed * deltaTime);

                if (Vector2.Distance(m_state.Position, m_state.TargetPosition) < 0.05f)
                {
                    EndAction();
                }
            }
            // 2. 일반 밀림 이동 (EnemyPushController에 의해 예약된 속도)
            else if (m_pendingPushVelocityX != 0)
            {
                float displacement = m_pendingPushVelocityX * deltaTime;
                m_state.Position.x += displacement;
                m_pendingPushVelocityX = 0f;
            }

            // 3. 강제 위치 보정 (적 리더와의 겹침 방지)
            if (m_forcedPushX != float.MaxValue)
            {
                if (m_state.Position.x > m_forcedPushX)
                {
                    m_state.Position.x = m_forcedPushX;
                }
                m_forcedPushX = float.MaxValue; // 초기화
            }

            // 4. 왼쪽 벽 제한 적용
            m_state.Position.x = Math.Max(m_state.Position.x, m_config.LeftWallX);
            
            // 일반 상태일 때는 목표 위치를 현재 위치로 유지 (뷰 보간용)
            if (!m_state.IsDashing && !m_state.IsRetreating)
            {
                m_state.TargetPosition = m_state.Position;
            }
        }

        private void UpdateStates(float deltaTime)
        {
            if (m_pushTimer > 0)
            {
                m_pushTimer -= deltaTime;
                if (m_pushTimer <= 0) m_pushTimer = 0;
            }
            m_state.IsBeingPushed = m_pushTimer > 0;
        }

        /// <summary>
        /// [설명]: 외부에서 가해지는 밀림 속도를 예약합니다. (중첩 방지)
        /// </summary>
        public void ApplyExternalPush(Vector2 velocity)
        {
            if (Mathf.Abs(velocity.x) > Mathf.Abs(m_pendingPushVelocityX))
            {
                m_pendingPushVelocityX = velocity.x;
            }
            m_pushTimer = 0.1f;
        }

        /// <summary>
        /// [설명]: 적과의 물리적 겹침을 방지하기 위해 강제로 밀어낼 위치를 지정합니다.
        /// </summary>
        public void ForcePushPosition(float x)
        {
            if (x < m_forcedPushX) m_forcedPushX = x;
        }
        #endregion

        #region 전투 스킬 로직
        public bool TryDash(float time)
        {
            if (time - m_state.LastDashTime < m_config.DashCooldown) return false;

            GameObject frontEnemy = GetFrontEnemy();
            if (frontEnemy == null) return false;

            float frontEnemyX = frontEnemy.transform.position.x;
            float targetX = frontEnemyX - m_config.DashStopDistance;
            
            if (m_state.IsAttacking || m_state.IsParrying) EndAction();

            m_state.LastDashTime = time;
            m_state.IsDashing = true;
            m_state.TargetPosition = new Vector2(targetX, m_state.Position.y);
            OnDashStarted?.Invoke();
            return true;
        }

        public bool TryParry(float time)
        {
            if (time - m_state.LastParryTime < m_config.ParryCooldown) return false;
            if (m_state.IsParrying || m_state.IsRetreating) return false;

            GameObject frontEnemy = GetFrontEnemy();
            if (frontEnemy == null) return false;

            Vector2 referencePos = m_state.IsDashing ? m_state.TargetPosition : m_state.Position;
            float distance = Mathf.Abs(frontEnemy.transform.position.x - referencePos.x);
            
            if (distance > m_config.ParryActivationRange) return false;

            if (m_state.IsDashing || m_state.IsAttacking) EndAction();

            m_state.LastParryTime = time;
            m_state.IsParrying = true;
            OnParryStarted?.Invoke();
            return true;
        }

        public bool TryAttack(float time)
        {
            if (time - m_state.LastAttackTime < m_config.AttackCooldown) return false;
            if (m_state.IsAttacking) EndAction();
            if (m_state.IsDashing || m_state.IsParrying || m_state.IsRetreating) return false;

            m_state.LastAttackTime = time;
            m_state.IsAttacking = true;
            OnAttackStarted?.Invoke();
            return true;
        }

        public GameObject GetFrontEnemy() => m_enemyDetection.GetFrontEnemy(m_state.Position);

        public void SetPosition(Vector2 position)
        {
            m_state.Position = new Vector2(Math.Max(position.x, m_config.LeftWallX), position.y);
            m_state.TargetPosition = m_state.Position;
        }

        public void StartRetreat() => m_state.IsRetreating = true;

        public void EndAction()
        {
            m_state.IsDashing = false;
            m_state.IsParrying = false;
            m_state.IsAttacking = false;
            m_state.IsRetreating = false;
            m_state.TargetPosition = m_state.Position;
        }

        public bool IsBusy() => m_state.IsDashing || m_state.IsParrying || m_state.IsAttacking || m_state.IsRetreating;
        #endregion

        #region 체력 및 사망 로직
        public void InitializeHealth(int health)
        {
            m_state.MaxHealth = health;
            m_state.Health = health;
        }

        public void TakeDamage(int damage)
        {
            m_state.Health = Math.Max(0, m_state.Health - damage);
            OnDamaged?.Invoke();
            m_eventBus.Publish(new OnPlayerDamaged { Damage = damage, CurrentHealth = m_state.Health, MaxHealth = m_state.MaxHealth });
            if (m_state.Health <= 0) Die();
        }

        public void Die()
        {
            OnDeath?.Invoke();
            m_eventBus.Publish(new OnGameOver { IsVictory = false });
        }
        #endregion
    }
    #endregion
}
