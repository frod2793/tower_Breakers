using UnityEngine;
using TowerBreakers.Player.View;
using TowerBreakers.Player.Data;
using TowerBreakers.Enemy.Logic;
using TowerBreakers.Core.Events;

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
        private readonly IEventBus m_eventBus;
        private float m_attackTimer;

        // [최적화]: GC 할당을 방지하기 위한 정적 히트 버퍼
        private static readonly Collider2D[] s_hitBuffer = new Collider2D[32];
        #endregion

        public PlayerAttackState(PlayerView view, PlayerModel model, PlayerData data, IEventBus eventBus)
        {
            m_view = view;
            m_model = model;
            m_data = data;
            m_eventBus = eventBus;
        }

        public void OnEnter()
        {
            Debug.Log("[PlayerAttackState] 진입");
            m_attackTimer = 0f;
            ExecuteAttack();
        }

        public void OnExit()
        {
            Debug.Log("[PlayerAttackState] 퇴출");
        }

        public void OnTick()
        {
            m_attackTimer += Time.deltaTime;
            
            // 공격 애니메이션 시간 체크 (임시로 0.5초 후 Idle로 복귀)
            // 무기 속도에 따라 이 수치도 조정 가능하도록 확장 가능
            if (m_attackTimer >= 0.5f)
            {
                // 상태 전환 로직 필요 (StateMachine 참조 주입 필요 시 리팩토링)
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
            // 무기 데이터에 기인한 최종 사거리와 공격력을 가져옵니다.
            float attackRange = m_model.FinalAttackRange(m_data.AttackRange);
            int attackPower = m_model.FinalAttackPower(m_data.AttackPower);

            // 플레이어 전방(오른쪽)에 사거리만큼 박스를 그려 적을 탐색합니다.
            Vector2 attackPoint = (Vector2)m_view.transform.position + Vector2.right * (attackRange * 0.5f);
            Vector2 size = new Vector2(attackRange, 2.0f); // 세로 폭은 여유 있게 설정
            
            // "Enemy" 레이어만 필터링 (정의되지 않았을 경우 모든 레이어 탐색 후 컴포넌트로 필터링)
            int enemyLayer = LayerMask.GetMask("Enemy");
            if (enemyLayer == 0)
            {
                enemyLayer = -1; // All Layers
            }

            int hitCount = Physics2D.OverlapBoxNonAlloc(attackPoint, size, 0f, s_hitBuffer, enemyLayer);

            for (int i = 0; i < hitCount; i++)
            {
                var enemyCollider = s_hitBuffer[i];
                var controller = enemyCollider.GetComponent<EnemyController>();
                if (controller != null && !controller.IsDead)
                {
                    // 장착 중인 무기가 있다면 그 무기의 넉백 값을 적용
                    float knockback = 0f;
                    if (m_model.CurrentWeapon != null)
                    {
                        knockback = m_model.CurrentWeapon.KnockbackForce;
                    }

                    controller.TakeDamage(attackPower, knockback);
                }
            }

            // 3. 타격 연출 실행 (카메라 쉐이크, 역경직)
            if (hitCount > 0)
            {
                m_eventBus?.Publish(new OnHitEffectRequested(attackPoint, 0.4f, 0.15f, 0.08f));
            }
        }
    }
}
