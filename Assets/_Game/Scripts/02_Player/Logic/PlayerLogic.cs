using System;
using UnityEngine;
using TowerBreakers.Player.DTO;
using TowerBreakers.Player.Stat;
using TowerBreakers.Player.Service;
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
        private readonly IEnemyDetectionService m_enemyDetection;
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
        public PlayerLogic(PlayerConfigDTO config, IPlayerStatService statService, IEnemyDetectionService enemyDetection)
        {
            m_config = config ?? new PlayerConfigDTO();
            m_statService = statService;
            m_enemyDetection = enemyDetection;
            m_state = new PlayerStateDTO();
        }
        #endregion

        #region 공개 메서드
        public void Update(float time, float deltaTime)
        {
            // 실제 이동은 View에서 진행하지만 상태 계산에 필요한 업데이트 수행
        }

        public bool TryDash(float time)
        {
            if (time - m_state.LastDashTime < m_config.DashCooldown)
            {
                Debug.Log($"[PlayerLogic] 대시 실패 - 쿨타임 중 (남은 시간: {m_config.DashCooldown - (time - m_state.LastDashTime):F2}s)");
                return false;
            }

            if (m_state.IsDashing || m_state.IsRetreating)
            {
                Debug.Log($"[PlayerLogic] 대시 실패 - 이미 이동 중 (Dashing: {m_state.IsDashing}, Retreating: {m_state.IsRetreating})");
                return false;
            }

            // [기반 수정]: 대시 타겟 위치를 로직 내부에서 직접 계산 (Detection Service 활용)
            float frontEnemyX = GetFrontEnemyX();
            float targetX = frontEnemyX - m_config.DashStopDistance;

            if (targetX <= m_state.Position.x)
            {
                Debug.Log($"[PlayerLogic] 대시 무시 - 이미 적 앞에 도달함 (TargetX: {targetX:F2})");
                return false;
            }

            // [개선]: 공격 중이거나 패링 중일 때 대쉬 입력 시 이전 액션 캔슬 허용
            if (m_state.IsAttacking || m_state.IsParrying)
            {
                Debug.Log("[PlayerLogic] 대시 발동 - 이전 액션(공격/패링) 캔슬");
                EndAction();
            }

            m_state.LastDashTime = time;
            m_state.IsDashing = true;
            m_state.Position = new Vector2(targetX, m_state.Position.y);
            Debug.Log("[PlayerLogic] 대시 발동 성공");
            OnDashStarted?.Invoke();
            return true;
        }

        public bool TryParry(float time)
        {
            if (time - m_state.LastParryTime < m_config.ParryCooldown)
            {
                Debug.Log($"[PlayerLogic] 패링 실패 - 쿨타임 대기 중 (남은 시간: {m_config.ParryCooldown - (time - m_state.LastParryTime):F2}s)");
                return false;
            }

            if (m_state.IsParrying || m_state.IsRetreating)
            {
                Debug.Log($"[PlayerLogic] 패링 실패 - 이미 패링/퇴격 중 (Parrying: {m_state.IsParrying}, Retreating: {m_state.IsRetreating})");
                return false;
            }

            // [기반 수정]: 패링 발동 가능 거리 판정을 로직 내부에서 수행
            float distanceToFront = m_enemyDetection.GetDistanceToFrontEnemy(m_state.Position);
            if (distanceToFront > m_config.ParryActivationRange)
            {
                Debug.Log($"[PlayerLogic] 패링 발동 실패: 최전방 적과의 거리({distanceToFront:F2})가 활성화 사거리({m_config.ParryActivationRange})보다 멂");
                return false;
            }

            // [개선]: 대쉬 중이거나 공격 중일 때 패링 입력 시 즉시 캔슬하고 패링 실행
            if (m_state.IsDashing || m_state.IsAttacking)
            {
                Debug.Log("[PlayerLogic] 패링 발동 - 이전 액션(대쉬/공격) 캔슬");
                EndAction();
            }

            m_state.LastParryTime = time;
            m_state.IsParrying = true;
            Debug.Log("[PlayerLogic] 패링 발동 성공");
            OnParryStarted?.Invoke();
            return true;
        }

        public bool TryAttack(float time)
        {
            if (time - m_state.LastAttackTime < m_config.AttackCooldown)
            {
                Debug.Log($"[PlayerLogic] 공격 실패 - 쿨타임 중 (남은 시간: {m_config.AttackCooldown - (time - m_state.LastAttackTime):F2}s)");
                return false;
            }

            // [기반 수정]: 공격 시퀀스가 이미 진행 중이면 중복 실행을 막아 비동기 시퀀스 중단(Cancellation) 방지 (Atomic Attack 보장)
            if (m_state.IsAttacking) return false;

            if (m_state.IsDashing || m_state.IsParrying || m_state.IsRetreating)
            {
                Debug.Log($"[PlayerLogic] 공격 실패 - 현재 조작 불가 상태 (Dashing: {m_state.IsDashing}, Parrying: {m_state.IsParrying}, Retreating: {m_state.IsRetreating})");
                return false;
            }

            m_state.LastAttackTime = time;
            m_state.IsAttacking = true;
            Debug.Log("[PlayerLogic] 공격 발동 성공");
            OnAttackStarted?.Invoke();
            return true;
        }

        public float GetFrontEnemyX()
        {
            var frontEnemy = m_enemyDetection.GetFrontEnemy(m_state.Position);
            return frontEnemy != null ? frontEnemy.transform.position.x : m_state.Position.x;
        }

        public GameObject GetFrontEnemy()
        {
            return m_enemyDetection.GetFrontEnemy(m_state.Position);
        }

        /// <summary>
        /// [설명]: 플레이어의 현재 위치를 설정합니다. 왼쪽 벽 제한이 적용됩니다.
        /// </summary>
        /// <param name="position">새로운 위치</param>
        public void SetPosition(Vector2 position)
        {
            float clampedX = Math.Max(position.x, m_config.LeftWallX);
            m_state.Position = new Vector2(clampedX, position.y);
        }

        /// <summary>
        /// [설명]: 패링 시 퇴각 상태를 시작합니다.
        /// </summary>
        public void StartRetreat()
        {
            m_state.IsRetreating = true;
        }

        /// <summary>
        /// [설명]: 현재 진행 중인 모든 액션 상태를 초기화합니다.
        /// </summary>
        public void EndAction()
        {
            m_state.IsDashing = false;
            m_state.IsParrying = false;
            m_state.IsAttacking = false;
            m_state.IsRetreating = false;
        }

        /// <summary>
        /// [설명]: 플레이어가 바쁜 상태(액션 수행 중)인지 확인합니다.
        /// </summary>
        /// <returns>바쁨 여부</returns>
        public bool IsBusy()
        {
            return m_state.IsDashing || m_state.IsParrying || m_state.IsAttacking || m_state.IsRetreating;
        }

        /// <summary>
        /// [설명]: 외부에서 가해지는 밀림 속도(Velocity)를 계산하여 위치에 반영합니다.
        /// </summary>
        /// <param name="velocity">외부 속도 (저항력이 적용된 값)</param>
        public void ApplyExternalPush(Vector2 velocity)
        {
            // [참고]: velocity에는 이미 View/Receiver 레벨에서 저항력이 곱해져서 전달됨
            Vector2 currentPos = m_state.Position;
            float newX = currentPos.x + velocity.x * Time.deltaTime;
            
            // 왼쪽 벽 제한 및 위치 갱신
            float clampedX = Math.Max(newX, m_config.LeftWallX);
            m_state.Position = new Vector2(clampedX, currentPos.y);
        }

        /// <summary>
        /// [설명]: 플레이어의 초기 체력을 설정합니다.
        /// </summary>
        /// <param name="health">최대 체력</param>
        public void InitializeHealth(int health)
        {
            m_state.MaxHealth = health;
            m_state.Health = health;
        }

        /// <summary>
        /// [설명]: 데미지를 입고 체력을 갱신합니다.
        /// </summary>
        /// <param name="damage">입힐 데미지량</param>
        public void TakeDamage(int damage)
        {
            m_state.Health = Math.Max(0, m_state.Health - damage);
            OnDamaged?.Invoke();
            
            if (m_state.Health <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// [설명]: 플레이어 사망 처리를 수행합니다.
        /// </summary>
        public void Die()
        {
            OnDeath?.Invoke();
        }
        #endregion
    }
    #endregion
}
