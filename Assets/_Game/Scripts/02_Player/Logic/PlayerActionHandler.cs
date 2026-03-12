using System;

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
        #endregion

        public PlayerActionHandler(PlayerStateMachine stateMachine)
        {
            m_stateMachine = stateMachine;
        }

        #region 공개 메서드
        /// <summary>
        /// [설명]: 특정 액션을 실행합니다.
        /// </summary>
        public void ExecuteAction(PlayerActionType actionType)
        {
            switch (actionType)
            {
                case PlayerActionType.Attack:
                    m_stateMachine.ChangeState<PlayerAttackState>();
                    break;
                case PlayerActionType.Leap:
                    m_stateMachine.ChangeState<PlayerLeapState>();
                    break;
                case PlayerActionType.Skill1:
                    m_stateMachine.GetState<PlayerSkillState>().SetSkill(1);
                    m_stateMachine.ChangeState<PlayerSkillState>();
                    break;
                case PlayerActionType.Skill2:
                    m_stateMachine.GetState<PlayerSkillState>().SetSkill(2);
                    m_stateMachine.ChangeState<PlayerSkillState>();
                    break;
                case PlayerActionType.Skill3:
                    m_stateMachine.GetState<PlayerSkillState>().SetSkill(3);
                    m_stateMachine.ChangeState<PlayerSkillState>();
                    break;
                default:
                    UnityEngine.Debug.LogWarning($"[PlayerActionHandler] 미구현 액션: {actionType}");
                    break;
            }
        }
        #endregion
    }
}
