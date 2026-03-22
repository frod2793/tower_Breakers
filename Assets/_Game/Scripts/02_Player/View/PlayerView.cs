using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using TowerBreakers.Player.DTO;
using TowerBreakers.Player.Logic;
using TowerBreakers.Player.Stat;
using TowerBreakers.UI;
using TowerBreakers.Tower.Service;
using Cysharp.Threading.Tasks;
using TowerBreakers.Battle;
using TowerBreakers.Core.Service;
using TowerBreakers.Enemy.Service;
using TowerBreakers.Player.Service;
using TowerBreakers.UI.ViewModel;
using VContainer;

namespace TowerBreakers.Player.View
{
    /// <summary>
    /// [설명]: 플레이어의 시각적 요소(애니메이션, 위치 갱신)를 담당하는 뷰 클래스입니다.
    /// 모든 물리/로직 이동은 PlayerLogic에서 수행하며, 본 클래스는 그 결과를 시각화합니다.
    /// </summary>
    public class PlayerView : MonoBehaviour
    {
        #region 에디터 설정
        [Header("캐릭터 참조")]
        [SerializeField, Tooltip("[설명]: SPUM 캐릭터 본체 참조")]
        private SPUM_Prefabs m_spumPrefabs;

        private Transform m_parryReference;
        #endregion

        #region 내부 필드
        private PlayerConfigDTO m_config;
        private PlayerLogic m_logic;
        public PlayerLogic Logic => m_logic;

        private BattleUIViewModel m_uiViewModel;
        private IPlayerStatService m_statService;
        private PlayerState m_currentAnimState = PlayerState.IDLE;
        private Vector3 m_lastPos;
        private CancellationTokenSource m_attackCts;
        private IEffectService m_effectService;
        private IEquipmentService m_equipmentService;
        #endregion

        #region 초기화 및 바인딩 로직
        public void Initialize(PlayerLogic logic, BattleUIViewModel uiViewModel, IPlayerStatService statService, IEquipmentService equipmentService, Transform parryReference, IEffectService effectService)
        {
            m_effectService = effectService;
            m_equipmentService = equipmentService;
            if (logic == null) return;

            m_logic = logic;
            m_config = logic.Config;
            m_uiViewModel = uiViewModel;
            m_statService = statService;
            m_parryReference = parryReference;

            if (m_spumPrefabs != null) m_spumPrefabs.OverrideControllerInit();

            // 로직 이벤트 바인딩
            m_logic.OnDashStarted += () => { CancelAttackSequence(); PlayAnimation(PlayerState.MOVE); };
            m_logic.OnParryStarted += () => { CancelAttackSequence(); StartParrySequence(); };
            m_logic.OnAttackStarted += StartAttackSequence;
            m_logic.OnWindstormSlashStarted += StartWindstormSlashSequence;
            m_logic.OnDamaged += OnPlayerDamaged;
            m_logic.OnDeath += OnPlayerDeath;

            if (m_uiViewModel != null) m_uiViewModel.OnSkillTriggered += HandleUISkill;

            m_lastPos = transform.position;
            m_logic.SetPosition(transform.position);
        }

        private void OnEnable()
        {
            ResetExpressions();
            PlayAnimation(PlayerState.IDLE);
        }

        private void ResetExpressions()
        {
            if (m_spumPrefabs == null || m_spumPrefabs._anim == null) return;
            m_spumPrefabs._anim.ResetTrigger("Damaged");
            m_spumPrefabs._anim.SetBool("isDeath", false);
        }

