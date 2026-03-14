using UnityEngine;
using System.Collections.Generic;
using TowerBreakers.Player.View;
using TowerBreakers.Player.Data.Models;
using TowerBreakers.Player.Data.SO;
using TowerBreakers.Core.Events;
using TowerBreakers.Core.Interfaces;
using TowerBreakers.Enemy.Logic;
using TowerBreakers.Enemy.Data;
using TowerBreakers.Tower;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using TowerBreakers.Effects;
using TowerBreakers.Tower.Logic;

namespace TowerBreakers.Player.Logic.Skills
{
    /// <summary>
    /// [설명]: 스킬 1 - 윈드스톰(Windstorm)의 실행 로직을 담당하는 클래스입니다.
    /// 돌진형(Dash)과 범위형(Non-Dash) 모드를 지원합니다.
    /// </summary>
    public class WindstormSkillExecutor : ISkillExecutor
    {
        #region 내부 필드
        private PlayerView m_view;
        private PlayerModel m_model;
        private PlayerData m_data;
        private Effects.EffectManager m_effectManager;
        private IEventBus m_eventBus;
        private Core.CooldownSystem m_cooldownSystem;
        
        private int m_normalHitCount;
        private const string SKILL_NAME = "Windstorm";

        // 타격 판정 전용 버퍼 (GC 방지)
        private static readonly Collider2D[] s_hitBuffer = new Collider2D[32];
        private static readonly HashSet<IDamageable> s_processedDamageables = new HashSet<IDamageable>();
        
        // 정적 캐싱
        private static readonly int s_enemyLayerMask = LayerMask.GetMask("Enemy", "Object");
        
        // 정렬용 버퍼 (GC 방지)
        private static readonly Collider2D[] s_sortedHitBuffer = new Collider2D[32];
        private static readonly float[] s_distanceBuffer = new float[32];
        private static readonly RaycastHit2D[] s_raycastHitBuffer = new RaycastHit2D[32];
        #endregion

        #region 프로퍼티
        public bool IsOnCooldown => m_cooldownSystem != null && m_cooldownSystem.IsOnCooldown(SKILL_NAME);
        #endregion

        #region 초기화
        public void Initialize(PlayerView view, PlayerModel model, PlayerData data, IEventBus eventBus, PlayerProjectileFactory factory, Effects.EffectManager effectManager, Core.CooldownSystem cooldownSystem, TowerManager towerManager = null)
        {
            m_view = view;
            m_model = model;
            m_data = data;
            m_eventBus = eventBus;
            m_effectManager = effectManager;
            m_cooldownSystem = cooldownSystem;
        }
        #endregion

        #region 공개 API
        public async UniTask ExecuteAsync()
        {
            if (IsOnCooldown) return;

            // [추가]: 스킬 사운드 출력
            m_eventBus?.Publish(new OnSoundRequested("Slash"));

            m_normalHitCount = 0;
            float cooldown = m_data.SkillData != null ? m_data.SkillData.Skill1Cooldown : 3.0f;
            m_cooldownSystem?.SetCooldown(SKILL_NAME, cooldown);

            if (m_data.SkillData != null && m_data.SkillData.Skill1DashEnabled)
            {
                await DoDashThrust();
            }
            else
            {
                ExecuteDefault();
            }
        }

        public void OnTick(float deltaTime)
        {
            // CooldownSystem.Update()는 PlayerSkillState에서 일괄 호출하므로 여기서는 개별적으로 수행하지 않음
        }
        #endregion

