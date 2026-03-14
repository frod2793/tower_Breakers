using UnityEngine;
using System.Collections.Generic;
using TowerBreakers.Player.View;
using TowerBreakers.Player.Data.SO;
using TowerBreakers.Player.Data.Models;
using TowerBreakers.Core.Events;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TowerBreakers.Core.Interfaces;
using TowerBreakers.Enemy.Logic;
using TowerBreakers.Enemy.Data;

namespace TowerBreakers.Player.Logic
{
    /// <summary>
    /// [설명]: 플레이어의 스킬 액션을 처리하는 공용 상태입니다.
    /// 스킬 타입에 따라 다른 애니메이션과 로직을 수행합니다.
    /// </summary>
    public class PlayerSkillState : IPlayerState
    {
        #region 내부 필드
        private readonly PlayerView m_view;
        private readonly PlayerStateMachine m_stateMachine;
        private readonly PlayerModel m_model;
        private readonly PlayerData m_data;
        private readonly IEventBus m_eventBus;
        
        private int m_skillIndex;
        private bool m_isDashing = false;

        // 쿨다운 남은 시간
        private float m_skill1CooldownRemaining;
        private float m_skill2CooldownRemaining;
        private float m_skill3CooldownRemaining;
        
        // 스킬 1의 일반 몹 처치 카운트
        private int m_skill1NormalHitCount;

        // 밀림/벽 압착 방지를 위한 참조
        private PlayerPushReceiver m_pushReceiver;

        // [최적화]: GC 할당 방지를 위한 캐싱 필드
        private static readonly Collider2D[] s_hitBuffer = new Collider2D[32];
        private static readonly HashSet<GameObject> s_processedHits = new HashSet<GameObject>();
        private static readonly int s_targetLayer = LayerMask.GetMask("Enemy", "Object");
        private static readonly ContactFilter2D s_hitFilter = CreateHitFilter();

        private static ContactFilter2D CreateHitFilter()
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(s_targetLayer);
            filter.useLayerMask = true;
            return filter;
        }
        #endregion

        public PlayerSkillState(PlayerView view, PlayerModel model, PlayerStateMachine stateMachine, PlayerData data, IEventBus eventBus)
        {
            m_view = view;
            m_model = model;
            m_stateMachine = stateMachine;
            m_data = data;
            m_eventBus = eventBus;
        }

        #region 공개 메서드
        public void SetSkill(int index) => m_skillIndex = index;
        /// <summary>
        /// [설명]: 해당 스킬이 현재 쿨다운 중인지 확인합니다. 0: 스킬1, 1: 스킬2, 2: 스킬3
        /// </summary>
        public bool IsSkillOnCooldown(int index)
        {
            switch (index)
            {
                case 0: return m_skill1CooldownRemaining > 0f;
                case 1: return m_skill2CooldownRemaining > 0f;
                case 2: return m_skill3CooldownRemaining > 0f;
                default: return false;
            }
        }
        /// <summary>
        /// [설명]: 대시 도중 모든 적 그룹 콜라이더를 활성화합니다.
        /// </summary>
        private void AllGroupCollidersEnableForDash()
        {
            EnemyPushLogic.EnableAllGroupCollidersForSkill();
        }

