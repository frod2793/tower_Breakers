using UnityEngine;
using TowerBreakers.Core.Events;
using TowerBreakers.Player.DTO;
using TowerBreakers.Player.Stat;
using TowerBreakers.Core.Service;
using TowerBreakers.Enemy.Service;
using System;
using TowerBreakers.Player.Data;

namespace TowerBreakers.Player.Logic
{
    #region 비즈니스 로직 (Logic)
    /// <summary>
    /// [설명]: 플레이어의 핵심 게임 로직(이동, 전투 판정, 상태 관리)을 담당하는 순수 C# 클래스입니다.
    /// MonoBehaviour를 상속받지 않으며, 외부에서 주입받은 데이터를 기반으로 연산을 수행합니다.
    /// </summary>
    public class PlayerLogic
    {
        #region 내부 필드
        private readonly PlayerConfigDTO m_config;
        private readonly PlayerStateDTO m_state;
        private readonly IEventBus m_eventBus;
        private readonly IPlayerStatService m_statService;

        public PlayerConfigDTO Config => m_config;
        public PlayerStateDTO State => m_state;
        #endregion

        #region 이벤트
        public event Action OnDashStarted;
        public event Action OnParryStarted;
        public event Action OnAttackStarted;
        public event Action OnWindstormSlashStarted;
        public event Action OnDamaged; // [추가]: 피격 이벤트
        public event Action OnDeath; // [추가]: 사망 이벤트
        #endregion

        #region 초기화 및 바인딩 로직
        public PlayerLogic(PlayerConfigDTO config, PlayerStateDTO state, IEventBus eventBus, IPlayerStatService m_statService)
        {
            m_config = config;
            m_state = state;
            m_eventBus = eventBus;
            this.m_statService = m_statService;
        }

        public void SetPosition(Vector2 position)
        {
            m_state.Position = position;
        }
        #endregion

        #region 비즈니스 로직
        public void Update(float time, float deltaTime)
        {
            UpdateMovement(time, deltaTime);
            UpdateAttack(time);
        }

        private void UpdateMovement(float time, float deltaTime)
        {
            // 1. 특수 액션 이동 (대시, 퇴각)
            if ((m_state.IsDashing || m_state.IsRetreating) && !m_state.IsCharging)
            {
                // [수정]: 속도 우선순위 정립 (패링 후퇴 > 위드폼/대시)
                float speed = m_config.DashSpeed;
                
                if (m_state.IsParrying) speed = m_config.ParryRetreatSpeed;
                else if (m_state.IsRetreating) speed = m_config.SkillRetreatSpeed;
                else if (m_state.IsWindstormDash) speed = m_config.WindstormDashSpeed;

                m_state.Position = Vector2.MoveTowards(m_state.Position, m_state.TargetPosition, speed * deltaTime);

                // [수정]: 백덤블링 중 Y축 곡선 이동 (Mathf.Sin 활용)
                // [조건]: 아직 목표 지점에 도달하지 않았을 때 활성화 (벽까지 유지)
                float distToTarget = Vector2.Distance(m_state.Position, m_state.TargetPosition);
                if (m_state.IsBackflip && distToTarget > m_config.MovementArrivalThreshold)
                {
                    float totalDist = Vector2.Distance(m_state.ParryStartPosition, m_state.TargetPosition);
                    if (totalDist > 0.1f)
                    {
                        float currentDist = Vector2.Distance(m_state.ParryStartPosition, m_state.Position);
                        float progress = Mathf.Clamp01(currentDist / totalDist);
                        float height = Mathf.Sin(progress * Mathf.PI) * m_config.ParryJumpHeight;
                        m_state.Position.y = m_state.ParryStartPosition.y + height;
                    }
                }
                else if (m_state.IsBackflip)
                {
                    // 목표 지점 도달 시 Y축 위치를 원래대로 복구 (착지)
                    m_state.Position.y = m_state.ParryStartPosition.y;
                }

                if (Vector2.Distance(m_state.Position, m_state.TargetPosition) < m_config.MovementArrivalThreshold)
                {
                    // [핵심 복구]: 일반 대시나 퇴각은 여기서 즉시 종료 처리
                    if (!m_state.IsWindstormDash)
                    {
                        EndAction();
                    }
                }
            }
        }

        private void UpdateAttack(float time)
        {
            // 필요한 공격 상태 업데이트 로직 (현재는 트리거 방식)
        }

        public bool TryDash(float time)
        {
            if (time - m_state.LastDashTime < m_config.DashCooldown) return false;
            if (IsBusy()) return false;

            GameObject nearestEnemy = GetFrontEnemy();
            if (nearestEnemy == null) return false;

            float distance = Vector2.Distance(m_state.Position, nearestEnemy.transform.position);
            if (distance < m_config.DashMinDistance) return false;

            m_state.LastDashTime = time;
            m_state.IsDashing = true;
            m_state.TargetPosition = new Vector2(nearestEnemy.transform.position.x - m_config.DashStopDistance, m_state.Position.y);
            
            OnDashStarted?.Invoke();
            return true;
        }

