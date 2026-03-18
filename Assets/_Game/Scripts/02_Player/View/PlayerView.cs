using System;
using UnityEngine;
using TowerBreakers.Player.DTO;
using TowerBreakers.Player.Logic;
using TowerBreakers.Player.Stat;
using TowerBreakers.UI;
using TowerBreakers.Tower.Service;
using Cysharp.Threading.Tasks;
using TowerBreakers.UI.ViewModel;

namespace TowerBreakers.Player.View
{
    /// <summary>
    /// [설명]: 플레이어의 시각적 요소(애니메이션, 위치 갱신)를 담당하는 뷰 클래스입니다.
    /// POCO 클래스인 PlayerLogic과 협력합니다.
    /// </summary>
    public class PlayerView : MonoBehaviour
    {
        #region 에디터 설정
        [Header("캐릭터 참조")]
        [SerializeField, Tooltip("SPUM 캐릭터 본체 참조")]
        private SPUM_Prefabs m_spumPrefabs;
        #endregion

        #region 내부 필드
        private PlayerConfigDTO m_config;
        private PlayerLogic m_logic;
        private BattleUIViewModel m_uiViewModel;
        private IPlayerStatService m_statService;
        private PlayerState m_currentAnimState = PlayerState.IDLE;
        private Vector3 m_lastPos;
        #endregion

        #region 초기화 및 바인딩 로직
        /// <summary>
        /// [설명]: 플레이어 뷰를 초기화하고 필요한 의존성을 주입받습니다.
        /// </summary>
        public void Initialize(PlayerLogic logic, BattleUIViewModel uiViewModel, IPlayerStatService statService)
        {
            if (logic == null)
            {
                Debug.LogError("[PlayerView] Logic is null!");
                return;
            }

            m_logic = logic;
            m_config = logic.Config;
            m_uiViewModel = uiViewModel;
            m_statService = statService;

            if (m_spumPrefabs != null)
            {
                m_spumPrefabs.OverrideControllerInit();
            }

            // 로직 이벤트 바인딩
            m_logic.OnDashStarted += () => PlayAnimation(PlayerState.ATTACK);
            m_logic.OnParryStarted += StartParrySequence;
            m_logic.OnAttackStarted += StartAttackSequence;
            m_logic.OnHit += (target) => PlayAnimation(PlayerState.ATTACK);
            m_logic.OnDamaged += OnPlayerDamaged;
            m_logic.OnDeath += OnPlayerDeath;

            // UI 이벤트 바인딩
            if (m_uiViewModel != null)
            {
                m_uiViewModel.OnSkillTriggered += HandleUISkill;
            }

            m_lastPos = transform.position;
            m_logic.SetPosition(transform.position);
        }

        private void HandleUISkill(string skillName)
        {
            Debug.Log($"[PlayerView] UI 스킬 입력 수신: {skillName}");
            if (skillName == "Dash")
            {
                float enemyFrontX = GetEnemyFrontX();
                float distanceToEnemy = enemyFrontX - transform.position.x;

                if (distanceToEnemy > m_config.DashMinDistance)
                {
                    float targetX = enemyFrontX - 1.5f;
                    if (!m_logic.TryDash(Time.time, targetX))
                    {
                        Debug.Log("[PlayerView] 대시 발동 실패 (로직 내부 거부)");
                    }
                }
                else
                {
                    Debug.Log($"[PlayerView] 대시 무시 - 적까지 거리({distanceToEnemy})가 최소 거리({m_config.DashMinDistance}) 이하시");
                }
            }
            else if (skillName == "Parry")
            {
                // [개선]: 기획 요구사항에 따라 설정된 거리(ParryActivationRange) 내에 적이 있을 때만 패링 발동
                if (IsAnyEnemyInRange(m_config.ParryActivationRange))
                {
                    m_logic.TryParry(Time.time);
                }
                else
                {
                    Debug.Log($"[PlayerView] 패링 발동 실패: 활성화 거리({m_config.ParryActivationRange}) 내에 적이 없음");
                }
            }
            else if (skillName == "Attack") m_logic.TryAttack(Time.time);
        }

        private float GetEnemyFrontX()
        {
            var enemies = FindObjectsOfType<EnemyPushController>();
            if (enemies == null || enemies.Length == 0) return transform.position.x + 5f;

            float minX = float.MaxValue;
            foreach (var enemy in enemies)
            {
                if (enemy != null && enemy.transform.position.x < minX) minX = enemy.transform.position.x;
            }
            return minX;
        }
        #endregion

        #region 유니티 생명주기
        private void Update()
        {
            if (m_logic == null) return;

            m_logic.Update(Time.time, Time.deltaTime);
            UpdateFacingDirection();
            UpdateVisualMovement();
        }

        private void UpdateFacingDirection()
        {
            float enemyFrontX = GetEnemyFrontX();
            float direction = enemyFrontX - transform.position.x;

            if (Mathf.Abs(direction) > 0.01f)
            {
                float scaleX = Mathf.Abs(transform.localScale.x) * (direction > 0 ? 1 : -1);
                transform.localScale = new Vector3(scaleX, transform.localScale.y, transform.localScale.z);
            }
        }