        #region 내부 로직
        private void ExecuteDefault()
        {
            float attackRange = m_model.FinalAttackRange(m_data.AttackRange);
            int attackPower = m_model.FinalAttackPower(m_data.AttackPower);

            float facingDir = TowerBreakers.Core.Utilities.DirectionHelper.GetFacingSign(m_view.transform);
            Vector2 attackPoint = (Vector2)m_view.transform.position + Vector2.right * (facingDir * attackRange * 1.5f);
            Vector2 size = new Vector2(attackRange * 3f, 3.0f);
            float multiplier = m_data.SkillData != null ? m_data.SkillData.Skill1Multiplier : 1.5f;
            int baseDamage = (int)(attackPower * multiplier);

            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(s_enemyLayerMask);
            filter.useLayerMask = true;

            // [수정]: 스킬 판정 전에 모든 적의 콜라이더를 활성화하여 리더 이외의 적도 탐지 가능하게 합니다.
            EnemyPushLogic.EnableAllGroupCollidersForSkill();

            s_processedDamageables.Clear();
            int hitCount = Physics2D.OverlapBox(attackPoint, size, 0f, filter, s_hitBuffer);
            
            int normalHitLimit = m_data.SkillData != null ? m_data.SkillData.Skill1NormalHitLimit : 3;
#if UNITY_EDITOR
            Debug.Log($"[WINDSTORM] ExecuteDefault: Center={attackPoint}, Size={size}, Limit={normalHitLimit}, RawHits={hitCount}");
#endif

            int processedHits = 0;

            Vector2 origin = m_view.transform.position;
            int validCount = 0;
            for (int i = 0; i < hitCount; i++)
            {
                var col = s_hitBuffer[i];
                if (col == null) continue;
                
                // [설명]: 플레이어 자신 및 자식 객체 제외
                if (col.transform == m_view.transform || col.transform.IsChildOf(m_view.transform))
                    continue;

                var damageable = col.GetComponentInParent<IDamageable>();
                if (damageable == null || damageable.IsDead)
                    continue;

                s_sortedHitBuffer[validCount] = col;
                s_distanceBuffer[validCount] = Vector2.Distance(origin, col.transform.position);
                validCount++;
            }
            
#if UNITY_EDITOR
            Debug.Log($"[WINDSTORM] ExecuteDefault: ValidCandidates={validCount}");
#endif
            
            // 거리 기반 정렬 (전방 적부터 타격)
            for (int i = 0; i < validCount - 1; i++)
            {
                for (int j = i + 1; j < validCount; j++)
                {
                    if (s_distanceBuffer[i] > s_distanceBuffer[j])
                    {
                        var tempCol = s_sortedHitBuffer[i];
                        s_sortedHitBuffer[i] = s_sortedHitBuffer[j];
                        s_sortedHitBuffer[j] = tempCol;

                        var tempDist = s_distanceBuffer[i];
                        s_distanceBuffer[i] = s_distanceBuffer[j];
                        s_distanceBuffer[j] = tempDist;
                    }
                }
            }
            
            for (int i = 0; i < validCount; i++)
            {
                ApplyDamage(s_sortedHitBuffer[i], baseDamage, normalHitLimit, ref processedHits);
            }

            // [수정]: 판정 완료 후 콜라이더를 원래 상태(리더만 활성)로 복원합니다.
            EnemyPushLogic.DisableAllGroupCollidersForSkill();

#if UNITY_EDITOR
            Debug.Log($"[WINDSTORM] ExecuteDefault Finished: Hits={processedHits}, NormalHitCount={m_normalHitCount}");
#endif

            if (processedHits > 0)
            {
                // [리팩토링]: 프로필 없이 직접 타격 연출 요청
                m_eventBus?.Publish(new OnHitEffectRequested(attackPoint, 0.6f, 0.2f, 0.1f));
 
                if (m_effectManager != null) m_effectManager.PlayEffect(EffectType.Hit, attackPoint);
            }
        }

