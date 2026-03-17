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
        [Header("설정")]
        [SerializeField] private PlayerConfigDTO m_config;

        [Header("참조")]
        [SerializeField] private SPUM_Prefabs m_spumPrefabs;
        #endregion

        #region 내부 필드
        private PlayerLogic m_logic;
        private BattleUIViewModel m_uiViewModel;
        private IPlayerStatService m_statService;
        private Vector2 m_targetPosition;
        #endregion

        #region 초기화 및 바인딩 로직
        public void Initialize(PlayerLogic logic, BattleUIViewModel uiViewModel, IPlayerStatService statService)
        {
            m_logic = logic;
            m_uiViewModel = uiViewModel;
            m_statService = statService;

            // 로직 이벤트 바인딩
            m_logic.OnDashStarted += () => PlayAnimation(PlayerState.ATTACK);
            m_logic.OnParryStarted += StartParrySequence;
            m_logic.OnAttackStarted += StartAttackSequence;
            m_logic.OnDamaged += OnPlayerDamaged;
            m_logic.OnDeath += OnPlayerDeath;

            // UI 이벤트 바인딩
            if (m_uiViewModel != null)
            {
                m_uiViewModel.OnSkillTriggered += HandleUISkill;
            }

            m_logic.SetPosition(transform.position);
        }

        private void HandleUISkill(string skillName)
        {
            if (skillName == "Dash")
            {
                float targetX = GetEnemyFrontX() - 1.5f;
                m_logic.TryDash(Time.time, targetX);
            }
            else if (skillName == "Parry") m_logic.TryParry(Time.time);
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
            UpdateVisualMovement();
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

                // 방향 전환 (이동 방향에 따라)
                float direction = state.Position.x - transform.position.x;
                if (Mathf.Abs(direction) > 0.01f)
                {
                    float scaleX = Mathf.Abs(transform.localScale.x) * (direction > 0 ? 1 : -1);
                    transform.localScale = new Vector3(scaleX, transform.localScale.y, transform.localScale.z);
                }

                if (Vector2.Distance(transform.position, state.Position) < 0.1f)
                {
                    m_logic.EndAction();
                    PlayAnimation(PlayerState.IDLE);
                }
            }
            else
            {
                float targetX = state.Position.x;
                float currentY = transform.position.y; // 뷰의 현재 Y 유지 또는 state.Position.y 사용

                transform.position = Vector2.Lerp(
                    transform.position,
                    new Vector2(targetX, state.Position.y),
                    Time.deltaTime * 5f
                );
            }
        }
        #endregion

        #region 내부 시퀀스
        private async void StartParrySequence()
        {
            PlayAnimation(PlayerState.ATTACK);
            
            // 후퇴 목표 설정
            m_logic.SetPosition(new Vector2(m_config.LeftWallX + 1f, transform.position.y));
            m_logic.StartRetreat();

            await UniTask.Delay((int)(m_config.ParryDuration * 1000));
            // 추가적인 적군 정지 로직 등은 서비스 레벨에서 처리 권장
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
            if (m_spumPrefabs != null)
            {
                m_spumPrefabs.PlayAnimation(state, 0);
                if (state == PlayerState.ATTACK && m_spumPrefabs._anim != null)
                    m_spumPrefabs._anim.SetTrigger("Attack");
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