        private void HandleUISkill(string skillName)
        {
            if (m_logic == null) return;
            if (skillName == "Dash") m_logic.TryDash(Time.time);
            else if (skillName == "Parry")
            {
                float refX = m_parryReference != null ? m_parryReference.position.x : float.MaxValue;
                m_logic.TryParry(Time.time, refX);
            }
            else if (skillName == "Attack") m_logic.TryAttack(Time.time);
            else if (skillName == "Skill1") m_logic.TryWindstormSlash(Time.time);
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
            if (m_logic == null) return;

            Vector2 logicPos = m_logic.State.Position;
            float lerpSpeed = m_config.VisualLerpSpeed > 0.1f ? m_config.VisualLerpSpeed : 100f;
            transform.position = Vector3.Lerp(transform.position, new Vector3(logicPos.x, logicPos.y, transform.position.z), Time.deltaTime * lerpSpeed);

            // [수정]: 백덤블링 연출 조건 (목표 지점에 도달하기 전까지 활성화 - 벽까지 유지)
            float distToTargetX = Mathf.Abs(transform.position.x - m_logic.State.TargetPosition.x);
            if (m_logic.State.IsBackflip && distToTargetX > m_config.MovementArrivalThreshold)
            {
                float totalDistX = Mathf.Abs(m_logic.State.TargetPosition.x - m_logic.State.ParryStartPosition.x);
                float distThreshold = m_config.BackflipDistanceThreshold > 0.001f ? m_config.BackflipDistanceThreshold : 0.1f;
                if (totalDistX > distThreshold)
                {
                    float currentDistX = Mathf.Abs(transform.position.x - m_logic.State.ParryStartPosition.x);
                    float progress = Mathf.Clamp01(currentDistX / totalDistX);
                    float currentY = transform.localEulerAngles.y;
                    float rotDeg = m_config.BackflipRotationDegrees > 1f ? m_config.BackflipRotationDegrees : 720f;
                    transform.localRotation = Quaternion.Euler(0, currentY, progress * -rotDeg);
                }
            }
            else
            {
                if (transform.localRotation != Quaternion.identity)
                {
                    float currentY = transform.localEulerAngles.y;
                    transform.localRotation = Quaternion.Euler(0, currentY, 0);
                }
            }

            float dist = Vector2.Distance(transform.position, m_lastPos);
            m_lastPos = transform.position;

            if (m_logic.State.IsBeingPushed) PlayAnimation(PlayerState.IDLE);
            else if (dist > (m_config.animMovementThreshold > 0.001f ? m_config.animMovementThreshold : 0.01f)) PlayAnimation(PlayerState.MOVE);
            else PlayAnimation(PlayerState.IDLE);
        }

        private void OnDestroy()
        {
            if (m_uiViewModel != null) m_uiViewModel.OnSkillTriggered -= HandleUISkill;
            CancelAttackSequence();
        }
        #endregion

        #region 애니메이션 및 시퀀스
        private void PlayAnimation(PlayerState state)
        {
            if (m_currentAnimState == state && (state == PlayerState.IDLE || state == PlayerState.MOVE)) return;
            m_currentAnimState = state;
            if (m_spumPrefabs != null) m_spumPrefabs.PlayAnimation(state, 0);
        }

        private void CancelAttackSequence()
        {
            if (m_attackCts != null) { m_attackCts.Cancel(); m_attackCts.Dispose(); m_attackCts = null; }
        }

        private async void StartParrySequence()
        {
            PlayAnimation(PlayerState.ATTACK);
            GameObject target = FindNearestEnemyInRange(m_config.ParryActivationRange);
            if (target != null) PushAndStunAllEnemiesOnFloor(m_config.EnemyStopDuration, m_config.ParryPushForce);
            await UniTask.Delay((int)(m_config.ParryDuration * 1000));
        }

        private GameObject FindNearestEnemyInRange(float range)
        {
            GameObject frontEnemy = m_logic?.GetFrontEnemy();
            if (frontEnemy != null)
            {
                float distance = Mathf.Abs(frontEnemy.transform.position.x - transform.position.x);
                if (distance <= range) return frontEnemy;
            }
            return null;
        }

        private void PushAndStunAllEnemiesOnFloor(float duration, float pushForce)
        {
            var allEnemies = GameObject.FindGameObjectsWithTag("Enemy");
            
            // [최적화]: 중복 넉백/스턴 방지를 위한 HashSet 도입
            HashSet<EnemyPushController> processedEnemies = new HashSet<EnemyPushController>();

            foreach (var enemy in allEnemies)
            {
                if (Mathf.Abs(enemy.transform.position.y - transform.position.y) < m_config.EnemyDetectionYRange && enemy.transform.position.x > transform.position.x - m_config.EnemyDetectionXOffset)
                {
                    var leaderPush = enemy.GetComponent<EnemyPushController>();
                    if (leaderPush != null)
                    {
                        var current = leaderPush;
                        while (current != null)
                        {
                            // [핵심]: 이미 처리된 적은 건너뜀 (중복 연산 및 넉백 튐 방지)
                            if (!processedEnemies.Contains(current))
                            {
                                current.Stun(duration);
                                current.ApplyKnockback(Vector2.right, pushForce);
                                processedEnemies.Add(current);
                            }
                            current = current.FollowerEnemy;
                        }
                    }
                }
            }
        }

