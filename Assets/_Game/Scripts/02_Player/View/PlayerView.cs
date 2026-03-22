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

        // [변경]: 하드코딩 방지를 위해 BattleLifetimeScope에서 주입받습니다.
        private Transform m_parryReference;
        #endregion

        #region 내부 필드
        private PlayerConfigDTO m_config;
        private PlayerLogic m_logic;
        public PlayerLogic Logic => m_logic;

        public void CheatEquip(string itemId)
        {
            m_equipmentService?.Equip(itemId);
        }
        private BattleUIViewModel m_uiViewModel;
        private IPlayerStatService m_statService;
        private PlayerState m_currentAnimState = PlayerState.IDLE;
        private Vector3 m_lastPos;
        private CancellationTokenSource m_attackCts;
        #endregion

        #region 초기화 및 바인딩 로직
        private IEquipmentService m_equipmentService;

        public void Initialize(PlayerLogic logic, BattleUIViewModel uiViewModel, IPlayerStatService statService, IEquipmentService equipmentService, Transform parryReference)
        {
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
                Debug.Log($"[PlayerView] 패링 UI 트리거: refX={refX}, PlayerX={transform.position.x}");
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

            // [핵심]: 로직 업데이트를 먼저 수행 (이동 변위 계산)
            m_logic.Update(Time.time, Time.deltaTime);
            
            // [핵심]: 로직의 결과를 뷰에 반영 (Lerp 속도를 100f로 하여 거의 즉각 동기화)
            UpdateVisualMovement();
        }

        private void UpdateVisualMovement()
        {
            if (m_logic == null) return;

            Vector2 logicPos = m_logic.State.Position;
            
            // 시각적 떨림 방지를 위한 매우 빠른 보간 (또는 즉시 대입)
            transform.position = Vector3.Lerp(transform.position, new Vector3(logicPos.x, logicPos.y, transform.position.z), Time.deltaTime * 100f);

            // [추가]: 백덤블링 회전 처리 (2바퀴 시계 반대 방향)
            if (m_logic.State.IsBackflip)
            {
                float totalDistX = Mathf.Abs(m_logic.State.TargetPosition.x - m_logic.State.ParryStartPosition.x);
                if (totalDistX > 0.1f)
                {
                    float currentDistX = Mathf.Abs(transform.position.x - m_logic.State.ParryStartPosition.x);
                    float progress = Mathf.Clamp01(currentDistX / totalDistX);
                    // [변경]: Y축 회전 고정을 해제하고 현재 Y축 값을 유지한 채 Z축 회전(백덤블링) 수행
                    float currentY = transform.localEulerAngles.y;
                    transform.localRotation = Quaternion.Euler(0, currentY, progress * 720f);
                }
            }
            else
            {
                if (transform.localRotation != Quaternion.identity)
                {
                    // [변경]: 기본 상태로 돌아올 때도 기존의 Y축 회전값(방향)을 유지하도록 계산
                    float currentY = transform.localEulerAngles.y;
                    transform.localRotation = Quaternion.Euler(0, currentY, 0);
                }
            }

            float dist = Vector2.Distance(transform.position, m_lastPos);
            m_lastPos = transform.position;

            if (m_logic.State.IsBeingPushed) PlayAnimation(PlayerState.IDLE);
            else if (dist > 0.01f) PlayAnimation(PlayerState.MOVE);
            else PlayAnimation(PlayerState.IDLE);
        }

        private void OnDestroy()
        {
            if (m_uiViewModel != null) m_uiViewModel.OnSkillTriggered -= HandleUISkill;
            CancelAttackSequence();
        }
        #endregion

        #region 애니메이션 및 시퀀스 (생략 없이 유지)
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
            
            // 1. 최소 애니메이션/이펙트 대기
            await UniTask.Delay((int)(m_config.ParryDuration * 1000));
            
            // 2. [수정]: 퇴각(리코일)이 진행 중이라면 완료될 때까지 대기 (후퇴 속도에 따른 가변 시간 대응)
            if (m_logic != null && m_logic.State.IsRetreating)
            {
                await UniTask.WaitUntil(() => !m_logic.State.IsRetreating);
                Debug.Log("[PlayerView] 퇴각 완료 확인 - 패링 액션 종료");
            }
            
            if (m_logic != null) { m_logic.EndAction(); PlayAnimation(PlayerState.IDLE); }
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
            foreach (var enemy in allEnemies)
            {
                if (Mathf.Abs(enemy.transform.position.y - transform.position.y) < 2.0f && enemy.transform.position.x > transform.position.x - 0.5f)
                {
                    var push = enemy.GetComponent<EnemyPushController>();
                    if (push != null) { push.Stun(duration); push.ApplyKnockback(Vector2.right, pushForce); }
                }
            }
        }

        private async void StartWindstormSlashSequence()
        {
            CancelAttackSequence();
            m_attackCts = new CancellationTokenSource();
            try {
                // 1. 대시 애니메이션 및 잔상 연출 시작
                PlayAnimation(PlayerState.MOVE);
                
                // 2. 대시 완료 대기 (PlayerLogic에서 Position을 MoveTowards로 업데이트함)
                // 시각적으로 대시가 끝날 때까지 약간의 대기
                await UniTask.Delay(150, cancellationToken: m_attackCts.Token);
                
                // 3. 발도 타격 애니메이션
                PlayAnimation(PlayerState.ATTACK);
                
                // 4. 연출용 슬로우 모션 또는 히트스탑 (필요 시)
                
                await UniTask.Delay(300, cancellationToken: m_attackCts.Token);
                
                if (m_logic != null) { m_logic.EndAction(); PlayAnimation(PlayerState.IDLE); }
            } catch (OperationCanceledException) { }
        }

        private async void StartAttackSequence()
        {
            CancelAttackSequence();
            m_attackCts = new CancellationTokenSource();
            try {
                PlayAnimation(PlayerState.ATTACK);
                await UniTask.Delay((int)(m_config.AttackCooldown * 0.4f * 1000), cancellationToken: m_attackCts.Token);
                GameObject nearest = FindNearestEnemy();
                if (nearest != null) nearest.GetComponent<IEnemyController>()?.TakeDamage(m_statService.TotalAttack);
                await UniTask.Delay((int)(m_config.AttackCooldown * 0.6f * 1000), cancellationToken: m_attackCts.Token);
                if (m_logic != null) { m_logic.EndAction(); PlayAnimation(PlayerState.IDLE); }
            } catch (OperationCanceledException) { }
        }

        private GameObject FindNearestEnemy()
        {
            GameObject front = m_logic?.GetFrontEnemy();
            if (front != null && Mathf.Abs(front.transform.position.x - transform.position.x) <= m_config.AttackRange + 0.2f) return front;
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
    }
}
