using UnityEngine;
using TowerBreakers.Player.View;
using TowerBreakers.Player.Data.SO;
using TowerBreakers.Player.Data.Models;
using TowerBreakers.Core.Events;
using TowerBreakers.Core.Interfaces;

namespace TowerBreakers.Player.Logic
{
    /// <summary>
    /// [설명]: 플레이어의 기본 공격 상태입니다.
    /// </summary>
    public class PlayerAttackState : IPlayerState
    {
        #region 내부 필드
        private readonly PlayerView m_view;
        private readonly PlayerModel m_model;
        private readonly PlayerData m_data;
        private readonly PlayerStateMachine m_stateMachine;
        private readonly IEventBus m_eventBus;
        private float m_attackTimer;
        private float m_currentAttackDuration; // 이번 공격의 지속 시간 (데이터 기반)

        // [최적화]: GC 할당 및 문자열 파싱 방지를 위한 정적 캐싱 필드들
        private static readonly Collider2D[] s_hitBuffer = new Collider2D[32];
        private static readonly int s_targetLayer = LayerMask.GetMask("Enemy", "Object");
        private static readonly ContactFilter2D s_hitFilter = CreateHitFilter();

        private static ContactFilter2D CreateHitFilter()
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(s_targetLayer);
            filter.useLayerMask = true;
            filter.useTriggers = true;
            return filter;
        }
        #endregion

        public PlayerAttackState(PlayerView view, PlayerModel model, PlayerData data, PlayerStateMachine stateMachine, IEventBus eventBus)
        {
            m_view = view;
            m_model = model;
            m_data = data;
            m_stateMachine = stateMachine;
            m_eventBus = eventBus;
        }

        public void OnEnter()
        {
            m_attackTimer = 0f;
            // [데이터 연동]: 실시간 장착 무기 보정이 포함된 최종 공격 속도(AttackSpeed)를 반영합니다.
            float finalAttackSpeed = m_model != null ? m_model.FinalAttackSpeed(m_data.AttackSpeed) : m_data.AttackSpeed;
            
            // 공속 1이면 1초, 2이면 0.5초 대기 후 Idle 복귀
            m_currentAttackDuration = Mathf.Max(0.2f, 1.0f / Mathf.Max(0.1f, finalAttackSpeed));

#if UNITY_EDITOR
            // [최적화]: 런타임 성능을 위해 에디터에서만 로그 출력
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[PlayerAttackState] OnEnter - 지속시간: {m_currentAttackDuration:F2}s, 최종공속: {finalAttackSpeed}");
            #endif
#endif
            ExecuteAttack();
        }

        public void OnExit()
        {
        }

        public void OnTick()
        {
            m_attackTimer += Time.deltaTime;
            
            // [데이터 연동]: 공격 속도 기반으로 Idle 상태 복귀 결정
            if (m_attackTimer >= m_currentAttackDuration)
            {
                m_stateMachine?.ChangeState<PlayerIdleState>();
            }
        }

        private void ExecuteAttack()
        {

            // 1. SPUM 공격 애니메이션 재생
            if (m_view != null)
            {
                m_view.PlayAnimation(global::PlayerState.ATTACK, 0);
            }


            // 2. 물리적 타격 판정 (Raycast/Overlap)
            float attackRange = m_model.FinalAttackRange(m_data.AttackRange);
            int attackPower = m_model.FinalAttackPower(m_data.AttackPower);

            Vector2 attackPoint = (Vector2)m_view.transform.position + Vector2.right * (attackRange * 0.5f);
            Vector2 size = new Vector2(attackRange, 2.0f);
            int hitCount = Physics2D.OverlapBox(attackPoint, size, 0.0f, s_hitFilter, s_hitBuffer);


            int validHitCount = 0;
            float knockback = (m_model.CurrentWeapon != null) ? m_model.CurrentWeapon.KnockbackForce : 0f;

            for (int i = 0; i < hitCount; i++)
            {
                var hitCollider = s_hitBuffer[i];
                if (hitCollider == null) continue;

                // [최적화]: IDamageable 컴포넌트 접근 최적화 (대부분의 경우 루트 오브젝트에 위치)
                if (!hitCollider.TryGetComponent<IDamageable>(out var damageable))
                {
                    // 루트에 없을 경우에만 부모 탐색 (차선의 방법)
                    if (hitCollider.transform.parent != null)
                    {
                        hitCollider.transform.parent.TryGetComponent<IDamageable>(out damageable);
                    }
                }

                if (damageable != null)
                {
                    validHitCount++; // [개선]: 시체(IsDead)를 때려도 타격감 유지를 위해 카운트 포함

                    if (!damageable.IsDead)
                    {
                        damageable.TakeDamage(attackPower, knockback);
                    }
                }
            }


            // 3. 타격 연출 실행 (카메라 쉐이크, 역경직)
            if (validHitCount > 0)
            {
                m_eventBus?.Publish(new OnHitEffectRequested(attackPoint, 0.4f, 0.15f, 0.08f));
            }

        }
    }
}
