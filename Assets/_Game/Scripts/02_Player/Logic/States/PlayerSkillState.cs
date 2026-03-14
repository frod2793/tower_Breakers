using UnityEngine;
using TowerBreakers.Player.View;
using TowerBreakers.Player.Data.SO;
using TowerBreakers.Player.Data.Models;
using TowerBreakers.Core.Events;
using Cysharp.Threading.Tasks;
using TowerBreakers.Enemy.Logic;

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
        #endregion

        #region 인터페이스 로직
        public void OnEnter()
        {
            Debug.Log($"[PlayerSkillState] 스킬 {m_skillIndex} 사용");
            
            // SPUM 스킬 애니메이션 호출 (OTHER_List 참조)
            if (m_view != null)
            {
                m_view.PlayAnimation(global::PlayerState.OTHER, m_skillIndex);
            }

            ExecuteSkillLogic(m_skillIndex);
            ReturnToIdleAfterDelay().Forget();
        }

        public void OnExit() { }
        public void OnTick() { }
        #endregion

        #region 내부 비즈니스 로직
        /// <summary>
        /// [설명]: 스킬 인덱스에 따라 각기 다른 전투 로직을 실행합니다.
        /// </summary>
        private void ExecuteSkillLogic(int index)
        {
            switch (index)
            {
                case 0: ExecuteWindstorm(); break;
                case 1: ExecutePowerStrike(); break;
                case 2: ExecuteShieldBash(); break;
                default: ExecuteWindstorm(); break;
            }
        }

        /// <summary>
        /// [설명]: 스킬 1 - 윈드스톰: 전방 넓은 범위를 다단히트(시뮬레이션) 타격합니다.
        /// </summary>
        private void ExecuteWindstorm()
        {
            float attackRange = m_model.FinalAttackRange(m_data.AttackRange);
            int attackPower = m_model.FinalAttackPower(m_data.AttackPower);

            Vector2 attackPoint = (Vector2)m_view.transform.position + Vector2.right * (attackRange * 1.5f);
            Vector2 size = new Vector2(attackRange * 3f, 3.0f);
            int damage = (int)(attackPower * m_data.Skill1Multiplier);
            
            PerformOverlapAttack(attackPoint, size, damage, "윈드스톰", 0.6f, 0.2f, 0.1f);
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
            int damage = (int)(attackPower * m_data.Skill2Multiplier);
            
            PerformOverlapAttack(attackPoint, size, damage, "파워 스트라이크", 0.8f, 0.3f, 0.15f);
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
            int damage = (int)(attackPower * m_data.Skill3Multiplier);
            
            int enemyLayer = LayerMask.GetMask("Enemy");
            Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(attackPoint, size, 0f, enemyLayer);

            if (hitEnemies.Length > 0)
            {
                m_eventBus?.Publish(new OnHitEffectRequested(attackPoint, 0.5f, 0.2f, 0.1f));
            }

            foreach (var col in hitEnemies)
            {
                var controller = col.GetComponent<EnemyController>();
                if (controller != null)
                {
                    controller.TakeDamage(damage);
                    controller.ChangeState<EnemyStunnedState>();
                }
            }
        }

        /// <summary>
        /// [설명]: 주어진 범위 내의 적을 탐색하여 데미지를 입히고 연출 이벤트를 발행합니다.
        /// </summary>
        private void PerformOverlapAttack(Vector2 point, Vector2 size, int damage, string skillName, float intensity, float duration, float hitStop)
        {
            int enemyLayer = LayerMask.GetMask("Enemy");
            Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(point, size, 0f, enemyLayer);

            if (hitEnemies.Length > 0)
            {
                m_eventBus?.Publish(new OnHitEffectRequested(point, intensity, duration, hitStop));
            }

            foreach (var col in hitEnemies)
            {
                var controller = col.GetComponent<EnemyController>();
                if (controller != null)
                {
                    controller.TakeDamage(damage);
                }
            }
        }

        private async UniTaskVoid ReturnToIdleAfterDelay()
        {
            // 스킬 애니메이션 시간에 맞춰 Idle 상태로 복귀
            await UniTask.Delay(800);
            if (m_stateMachine != null)
            {
                m_stateMachine.ChangeState<PlayerIdleState>();
            }
        }
        #endregion
    }
}