        private async UniTask DoDashThrust()
        {
            if (m_view == null) return;
            var skillData = m_data.SkillData;
            var cts = m_view.GetCancellationTokenOnDestroy();

            float originalTimeScale = Time.timeScale;
            try
            {
                float zoomDelta = skillData.Skill1CameraZoomDelta;
                float zoomDuration = skillData.Skill1CameraZoomDuration;
                float timeScale = skillData.Skill1TimeScaleDuringDash;
                float dashMaxDist = skillData.Skill1DashMaxDistance;
                float dashSpeed = skillData.Skill1DashSpeed;

                EnemyPushLogic.EnableAllGroupCollidersForSkill();

                Camera cam = Camera.main;
                float originalSize = (cam != null) ? cam.orthographicSize : 5f;
                
                if (cam != null)
                {
                    cam.DOOrthoSize(Mathf.Max(1f, originalSize - zoomDelta), zoomDuration).SetEase(Ease.InOutQuad).ToUniTask(cancellationToken: cts).Forget();
                    await UniTask.Delay((int)(zoomDuration * 1000), cancellationToken: cts);
                }

                Time.timeScale = timeScale;

                Vector3 startPos = m_view.transform.position;
                float facingDir = TowerBreakers.Core.Utilities.DirectionHelper.GetFacingSign(m_view.transform);
                float dashDist = dashMaxDist;
                int hitCount = Physics2D.RaycastNonAlloc(startPos, Vector2.right * facingDir, s_raycastHitBuffer, dashDist, s_enemyLayerMask);
                
                float actualDash = dashDist;
                for (int i = 0; i < hitCount; i++)
                {
                    var damageable = s_raycastHitBuffer[i].collider.GetComponentInParent<IDamageable>();
                    if (damageable != null && !damageable.IsDead)
                    {
                        actualDash = Mathf.Max(0f, s_raycastHitBuffer[i].distance - 0.5f);
                        break;
                    }
                }

                Vector3 endPos = new Vector3(startPos.x + (actualDash * facingDir), startPos.y, startPos.z);
                await m_view.transform.DOMoveX(endPos.x, actualDash / dashSpeed).SetEase(Ease.Linear).ToUniTask(cancellationToken: cts);

                ProcessDashHits(startPos, skillData, out int killedCount);

                if (killedCount > 0)
                {
                    // [리팩토링]: 프로필 없이 직접 타격 연출 요청
                    m_eventBus?.Publish(new OnHitEffectRequested(endPos, 0.6f, 0.2f, 0.1f));
 
                    if (m_effectManager != null) m_effectManager.PlayEffect(EffectType.Hit, endPos);
                }

                if (cam != null)
                {
                    cam.DOOrthoSize(originalSize, zoomDuration).SetEase(Ease.InOutQuad).ToUniTask(cancellationToken: cts).Forget();
                    await UniTask.Delay((int)(zoomDuration * 1000), cancellationToken: cts);
                }
                
                Time.timeScale = originalTimeScale;
            }
            catch (System.OperationCanceledException) { Time.timeScale = 1.0f; }
            finally
            {
                Time.timeScale = originalTimeScale;
                EnemyPushLogic.DisableAllGroupCollidersForSkill();
                m_model.Position = m_view.transform.position;
            }
        }

