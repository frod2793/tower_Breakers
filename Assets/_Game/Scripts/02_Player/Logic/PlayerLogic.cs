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
            if (time - m_state.LastDashTime < m_config.DashCooldown)
            {
                Debug.Log($"[PlayerLogic] 대시 실패 - 쿨타임 중 (남은 시간: {m_config.DashCooldown - (time - m_state.LastDashTime):F2}s)");
                return false;
            }

            if (IsBusy())
            {
                Debug.Log($"[PlayerLogic] 대시 실패 - 현재 바쁜 상태 (Dashing: {m_state.IsDashing}, Parrying: {m_state.IsParrying}, Attacking: {m_state.IsAttacking}, Retreating: {m_state.IsRetreating})");
                return false;
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

            if (IsBusy())
            {
                Debug.Log($"[PlayerLogic] 패링 실패 - 현재 바쁜 상태 (Dashing: {m_state.IsDashing}, Parrying: {m_state.IsParrying}, Attacking: {m_state.IsAttacking}, Retreating: {m_state.IsRetreating})");
                return false;
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

            // [개선]: 핵앤슬래쉬 장르 특성상 공격 중이더라도 다른 행동(대쉬, 패링 등)이 아니라면 연타 가능하도록 허용
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
        /// [설명]: 외부에서 가해지는 밀림 힘을 계산하여 위치에 반영합니다.
        /// </summary>
        /// <param name="force">외부 힘 (저항력이 적용된 값)</param>
        public void ApplyExternalPush(Vector2 force)
        {
            // [참고]: force에는 이미 View/Receiver 레벨에서 저항력이 곱해져서 전달됨
            Vector2 currentPos = m_state.Position;
            float newX = currentPos.x + force.x * Time.deltaTime;
            
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