        private void UpdateVisualMovement()
        {
            var state = m_logic.State;
            
            if (state.IsDashing || state.IsRetreating)
            {
                transform.position = Vector2.MoveTowards(
                    transform.position,
                    state.Position,
                    (state.IsDashing ? m_config.DashSpeed : m_config.RetreatSpeed) * Time.deltaTime
                );

                if (Vector2.Distance(transform.position, state.Position) < 0.1f)
                {
                    m_logic.EndAction();
                    m_currentAnimState = PlayerState.IDLE; // 상태 강제 리셋
                    PlayAnimation(PlayerState.IDLE);
                }
            }
            else
            {
                float targetX = Mathf.Max(state.Position.x, m_config.LeftWallX);
                Vector2 targetPos = new Vector2(targetX, state.Position.y);

                transform.position = Vector2.Lerp(
                    transform.position,
                    targetPos,
                    Time.deltaTime * 5f
                );

                // 실제 이동 거리를 체크하여 MOVE/IDLE 전환
                float dist = Vector2.Distance(transform.position, m_lastPos);
                m_lastPos = transform.position;

                if (dist > 0.01f)
                {
                    PlayAnimation(PlayerState.MOVE);
                }
                else
                {
                    PlayAnimation(PlayerState.IDLE);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (m_config == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(
                new Vector3(m_config.LeftWallX, transform.position.y - 10f, 0),
                new Vector3(m_config.LeftWallX, transform.position.y + 10f, 0)
            );
        }
        #endregion

        private bool IsAnyEnemyInRange(float range)
        {
            var enemies = FindObjectsOfType<EnemyPushController>();
            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                
                float dist = Vector2.Distance(transform.position, enemy.transform.position);
                if (dist <= range) return true;
            }
            return false;
        }

        #region 내부 시퀀스
        private async void StartParrySequence()
        {
            PlayAnimation(PlayerState.ATTACK);

            PushAndStunEnemies(m_config.ParryRange, m_config.EnemyStopDuration, m_config.ParryPushForce);

            m_logic.SetPosition(new Vector2(m_config.LeftWallX + 1f, transform.position.y));
            m_logic.StartRetreat();

            // [개선]: 패링 시간 이후에 반드시 EndAction을 호출하여 'IsBusy' 상태가 무한 루프 도는 것을 방지
            await UniTask.Delay((int)(m_config.ParryDuration * 1000));
            m_logic.EndAction();
            PlayAnimation(PlayerState.IDLE);
        }

        private void PushAndStunEnemies(float range, float duration, float pushForce)
        {
            // [개선]: 사거리 기반이 아닌, 씬 내의 모든 활성화된 적 군집을 대상으로 효과 적용 (요구사항 반영)
            var enemies = FindObjectsOfType<EnemyPushController>();
            Debug.Log($"[PlayerView] 패링 발동 - 군집 전체({enemies.Length}명) 대상 효과 적용");

            foreach (var enemyPush in enemies)
            {
                if (enemyPush != null)
                {
                    // 1. 경직(스턴) 적용 및 중첩
                    enemyPush.Stun(duration);

                    // 2. 넉백(밀기) 적용 및 중첩
                    enemyPush.ApplyKnockback(Vector2.right, pushForce);
                }
            }
        }

        private async void StartAttackSequence()
        {
            PlayAnimation(PlayerState.ATTACK);
            await UniTask.Delay(300);

            // 공격 판정 로직 (View에서 OverlapCircle을 통해 수행 후 로직에 알림)
            GameObject nearestEnemy = FindNearestEnemy();
            if (nearestEnemy != null)
            {
                var enemyController = nearestEnemy.GetComponent<IEnemyController>();
                if (enemyController != null && m_statService != null)
                {
                    enemyController.TakeDamage(m_statService.TotalAttack);
                }
            }

            await UniTask.Delay(200);
            m_logic.EndAction();
            PlayAnimation(PlayerState.IDLE);
        }

        private GameObject FindNearestEnemy()
        {
            var colliders = Physics2D.OverlapCircleAll(transform.position, m_config.AttackRange);
            GameObject nearestEnemy = null;
            float nearestDistance = float.MaxValue;

            foreach (var collider in colliders)
            {
                if (collider.GetComponent<IEnemyController>() != null)
                {
                    float distance = Vector2.Distance(transform.position, collider.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestEnemy = collider.gameObject;
                    }
                }
            }
            return nearestEnemy;
        }

        private void PlayAnimation(PlayerState state)
        {
            if (m_currentAnimState == state && (state == PlayerState.IDLE || state == PlayerState.MOVE)) return;
            m_currentAnimState = state;

            if (m_spumPrefabs != null)
            {
                m_spumPrefabs.PlayAnimation(state, 0);
            }
        }

        private void OnPlayerDamaged()
        {
            Debug.Log("[PlayerView] 피해 입음 - DAMAGED 애니메이션 재생");
            PlayAnimation(PlayerState.DAMAGED);
            if (m_spumPrefabs != null && m_spumPrefabs._anim != null)
            {
                m_spumPrefabs._anim.SetTrigger("Damaged");
            }
        }

        private void OnPlayerDeath()
        {
            Debug.Log("[PlayerView] 사망 - DEATH 애니메이션 재생");
            PlayAnimation(PlayerState.DEATH);
            if (m_spumPrefabs != null && m_spumPrefabs._anim != null)
            {
                m_spumPrefabs._anim.SetBool("isDeath", true);
            }
            enabled = false;
        }
        #endregion
    }
}