        private async void StartWindstormSlashSequence()
        {
            CancelAttackSequence();
            m_attackCts = new CancellationTokenSource();
            float originalTimeScale = Time.timeScale;
            try {
                // [연출]: 기모으기 단계 - 시간 감속 및 카메라 플레이어 줌인
                Time.timeScale = 0.2f;
                
                if (m_effectService != null)
                {
                    // [핵심]: 카메라 모드(Ortho/Persp)에 따른 줌 값 선택
                    Camera mainCam = Camera.main;
                    float targetZoomValue = (mainCam != null && mainCam.orthographic) 
                        ? m_config.WindstormZoomOrthoSize 
                        : m_config.WindstormZoomFOV;

                    Debug.Log($"[PlayerView] 질풍참 시퀀스 시작. Mode: {(mainCam?.orthographic == true ? "Ortho" : "Persp")}, TargetValue: {targetZoomValue}");
                    m_effectService.PlayCameraZoomOnTarget(transform, targetZoomValue, m_config.WindstormChargeDuration);
                }
                
                await UniTask.Delay(TimeSpan.FromSeconds(m_config.WindstormChargeDuration), ignoreTimeScale: true, cancellationToken: m_attackCts.Token);
                
                // [연출]: 돌진 준비 - 시간 정상화 및 카메라 복구
                Time.timeScale = originalTimeScale;
                m_effectService?.ResetCamera(0.1f);
                
                // [로직]: 적 앞까지 돌진 시작
                m_logic?.StartWindstormDash();
                PlayAnimation(PlayerState.MOVE);
                
                int dashDelay = m_config.WindstormDashDelayMs > 0 ? m_config.WindstormDashDelayMs : 100;
                await UniTask.Delay(dashDelay, cancellationToken: m_attackCts.Token);
                
                // [연출]: 타격 - 애니메이션 재생 및 화면 흔들림
                PlayAnimation(PlayerState.ATTACK);
                m_effectService?.PlayCameraShake(0.4f, 1.2f); 
                
                // [로직]: 실제 데미지 적용 (고정 피해 150, 최대 타겟 3명)
                if (m_logic != null)
                {
                    m_logic.ApplyWindstormDamage(150f);
                }

                // [연출]: 타격 이펙트 (비주얼 전용)
                GameObject leader = m_logic?.GetFrontEnemy();
                if (leader != null)
                {
                    int hitCount = 0;
                    var currentPush = leader.GetComponent<EnemyPushController>();
                    while (currentPush != null && hitCount < m_config.WindstormMaxTargets)
                    {
                        m_effectService?.PlaySpriteEffect("HitEffect", currentPush.transform.position, Quaternion.identity);
                        hitCount++;
                        currentPush = currentPush.FollowerEnemy;
                    }
                }

                int attackDelay = m_config.WindstormAttackDelayMs > 0 ? m_config.WindstormAttackDelayMs : 250;
                await UniTask.Delay(attackDelay, cancellationToken: m_attackCts.Token);

                if (m_logic != null) 
                { 
                    m_logic.EndAction(); 
                    PlayAnimation(PlayerState.IDLE); 
                }
            } catch (OperationCanceledException) { }
            finally {
                Time.timeScale = originalTimeScale;
                m_effectService?.ResetCamera(0.2f);
            }
        }

        private async void StartAttackSequence()
        {
            CancelAttackSequence();
            m_attackCts = new CancellationTokenSource();
            try {
                PlayAnimation(PlayerState.ATTACK);
                await UniTask.Delay((int)(m_config.AttackCooldown * 0.4f * 1000), cancellationToken: m_attackCts.Token);
                GameObject nearest = FindNearestEnemy();
                if (nearest != null)
                {
                    nearest.GetComponent<IEnemyController>()?.TakeDamage(m_statService.TotalAttack);
                    m_effectService?.PlaySpriteEffect("HitEffect", nearest.transform.position, Quaternion.identity);
                    m_effectService?.PlayDefaultCameraShake();
                }
                await UniTask.Delay((int)(m_config.AttackCooldown * 0.6f * 1000), cancellationToken: m_attackCts.Token);
                if (m_logic != null) { m_logic.EndAction(); PlayAnimation(PlayerState.IDLE); }
            } catch (OperationCanceledException) { }
        }

        private GameObject FindNearestEnemy()
        {
            GameObject front = m_logic?.GetFrontEnemy();
            if (front != null && Mathf.Abs(front.transform.position.x - transform.position.x) <= m_config.AttackRange + m_config.AttackRangeBuffer) return front;
            return null;
        }

        private void OnPlayerDamaged()
        {
            PlayAnimation(PlayerState.DAMAGED);
            if (m_spumPrefabs != null && m_spumPrefabs._anim != null) m_spumPrefabs._anim.SetTrigger("Damaged");
        }

        private void OnPlayerDeath()
        {
            PlayAnimation(PlayerState.DEATH);
            if (m_spumPrefabs != null && m_spumPrefabs._anim != null) m_spumPrefabs._anim.SetBool("isDeath", true);
            enabled = false;
        }
        #endregion
        
        public void CheatEquip(string itemId) => m_equipmentService?.Equip(itemId);
    }
}