        public bool TryParry(float time, float referenceX)
        {
            if (time - m_state.LastParryTime < m_config.ParryCooldown) return false;
            if (IsBusy()) return false;

            m_state.LastParryTime = time;
            m_state.IsParrying = true;
            m_state.IsBackflip = m_state.Position.x > referenceX; // [수정]: 시작 지점이 기준보다 오른쪽일 때만 백덤블링 활성화
            m_state.ParryStartPosition = m_state.Position;
            m_state.ParryReferenceX = referenceX; // [수정]: 백덤블링 연출 기준 좌표 저장
            
            // [수정]: 무조건 왼쪽 벽(LeftWallX)까지만 후퇴하도록 명시적 고정
            m_state.IsRetreating = true;
            m_state.TargetPosition = new Vector2(m_config.LeftWallX, m_state.Position.y);

            // [추가]: 패링 성공 즉시 시스템 이벤트 발행 (1초 무적 부여용)
            m_eventBus.Publish(new OnParryPerformed());

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

        public GameObject GetFrontEnemy()
        {
            var enemies = GameObject.FindGameObjectsWithTag("Enemy");
            if (enemies == null || enemies.Length == 0) return null;

            GameObject nearest = null;
            float minDistance = float.MaxValue;

            foreach (var enemy in enemies)
            {
                // [수정]: 동일 층(Y축 범위 내)의 적만 타겟팅하도록 명시적 필터링 추가
                if (Mathf.Abs(enemy.transform.position.y - m_state.Position.y) > m_config.EnemyDetectionYRange) continue;

                if (enemy.transform.position.x > m_state.Position.x)
                {
                    float dist = enemy.transform.position.x - m_state.Position.x;
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nearest = enemy;
                    }
                }
            }

            return nearest;
        }

        public void EndAction()
        {
            if (m_state.IsBackflip)
            {
                m_state.Position.y = m_state.ParryStartPosition.y;
            }

            m_state.IsDashing = false;
            m_state.IsWindstormDash = false;
            m_state.IsCharging = false;
            m_state.IsParrying = false;
            m_state.IsAttacking = false;
            m_state.IsRetreating = false;
            m_state.IsBackflip = false;
            m_state.TargetPosition = m_state.Position;
        }

        public bool IsBusy() => m_state.IsDashing || m_state.IsParrying || m_state.IsAttacking || m_state.IsRetreating;

        public bool TryWindstormSlash(float time)
        {
            if (time - m_state.LastAttackTime < m_config.WindstormCooldown) 
            {
                Debug.Log($"[PlayerLogic] 스킬 발동 실패: 쿨타임 중 (경과: {time - m_state.LastAttackTime:F2}s)");
                return false;
            }
            if (IsBusy()) 
            {
                Debug.Log($"[PlayerLogic] 스킬 발동 실패: 플레이어가 다른 동작 중 (Busy: {IsBusy()})");
                return false;
            }

            var currentWeapon = m_statService.GetEquippedWeapon();
            if (currentWeapon == null || currentWeapon.WeaponType != WeaponType.Sword)
            {
                Debug.Log($"[PlayerLogic] 스킬 발동 실패: 유효한 검을 장비하지 않음");
                return false;
            }

            GameObject frontEnemy = GetFrontEnemy();
            if (frontEnemy == null) 
            {
                Debug.Log("[PlayerLogic] 스킬 발동 실패: 전방에 적이 없음 (GetFrontEnemy null)");
                return false;
            }

            // [규칙]: 적의 앞까지 돌진 (DashStopDistance 활용)
            float targetX = frontEnemy.transform.position.x - m_config.DashStopDistance;
            
            m_state.LastAttackTime = time;
            m_state.IsDashing = true;
            m_state.IsWindstormDash = true;
            m_state.IsCharging = true;
            m_state.TargetPosition = new Vector2(targetX, m_state.Position.y);

            OnWindstormSlashStarted?.Invoke();
            return true;
        }

        /// <summary>
        /// [설명]: 애니메이션 타격 시점에 호출되어 실제 피해를 입힙니다.
        /// 앞에서부터 최대 3명까지 피해를 입힙니다.
        /// </summary>
        public void ApplyWindstormDamage(float damageValue)
        {
            GameObject frontEnemy = GetFrontEnemy();
            if (frontEnemy == null) return;

            var leaderPush = frontEnemy.GetComponent<EnemyPushController>();
            if (leaderPush != null)
            {
                int hitCount = 0;
                var current = leaderPush;

                while (current != null && hitCount < m_config.WindstormMaxTargets)
                {
                    var controller = current.GetComponent<IEnemyController>();
                    if (controller != null)
                    {
                        controller.TakeDamage(damageValue);
                        hitCount++;
                    }
                    current = current.FollowerEnemy;
                }
            }
            else
            {
                var controller = frontEnemy.GetComponent<IEnemyController>();
                controller?.TakeDamage(damageValue);
            }
        }

        public void StartWindstormDash()
        {
            m_state.IsCharging = false;
        }

        public void ApplyExternalPush(Vector2 force)
        {
            m_state.Position += force * Time.deltaTime;
        }

        public void ForcePushPosition(float x)
        {
            if (m_state.Position.x > x)
            {
                m_state.Position.x = x;
            }
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