        private void ProcessDashHits(Vector3 startPos, PlayerSkillData skillData, out int killedCount)
        {
            s_processedDamageables.Clear();
            killedCount = 0;
            
            float multiplier = skillData.Skill1Multiplier;
            int damage = (int)(m_model.FinalAttackPower(m_data.AttackPower) * multiplier);
            int normalHitLimit = skillData.Skill1NormalHitLimit;
            
            float hitRange = skillData.Skill1DashMaxDistance;
            float facingDir = TowerBreakers.Core.Utilities.DirectionHelper.GetFacingSign(m_view.transform);
            Vector2 boxSize = new Vector2(hitRange + 3.0f, 4.0f);
            Vector2 boxCenter = new Vector2(startPos.x + (facingDir * hitRange * 0.5f), startPos.y);
            
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(s_enemyLayerMask);
            filter.useLayerMask = true;

            int count = Physics2D.OverlapBox(boxCenter, boxSize, 0f, filter, s_hitBuffer);
            
            int validCount = 0;
            for (int i = 0; i < count; i++)
            {
                var col = s_hitBuffer[i];
                if (col == null) continue;

                if (col.transform == m_view.transform || col.transform.IsChildOf(m_view.transform))
                    continue;
                
                var damageable = col.GetComponentInParent<IDamageable>();
                if (damageable == null || damageable.IsDead)
                    continue;

                if (s_processedDamageables.Contains(damageable))
                    continue;

                s_sortedHitBuffer[validCount] = col;
                s_distanceBuffer[validCount] = Vector2.Distance(startPos, col.transform.position);
                validCount++;
            }
            
            // 거리 기반 정렬
            for (int i = 0; i < validCount - 1; i++)
            {
                for (int j = i + 1; j < validCount; j++)
                {
                    if (s_distanceBuffer[i] > s_distanceBuffer[j])
                    {
                        (s_sortedHitBuffer[i], s_sortedHitBuffer[j]) = (s_sortedHitBuffer[j], s_sortedHitBuffer[i]);
                        (s_distanceBuffer[i], s_distanceBuffer[j]) = (s_distanceBuffer[j], s_distanceBuffer[i]);
                    }
                }
            }
            
            int processedNum = 0;
#if UNITY_EDITOR
            Debug.Log($"[WINDSTORM] ProcessDashHits: BoxCenter={boxCenter}, BoxSize={boxSize}, RawHits={count}, ValidCandidates={validCount}");
#endif

            for (int i = 0; i < validCount; i++)
            {
                var col = s_sortedHitBuffer[i];
                if (ApplyDamage(col, damage, normalHitLimit, ref processedNum))
                {
                    killedCount++;
                }
            }
#if UNITY_EDITOR
            Debug.Log($"[WINDSTORM] ProcessDashHits Finished: Processed={processedNum}, Killed={killedCount}, NormalHitCount={m_normalHitCount}");
#endif
        }

        private bool ApplyDamage(Collider2D col, int damage, int normalHitLimit, ref int processedHits)
        {
            var damageable = col.GetComponentInParent<IDamageable>();
            if (damageable == null || damageable.IsDead) return false;

            // [설명]: 한 번의 스킬 실행에서 동일한 IDamageable(적)을 중복 타격하지 않도록 체크합니다.
            if (s_processedDamageables.Contains(damageable)) return false;
            s_processedDamageables.Add(damageable);

            var enemy = col.GetComponentInParent<EnemyController>();
            if (enemy != null)
            {
                if (enemy.Type == EnemyType.Normal)
                {
                    if (m_normalHitCount < normalHitLimit)
                    {
#if UNITY_EDITOR
                        Debug.Log($"[WINDSTORM] Hit Normal: {enemy.name}, Count: {m_normalHitCount + 1}/{normalHitLimit}");
#endif
                        // [설명]: 일반 적은 즉사 판정 (스킬 데이터의 배율 적용)
                        float mult = m_data.SkillData != null ? m_data.SkillData.Skill1NormalDamageMultiplier : 10f;
                        damageable.TakeDamage((int)(damage * mult));
                        m_normalHitCount++;
                        processedHits++;
                        return true;
                    }
#if UNITY_EDITOR
                    else
                    {
                        Debug.Log($"[WINDSTORM] Normal Hit Limit Reached. Skip: {enemy.name}");
                    }
#endif
                }
                else
                {
#if UNITY_EDITOR
                    Debug.Log($"[WINDSTORM] Hit Special/Boss: {enemy.name}");
#endif
                    // [설명]: 특수 적은 낮은 배율 (설정 가능)
                    float mult = m_data.SkillData != null ? m_data.SkillData.Skill1SpecialDamageMultiplier : 2f;
                    damageable.TakeDamage((int)(damage * mult));
                    enemy.ApplyKnockback(2.0f, 0.25f);
                    processedHits++;
                    return true;
                }
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log($"[WINDSTORM] Hit Object: {col.name}");
#endif
                // [설명]: 오브젝트에는 기본 데미지 (스킬 데이터의 배율 적용)
                float mult = m_data.SkillData != null ? m_data.SkillData.Skill1ObjectDamageMultiplier : 1f;
                damageable.TakeDamage((int)(damage * mult));
                processedHits++;
                return true;
            }
            return false;
        }
        #endregion
    }
}