        /// <summary>
        /// [설명]: 스킬 1의 돌진 및 일격 필살 로직을 수행합니다.
        /// </summary>
        private async UniTaskVoid DoDashThrust()
        {
            if (m_view == null) return;
            if (m_isDashing) return;
            m_isDashing = true;

            var cts = m_view.GetCancellationTokenOnDestroy();

            try
            {
                // SkillData가 null이면 기본값 사용
                var skillData = m_data.SkillData;
                float zoomDelta = skillData != null ? skillData.Skill1CameraZoomDelta : 0.8f;
                float zoomDuration = skillData != null ? skillData.Skill1CameraZoomDuration : 0.25f;
                float timeScale = skillData != null ? skillData.Skill1TimeScaleDuringDash : 0.5f;
                float dashMaxDist = skillData != null ? skillData.Skill1DashMaxDistance : 6.0f;
                float dashSpeed = skillData != null ? skillData.Skill1DashSpeed : 24.0f;

                // Disable group colliders for dash (플레이어 통과를 위해)
                EnemyPushLogic.EnableAllCollidersForSkill();

                // Cinematic: 카메라 확대
                Camera cam = Camera.main;
                float originalTimeScale = Time.timeScale;
                float originalSize = (cam != null) ? cam.orthographicSize : 0f;
                
                if (cam != null)
                {
                    float targetSize = Mathf.Max(1f, originalSize - zoomDelta);
                    cam.DOOrthoSize(targetSize, zoomDuration).SetEase(Ease.InOutQuad).ToUniTask(cancellationToken: cts).Forget();
                    await UniTask.Delay((int)(zoomDuration * 1000), cancellationToken: cts);
                }

                // 느린 체감 시간 적용
                Time.timeScale = timeScale;

                // 앞의 장애물(적) 확인 후 실제 돌진 거리 보정
                // [수정]: 사망한 적을 무시하고 살아있는 적만 감지
                Vector3 startPos = m_view.transform.position;
                float dashDist = dashMaxDist;
                int enemyLayerMask = LayerMask.GetMask("Enemy");
                RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(startPos.x, startPos.y), Vector2.right, dashDist, enemyLayerMask);
                
                float actualDash = dashDist;
                foreach (var hit in hits)
                {
                    if (hit.collider != null)
                    {
                        var damageable = hit.collider.GetComponentInParent<IDamageable>();
                        // 사망한 적은 무시
                        if (damageable == null || damageable.IsDead)
                            continue;
                        
                        // 살아있는 적이 있으면 그곳에서 멈춤
                        actualDash = Mathf.Max(0f, hit.distance - 0.5f);
                        break;
                    }
                }

                Vector3 endPos = new Vector3(startPos.x + actualDash, startPos.y, startPos.z);
                
                // 돌진 이동 (속도 기반 계산: 거리 / 속도 = 시간)
                float dashDuration = actualDash / dashSpeed;
                await m_view.transform.DOMoveX(endPos.x, dashDuration).SetEase(Ease.Linear).ToUniTask(cancellationToken: cts);

                // [최적화]: GC 할당 제거를 위해 정적 필드 재사용
                s_processedHits.Clear();
                
                float multiplier = skillData != null ? skillData.Skill1Multiplier : 1.5f;
                int damage = (int)(m_model.FinalAttackPower(m_data.AttackPower) * multiplier);
                int normalHitLimit = skillData != null ? skillData.Skill1NormalHitLimit : 3;
                int killedCount = 0;
                
                // 타격 판정 범위 (항상 최대 돌진 거리 기준으로 감지)
                float hitRange = skillData != null ? skillData.Skill1DashMaxDistance : 8.0f;
                Vector2 boxSize = new Vector2(hitRange + 3.0f, 4.0f);
                Vector2 boxCenter = new Vector2(startPos.x + (hitRange * 0.5f), startPos.y);
                
                int count = Physics2D.OverlapBox(boxCenter, boxSize, 0f, s_hitFilter, s_hitBuffer);
                
                var sortedHits = new List<Collider2D>();
                for (int i = 0; i < count; i++)
                {
                    var col = s_hitBuffer[i];
                    if (col == null) continue;

                    // Self-hit 방지: 플레이어 콜라이더와 직접 비교
                    var playerCollider = m_view.GetComponent<Collider2D>();
                    if (playerCollider != null && col == playerCollider)
                        continue;
                    
                    // 같은 Transform인 경우도 제외
                    if (col.transform == m_view.transform)
                        continue;
                    
                    GameObject targetObj = col.gameObject;
                    if (s_processedHits.Contains(targetObj)) continue;
                    
                    sortedHits.Add(col);
                }
                
                sortedHits.Sort((a, b) => 
                {
                    float distA = Vector2.Distance(m_view.transform.position, a.transform.position);
                    float distB = Vector2.Distance(m_view.transform.position, b.transform.position);
                    return distA.CompareTo(distB);
                });
                
                foreach (var col in sortedHits)
                {
                    GameObject targetObj = col.gameObject;
                    s_processedHits.Add(targetObj);

                    var damageable = targetObj.GetComponentInParent<IDamageable>();
                    if (damageable != null && !damageable.IsDead)
                    {
                        var enemy = targetObj.GetComponentInParent<EnemyController>();
                        if (enemy != null)
                        {
                            if (enemy.Type == EnemyType.Normal)
                            {
                                if (m_skill1NormalHitCount < normalHitLimit)
                                {
                                    damageable.TakeDamage(damage * 10);
                                    m_skill1NormalHitCount++;
                                    killedCount++;
                                }
                            }
                            else
                            {
                                damageable.TakeDamage(damage * 2);
                                enemy.ApplyKnockback(2.0f, 0.25f);
                                killedCount++;
                            }
                        }
                        else
                        {
                            damageable.TakeDamage(damage);
                            killedCount++;
                        }
                    }
                }

                if (killedCount > 0)
                {
                    m_eventBus?.Publish(new OnHitEffectRequested(endPos, 0.6f, 0.2f, 0.1f));
                }

                // 복구 로직
                if (cam != null)
                {
                    cam.DOOrthoSize(originalSize, zoomDuration).SetEase(Ease.InOutQuad).ToUniTask(cancellationToken: cts).Forget();
                    await UniTask.Delay((int)(zoomDuration * 1000), cancellationToken: cts);
                }
                
                Time.timeScale = originalTimeScale;
            }
            catch (System.OperationCanceledException)
            {
                // 취소 시 시간 복구만 수행
                Time.timeScale = 1.0f;
            }
            finally
            {
                EnemyPushLogic.EnableAllGroupCollidersForSkill();
                m_isDashing = false;
                
                // 스킬 종료 후 위치 동기화 (벽 판정 방지)
                if (m_model != null && m_view != null)
                {
                    m_model.Position = m_view.transform.position;
                }
                
                // 안전하게 상태 복귀
                if (m_view != null)
                {
                    m_stateMachine?.ChangeState<PlayerIdleState>();
                }
            }
        }
        #endregion

        #region 인터페이스 로직
        public void OnEnter()
        {
#if UNITY_EDITOR
            // [최적화]: 런타임 성능을 위해 에디터에서만 로그 출력

#endif
            
            // 스킬 사용 중 벽 압착 판정 방지를 위해 밀림 수신 비활성화
            if (m_pushReceiver == null && m_view != null)
            {
                m_view.TryGetComponent(out m_pushReceiver);
            }
            if (m_pushReceiver != null)
            {
                m_pushReceiver.IsClampingEnabled = false;
            }

            // 진행 중인 트윈 제거 (이전 상태의 잔존 연출 방지)
            if (m_view != null)
            {
                m_view.transform.DOKill();
            }

            // SPUM 스킬 애니메이션 호출 (OTHER_List 참조)
            // 스킬 카운트 초기화
            m_skill1NormalHitCount = 0;
            if (m_view != null)
            {
                m_view.PlayAnimation(global::PlayerState.OTHER, m_skillIndex);
            }

            // 쿨다운 체크: 해당 스킬의 남은 쿨다운이 있으면 무시하고 Idle로 복귀
            bool isOnCooldown = false;
            if (m_skillIndex == 0 && m_skill1CooldownRemaining > 0f) isOnCooldown = true;
            if (m_skillIndex == 1 && m_skill2CooldownRemaining > 0f) isOnCooldown = true;
            if (m_skillIndex == 2 && m_skill3CooldownRemaining > 0f) isOnCooldown = true;

            if (isOnCooldown)
            {
                // 쿨다운 중이면 즉시 Idle로 돌아가고 로직 종료
                if (m_pushReceiver != null) m_pushReceiver.IsClampingEnabled = true;
                m_stateMachine?.ChangeState<PlayerIdleState>();
                return;
            }

            bool isAsyncSkill = ExecuteSkillLogic(m_skillIndex);
            
            // 일반 스킬(단발성)인 경우에만 일정 시간 후 Idle로 복귀
            if (!isAsyncSkill)
            {
                ReturnToIdleAfterDelay().Forget();
            }
        }

        public void OnExit()
        {
            // 스킬 종료 후 밀림 수신 및 벽 압착 판정 복구
            if (m_pushReceiver != null)
            {
                m_pushReceiver.IsClampingEnabled = true;
            }
        }

        public void OnTick()
        {
            float deltaTime = Time.deltaTime;

            // Cooldown countdown
            if (m_skill1CooldownRemaining > 0f) 
                m_skill1CooldownRemaining = Mathf.Max(0f, m_skill1CooldownRemaining - deltaTime);
            
            if (m_skill2CooldownRemaining > 0f) 
                m_skill2CooldownRemaining = Mathf.Max(0f, m_skill2CooldownRemaining - deltaTime);
            
            if (m_skill3CooldownRemaining > 0f) 
                m_skill3CooldownRemaining = Mathf.Max(0f, m_skill3CooldownRemaining - deltaTime);
        }
        #endregion

        #region 내부 비즈니스 로직
        /// <summary>
        /// [설명]: 스킬 인덱스에 따라 각기 다른 전투 로직을 실행합니다.
        /// </summary>
        /// <returns>비동기 로직이 시작되었는지 여부</returns>
        private bool ExecuteSkillLogic(int index)
        {
            // index는 0,1,2 로 가정: Skill1 -> Windstorm, Skill2 -> PowerStrike, Skill3 -> ShieldBash
            switch (index)
            {
                case 0: return ExecuteWindstorm();
                case 1: ExecutePowerStrike(); return false;
                case 2: ExecuteShieldBash(); return false;
                default: return ExecuteWindstorm();
            }
        }

        /// <summary>
        /// [설명]: 스킬 1 - 윈드스톰: 전방 넓은 범위를 다단히트(시뮬레이션) 타격합니다.
        /// </summary>
        /// <returns>비동기 로직이 시작되었는지 여부</returns>
        private bool ExecuteWindstorm()
        {
            // SkillData가 없으면 기본값으로 처리
            if (m_data.SkillData == null)
            {
                Debug.LogWarning("[PlayerSkillState] SkillData가 할당되지 않았습니다. 기본값으로 실행합니다.");
                ExecuteWindstormDefault();
                return false;
            }
    
            // Dash(돌진 찌르기) 모드인 경우 Inspector에서 활성화된지 확인
            if (m_data.SkillData.Skill1DashEnabled)
            {
                DoDashThrust().Forget();
                return true;
            }
            ExecuteWindstormDefault();
            return false;
        }

        /// <summary>
        /// [설명]: 스킬 1 - 윈드스톰 기본 (Non-Dash) 로직
        /// </summary>
        private void ExecuteWindstormDefault()
        {
            float attackRange = m_model.FinalAttackRange(m_data.AttackRange);
            int attackPower = m_model.FinalAttackPower(m_data.AttackPower);

            Vector2 attackPoint = (Vector2)m_view.transform.position + Vector2.right * (attackRange * 1.5f);
            Vector2 size = new Vector2(attackRange * 3f, 3.0f);
            float multiplier = m_data.SkillData != null ? m_data.SkillData.Skill1Multiplier : 1.5f;
            int baseDamage = (int)(attackPower * multiplier);

            // 다수 타격 로직: 일반 몹과 특수 몹에 따라 다르게 처리
            int hitCount = Physics2D.OverlapBox(attackPoint, size, 0f, s_hitFilter, s_hitBuffer);
            int processedHits = 0;
            int normalHitLimit = m_data.SkillData != null ? m_data.SkillData.Skill1NormalHitLimit : 3;

            var sortedHits = new List<Collider2D>();
            for (int i = 0; i < hitCount; i++)
            {
                var col = s_hitBuffer[i];
                if (col == null) continue;
                // Self-hit 방지: 플레이어 본인 계층 제외
                if (col.transform.root == m_view.transform.root)
                    continue;
                
                sortedHits.Add(col);
            }
            
            sortedHits.Sort((a, b) => 
            {
                float distA = Vector2.Distance(m_view.transform.position, a.transform.position);
                float distB = Vector2.Distance(m_view.transform.position, b.transform.position);
                return distA.CompareTo(distB);
            });
            
            foreach (var col in sortedHits)
            {
                var damageable = col.GetComponentInParent<IDamageable>();
                if (damageable != null && !damageable.IsDead)
                {
                    var enemyController = col.GetComponentInParent<EnemyController>();
                    if (enemyController != null)
                    {
                        if (enemyController.Type == EnemyType.Normal)
                        {
                            if (m_skill1NormalHitCount < normalHitLimit)
                            {
                                damageable.TakeDamage(baseDamage * 10);
                                m_skill1NormalHitCount++;
                                processedHits++;
                            }
                        }
                        else
                        {
                            damageable.TakeDamage(baseDamage * 2);
                            enemyController.ApplyKnockback(2.0f, 0.25f);
                            processedHits++;
                        }
                    }
                    else
                    {
                        damageable.TakeDamage(baseDamage);
                        processedHits++;
                    }
                }
            }

            if (processedHits > 0)
            {
                m_eventBus?.Publish(new OnHitEffectRequested(attackPoint, 0.6f, 0.2f, 0.1f));
            }
            // Start cooldown for Skill1
            m_skill1CooldownRemaining = m_data.SkillData != null ? m_data.SkillData.Skill1Cooldown : 3.0f;
        }

        /// <summary>
        /// [설명]: 스킬 2 - 파워 스트라이크: 좁은 범위에 매우 강력한 일격을 가합니다.
        /// </summary>
        private void ExecutePowerStrike()
        {
            float attackRange = m_model.FinalAttackRange(m_data.AttackRange);
            int attackPower = m_model.FinalAttackPower(m_data.AttackPower);

            Vector2 attackPoint = (Vector2)m_view.transform.position + Vector2.right * (attackRange);
            Vector2 size = new Vector2(attackRange, 2.0f);
            float multiplier = m_data.SkillData != null ? m_data.SkillData.Skill2Multiplier : 3.5f;
            int damage = (int)(attackPower * multiplier);
            
            PerformOverlapAttack(attackPoint, size, damage, "파워 스트라이크", 0.8f, 0.3f, 0.15f);
            m_skill2CooldownRemaining = m_data.SkillData != null ? m_data.SkillData.Skill2Cooldown : 4.0f;
        }

        /// <summary>
        /// [설명]: 스킬 3 - 실드 배시: 적을 타격하여 데미지를 입히고 즉시 기절(Stun) 상태로 만듭니다.
        /// </summary>
        private void ExecuteShieldBash()
        {
            float attackRange = m_model.FinalAttackRange(m_data.AttackRange);
            int attackPower = m_model.FinalAttackPower(m_data.AttackPower);

            Vector2 attackPoint = (Vector2)m_view.transform.position + Vector2.right * (attackRange);
            Vector2 size = new Vector2(attackRange, 2.0f);
            float multiplier = m_data.SkillData != null ? m_data.SkillData.Skill3Multiplier : 1.2f;
            int damage = (int)(attackPower * multiplier);
            
            // [최적화]: 캐싱된 레이어 마스크와 필터를 사용하여 할당 제거
            int hitCount = Physics2D.OverlapBox(attackPoint, size, 0f, s_hitFilter, s_hitBuffer);

            int validHitCount = 0;
            
            for (int i = 0; i < hitCount; i++)
            {
                var col = s_hitBuffer[i];
                if (col == null) continue;

                // Self-hit 방지: 플레이어 본인 계층 제외
                if (col.transform.root == m_view.transform.root)
                {
                    continue;
                }

                var damageable = col.GetComponentInParent<IDamageable>();
                if (damageable != null && !damageable.IsDead)
                {
                    validHitCount++;
                    damageable.TakeDamage(damage);
                    
                    // 스턴 효과 적용 (몬스터인 경우에만)
                    var enemyController = col.GetComponentInParent<EnemyController>();
                    if (enemyController != null)
                    {
                        enemyController.ChangeState<EnemyStunnedState>();
                    }
                }
            }

            if (validHitCount > 0)
            {
                m_eventBus?.Publish(new OnHitEffectRequested(attackPoint, 0.5f, 0.2f, 0.1f));
            }
            m_skill3CooldownRemaining = m_data.SkillData != null ? m_data.SkillData.Skill3Cooldown : 3.5f;
        }

        /// <summary>
        /// [설명]: 주어진 범위 내의 적을 탐색하여 데미지를 입히고 연출 이벤트를 발행합니다.
        /// </summary>
        private void PerformOverlapAttack(Vector2 point, Vector2 size, int damage, string skillName, float intensity, float duration, float hitStop)
        {
            // [최적화]: 캐싱된 레이어 마스크와 필터를 사용하여 할당 제거
            int hitCount = Physics2D.OverlapBox(point, size, 0f, s_hitFilter, s_hitBuffer);

            int validHitCount = 0;
            for (int i = 0; i < hitCount; i++)
            {
                var col = s_hitBuffer[i];
                if (col == null) continue;
                // Self-hit 방지: 플레이어 본인 계층 제외
                if (col.transform.root == m_view.transform.root)
                {
                    continue;
                }

                var damageable = col.GetComponentInParent<IDamageable>();
                if (damageable != null && !damageable.IsDead)
                {
                    validHitCount++;
                    damageable.TakeDamage(damage);
                }
            }

            if (validHitCount > 0)
            {
                m_eventBus?.Publish(new OnHitEffectRequested(point, intensity, duration, hitStop));
            }
        }

        /// <summary>
        /// [설명]: 애니메이션 재생 완료 후 대기 상태로 복귀합니다.
        /// </summary>
        private async UniTaskVoid ReturnToIdleAfterDelay()
        {
            var cts = m_view != null ? m_view.GetCancellationTokenOnDestroy() : default;

            try
            {
                // 스킬 애니메이션 시간에 맞춰 Idle 상태로 복귀
                await UniTask.Delay(800, cancellationToken: cts);
                
                if (m_stateMachine != null && m_view != null)
                {
                    m_stateMachine.ChangeState<PlayerIdleState>();
                }
            }
            catch (System.OperationCanceledException) { }
        }
        #endregion
    }
}
