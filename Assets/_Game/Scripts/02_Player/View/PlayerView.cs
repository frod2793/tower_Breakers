using System;
using System.Threading;
using UnityEngine;
using TowerBreakers.Player.DTO;
using TowerBreakers.Player.Logic;
using TowerBreakers.Player.Stat;
using TowerBreakers.UI;
using TowerBreakers.Tower.Service;
using Cysharp.Threading.Tasks;
using TowerBreakers.Battle;
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

        /// <summary>
        /// [설명]: 연타 시 이전 공격 시퀀스를 즉시 중단시키기 위한 캔슬 토큰 소스입니다.
        /// </summary>
        private CancellationTokenSource m_attackCts;
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
                if (!m_logic.TryDash(Time.time))
                {
                    Debug.Log("[PlayerView] 대시 발동 실패 (로직 내부 거부)");
                }
            }
            else if (skillName == "Parry")
            {
                if (!m_logic.TryParry(Time.time))
                {
                    Debug.Log("[PlayerView] 패링 발동 실패 (로직 내부 거부)");
                }
            }
            else if (skillName == "Attack")
            {
                m_logic.TryAttack(Time.time);
            }
        }

        private GameObject GetFrontEnemy()
        {
            // [기반 수정]: View에서 직접 검색하는 대신 로직에서 제공하는 최전방 적 정보를 사용
            return m_logic.GetFrontEnemy();
        }

        private float GetEnemyFrontX()
        {
            // [기반 수정]: View에서 직접 계산하는 대신 로직에서 계산된 위치를 사용
            return m_logic.GetFrontEnemyX();
        }
        #endregion

        #region 유니티 생명주기
        private void Update()
        {
            if (m_logic == null) return;

            m_logic.Update(Time.time, Time.deltaTime);
            // [수정]: 플레이어는 항상 오른쪽만 바라보도록 고정 (UpdateFacingDirection 제거)
            UpdateVisualMovement();
        }

        // [삭제]: 사용자의 요청에 따라 더 이상 방향 전환을 하지 않음
        // private void UpdateFacingDirection() { ... }

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



        #region 내부 시퀀스
        private async void StartParrySequence()
        {
            // [개선]: 액션 캔슬 시 시각적으로 현재 위치를 로직에 동기화하여 대쉬 이동을 즉시 멈춤
            m_logic.SetPosition(transform.position);
            
            PlayAnimation(PlayerState.ATTACK);

            PushAndStunEnemies(m_config.ParryRange, m_config.EnemyStopDuration, m_config.ParryPushForce);

            // [개선]: 패링 시간 이후에 반드시 EndAction을 호출하여 'IsBusy' 상태가 무한 루프 도는 것을 방지
            await UniTask.Delay((int)(m_config.ParryDuration * 1000));
            m_logic.EndAction();
            PlayAnimation(PlayerState.IDLE);
        }

        private void PushAndStunEnemies(float range, float duration, float pushForce)
        {
            // [수정]: 태그로 모든 적을 찾은 후, 현재 플레이어와 동일한 층(Y값 유사)에 있는 적만 필터링
            var allEnemies = GameObject.FindGameObjectsWithTag("Enemy");
            var currentFloorEnemies = new System.Collections.Generic.List<GameObject>();
            
            float playerY = transform.position.y;
            foreach (var enemy in allEnemies)
            {
                if (enemy == null) continue;
                // 층간 간격이 10이므로 오차 범위를 2 정도로 설정하여 안전하게 필터링
                if (Mathf.Abs(enemy.transform.position.y - playerY) < 2.0f)
                {
                    currentFloorEnemies.Add(enemy);
                }
            }

            Debug.Log($"[PlayerView] 패링 발동 - 현재 층 적({currentFloorEnemies.Count}명) 대상 효과 적용 (전체 검색: {allEnemies.Length}명)");

            foreach (var go in currentFloorEnemies)
            {
                // EnemyPushController가 있는 경우 스턴 및 넉백 적용
                var enemyPush = go.GetComponent<EnemyPushController>();
                if (enemyPush != null)
                {
                    enemyPush.Stun(duration);
                    enemyPush.ApplyKnockback(Vector2.right, pushForce);
                }
                
                // 보상 상자가 타겟인 경우
                var chest = go.GetComponent<RewardChestView>();
                if (chest != null)
                {
                    chest.TakeDamage(1f);
                }
            }
        }

        private async void StartAttackSequence()
        {
            // [개선]: 이전 공격 시퀀스가 진행 중이라면 취소하여 즉시 다음 연타/모션 갱신 허용
            if (m_attackCts != null)
            {
                m_attackCts.Cancel();
                m_attackCts.Dispose();
            }
            m_attackCts = new CancellationTokenSource();
            var token = m_attackCts.Token;

            try
            {
                PlayAnimation(PlayerState.ATTACK);

                // [개선]: 하드코딩(300ms, 200ms) 대신 AttackCooldown 수치에 비례하여 동적으로 대기 시간 분배
                // 쿨타임의 40% 시점에 타격 판정, 나머지 60%를 후딜레이로 사용
                float attackCooldown = m_config.AttackCooldown;
                int preDelay = (int)(attackCooldown * 0.4f * 1000);
                int postDelay = (int)(attackCooldown * 0.6f * 1000);

                await UniTask.Delay(preDelay, cancellationToken: token);

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

                await UniTask.Delay(postDelay, cancellationToken: token);

                m_logic.EndAction();
                PlayAnimation(PlayerState.IDLE);
            }
            catch (OperationCanceledException)
            {
                // 연타에 의한 취소는 의도된 것이며 별도 로그 없이 종료
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerView] StartAttackSequence 예외 발생: {ex.Message}");
            }
        }

        private GameObject FindNearestEnemy()
        {
            // [기반 수정]: 타격 판정 시 프레임 지연이나 적의 미세 이동으로 인한 '씹힘' 방지를 위해 사거리 여유값(0.2f) 추가
            float searchRange = m_config.AttackRange + 0.2f;
            
            // [기반 수정]: 사용자의 요구사항에 따라 최전방 적(Leader)이 있다면 해당 적을 최우선 타겟으로 삼음
            GameObject frontEnemy = GetFrontEnemy();
            if (frontEnemy != null)
            {
                float distance = Vector2.Distance(transform.position, frontEnemy.transform.position);
                if (distance <= searchRange)
                {
                    return frontEnemy;
                }
            }

            // [개선]: 레이어 마스크를 사용하여 배경이나 UI 콜라이더의 간섭을 차단 (Enemy, Object 레이어 대상)
            int layerMask = LayerMask.GetMask("Enemy", "Object");
            var colliders = Physics2D.OverlapCircleAll(transform.position, searchRange, layerMask);
            
            float nearestDistance = float.MaxValue;
            GameObject nearestEnemy = null;

            foreach (var collider in colliders)
            {
                // [개선]: 자식 오브젝트의 콜라이더가 검출되더라도 부모의 IEnemyController나 태그를 확인하여 타겟으로 인정
                var enemyController = collider.GetComponent<IEnemyController>() ?? collider.GetComponentInParent<IEnemyController>();
                bool isEnemy = enemyController != null || collider.CompareTag("Enemy") || (collider.transform.root.CompareTag("Enemy"));

                if (isEnemy)
                {
                    // 실제 거리 계산은 루트 객체(또는 컨트롤러가 있는 객체) 기준으로 수행하는 것이 정확함
                    Transform targetTransform = (enemyController != null) ? ((MonoBehaviour)enemyController).transform : collider.transform;
                    float distance = Vector2.Distance(transform.position, targetTransform.position);
                    
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestEnemy = targetTransform.gameObject;
                    }
                }
            }

            // [디버그]: 타격 실패 시 주변에 인식된 오브젝트가 있는지 로그로 남겨 물리적 '씹힘' 원인 추적
            if (nearestEnemy == null && colliders.Length > 0)
            {
                string info = "";
                foreach(var c in colliders) info += $"[{c.name}(Layer:{c.gameObject.layer})] ";
                Debug.Log($"[PlayerView] 타격 판정 범위 내 오브젝트는 있으나 유효한 적 없음 (검출: {info})");
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
