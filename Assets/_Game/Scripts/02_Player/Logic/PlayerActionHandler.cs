using TowerBreakers.Player.Data.Models;
using TowerBreakers.Player.Data.SO;
using TowerBreakers.Core.Events;
using UnityEngine;

namespace TowerBreakers.Player.Logic
{
    /// <summary>
    /// [설명]: 플레이어의 6가지 액타입을 정의합니다.
    /// </summary>
    public enum PlayerActionType
    {
        Attack,
        Skill1,
        Skill2,
        Skill3,
        Leap,
        Defend
    }

    /// <summary>
    /// [설명]: 플레이어의 버튼 입력을 받아 실제 액션을 실행하는 핸들러 클래스입니다.
    /// </summary>
    public class PlayerActionHandler
    {
        #region 내부 필드
        private readonly PlayerStateMachine m_stateMachine;
        private readonly IEventBus m_eventBus;
        private readonly PlayerData m_playerData;
        private readonly PlayerModel m_playerModel;
        private float m_lastAttackTime = -10.0f; // [최적화]: 초기값을 음수로 설정하여 즉시 첫 공격 가능
        #endregion

        public PlayerActionHandler(PlayerStateMachine stateMachine, PlayerData playerData, PlayerModel playerModel, IEventBus eventBus)
        {
            m_stateMachine = stateMachine;
            m_playerData = playerData;
            m_playerModel = playerModel;
            m_eventBus = eventBus;
        }

        #region 공개 메서드
        /// <summary>
        /// [설명]: 특정 액션을 실행합니다.
        /// </summary>
        public void ExecuteAction(PlayerActionType actionType)
        {
#if UNITY_EDITOR
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnityEngine.Debug.Log($"[PlayerActionHandler] ExecuteAction 호출: {actionType}");
            #endif
#endif
            
            // [최적화]: Attack 액션 시에는 이벤트 발행 생략 (불필요한 브로드캐스트 비용 절감)
            if (actionType != PlayerActionType.Attack)
            {
                m_eventBus?.Publish(new Core.Events.OnPlayerActionStarted(actionType));
            }

            // 스킬 쿨다운 중인 경우 실행 방지 로직 추가
            if (actionType == PlayerActionType.Skill1 || actionType == PlayerActionType.Skill2 || actionType == PlayerActionType.Skill3)
            {
                var skillState = m_stateMachine.GetState<PlayerSkillState>();
                if (skillState != null && skillState.IsSkillOnCooldown((int)actionType - (int)PlayerActionType.Skill1))
                {
                    // 쿨다운 중인 스킬은 무쓸행동 무시
                    #if UNITY_EDITOR
                    UnityEngine.Debug.Log("[PlayerActionHandler] 스킬 쿨다운 중: 실행 무시");
                    #endif
                    return;
                }
            }
            switch (actionType)
            {
                case PlayerActionType.Attack:
                    m_stateMachine.ChangeState<PlayerAttackState>();
                    break;
                case PlayerActionType.Leap:
                    m_stateMachine.ChangeState<PlayerLeapState>();
                    break;
                case PlayerActionType.Skill1:
                    // Align with 0-based skill indices in PlayerSkillState
                    m_stateMachine.GetState<PlayerSkillState>().SetSkill(0);
                    m_stateMachine.ChangeState<PlayerSkillState>();
                    break;
                case PlayerActionType.Skill2:
                    m_stateMachine.GetState<PlayerSkillState>().SetSkill(1);
                    m_stateMachine.ChangeState<PlayerSkillState>();
                    break;
                case PlayerActionType.Skill3:
                    m_stateMachine.GetState<PlayerSkillState>().SetSkill(2);
                    m_stateMachine.ChangeState<PlayerSkillState>();
                    break;
                case PlayerActionType.Defend:
                    m_stateMachine.ChangeState<PlayerDefendState>();
                    break;
                default:
#if UNITY_EDITOR
                    UnityEngine.Debug.LogWarning($"[PlayerActionHandler] 미구현 액션: {actionType}");
#endif
                    break;
            }
        }
        #endregion
    }
}
