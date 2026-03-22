using System;
using UnityEngine;
using TowerBreakers.Player.DTO;
using TowerBreakers.Player.Stat;
using TowerBreakers.Player.Service;
using Cysharp.Threading.Tasks;
using TowerBreakers.Enemy.Service;
using TowerBreakers.Core.Events;
using TowerBreakers.Player.Data;

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
        public event Action OnWindstormSlashStarted;
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

                // [추가]: 백덤블링 중 Y축 곡선 이동 (Mathf.Sin 활용)
                if (m_state.IsRetreating && m_state.IsBackflip)
                {
                    float totalDistX = Mathf.Abs(m_state.TargetPosition.x - m_state.ParryStartPosition.x);
                    if (totalDistX > 0.1f)
                    {
                        float currentDistX = Mathf.Abs(m_state.Position.x - m_state.ParryStartPosition.x);
                        float progress = Mathf.Clamp01(currentDistX / totalDistX);
                        // Sin 곡선으로 점프 높이 계산 (0 -> 1 -> 0)
                        m_state.Position.y = m_state.ParryStartPosition.y + Mathf.Sin(progress * Mathf.PI) * m_config.ParryJumpHeight;
                    }
                }

                if (Vector2.Distance(m_state.Position, m_state.TargetPosition) < 0.05f)
                {
                    Debug.Log($"[PlayerLogic] 퇴각/대시 종료: Pos={m_state.Position}, Target={m_state.TargetPosition}");
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

            // 3. [개선]: 강제 위치 보정 (적 리더와의 겹침 방지 및 밀림 누락 해결)
            // 간헐적인 밀림 누락은 물리 프레임 오차로 발생하므로, 강제 위치(forcedPushX)가 속도 기반 이동보다 우선함
            if (m_forcedPushX != float.MaxValue)
            {
                // 적이 밀고 들어오는 경우, 플레이어는 무조건 forcedPushX 이하의 좌표에 있어야 함
                if (m_state.Position.x > m_forcedPushX)
                {
                    m_state.Position.x = m_forcedPushX;
                }
                
                // [추가]: 밀리고 있을 때는 목표 좌표도 동기화하여 뷰 떨림 방지
                m_state.TargetPosition.x = m_state.Position.x;
                m_forcedPushX = float.MaxValue; // 초기화
            }

            // 4. 왼쪽 벽 제한 적용
            if (m_state.Position.x < m_config.LeftWallX)
            {
                m_state.Position.x = m_config.LeftWallX;
            }
            
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

        /// <summary>
        /// [설명]: 패링을 시도합니다. 성공 시 퇴각하며, 기준점보다 오른쪽에 있을 경우 백덤블링을 수행합니다.
        /// </summary>
        /// <param name="time">현재 시간</param>
        /// <param name="referenceX">백덤블링 판정 기준점 X 좌표</param>
        public bool TryParry(float time, float referenceX)
        {
            if (time - m_state.LastParryTime < m_config.ParryCooldown) 
            {
                Debug.Log($"[PlayerLogic] 패링 실패: 쿨타임 중 ({time - m_state.LastParryTime:F2}/{m_config.ParryCooldown})");
                return false;
            }
            if (m_state.IsParrying || m_state.IsRetreating)
            {
                Debug.Log($"[PlayerLogic] 패링 실패: 이미 패링/퇴각 중 (IsParrying={m_state.IsParrying}, IsRetreating={m_state.IsRetreating})");
                return false;
            }

            GameObject frontEnemy = GetFrontEnemy();
            if (frontEnemy == null)
            {
                Debug.Log("[PlayerLogic] 패링 실패: 전방에 적이 없음");
                return false;
            }

            Vector2 referencePos = m_state.IsDashing ? m_state.TargetPosition : m_state.Position;
            float distance = Mathf.Abs(frontEnemy.transform.position.x - referencePos.x);
            
            if (distance > m_config.ParryActivationRange)
            {
                Debug.Log($"[PlayerLogic] 패링 실패: 거리 초과 (distance={distance:F2}, Range={m_config.ParryActivationRange})");
                return false;
            }

            if (m_state.IsDashing || m_state.IsAttacking) EndAction();

            m_state.LastParryTime = time;
            m_state.IsParrying = true;
            
            // [추가]: 패링 후 퇴각 및 백덤블링 설정
            m_state.ParryStartPosition = m_state.Position;
            m_state.IsBackflip = m_state.Position.x > referenceX;
            m_state.TargetPosition = new Vector2(m_config.LeftWallX, m_state.Position.y);
            m_state.IsRetreating = true;
            
            Debug.Log($"[PlayerLogic] 패링 성공: Pos={m_state.Position}, Target={m_state.TargetPosition}, LeftWallX={m_config.LeftWallX}, IsBackflip={m_state.IsBackflip}");
            
            // [추가]: 패링 수행 이벤트 발행 (CombatSystem에서 압착 피해 리셋용으로 사용)
            m_eventBus.Publish(new OnParryPerformed());
            
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
            m_state.IsBackflip = false; // [추가]: 백덤블링 상태 해제
            m_state.TargetPosition = m_state.Position;
        }

        public bool IsBusy() => m_state.IsDashing || m_state.IsParrying || m_state.IsAttacking || m_state.IsRetreating;

        /// <summary>
        /// [설명]: 질풍참 스킬을 시도합니다. 적 대열의 리더 앞으로 대시하며 다수의 적을 공격합니다.
        /// </summary>
        public bool TryWindstormSlash(float time)
        {
            if (time - m_state.LastAttackTime < m_config.WindstormCooldown) return false;
            if (IsBusy()) return false;

            GameObject frontEnemy = GetFrontEnemy();
            if (frontEnemy == null) return false;

            float targetX = frontEnemy.transform.position.x - m_config.DashStopDistance;
            
            m_state.LastAttackTime = time;
            m_state.IsDashing = true;
            m_state.TargetPosition = new Vector2(targetX, m_state.Position.y);

            // [체크]: 현재 장착된 무기가 '검'인지 확인
            var currentWeapon = m_statService is PlayerStatService service ? service.GetEquippedWeapon() : null;
            if (currentWeapon == null || currentWeapon.WeaponType != WeaponType.Sword) return false;

            var leaderPush = frontEnemy.GetComponent<EnemyPushController>();
            float finalDamage = m_statService.TotalAttack * m_config.WindstormDamageMultiplier;

            if (leaderPush != null)
            {
                int hitCount = 0;
                var current = leaderPush;

                while (current != null && hitCount < m_config.WindstormMaxTargets)
                {
                    var controller = current.GetComponent<IEnemyController>();
                    if (controller != null)
                    {
                        controller.TakeDamage(finalDamage);
                        hitCount++;
                    }
                    current = current.FollowerEnemy;
                }
            }
            else
            {
                // 리더(군집 제어)가 없는 대상(보상 상자 등)은 단일 타격 처리
                var controller = frontEnemy.GetComponent<IEnemyController>();
                controller?.TakeDamage(finalDamage);
            }

            OnWindstormSlashStarted?.Invoke();
            return true;
        }
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
