using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerBreakers.Player.Logic
{
    /// <summary>
    /// [설명]: 플레이어의 상태를 변경하고 업데이트하는 상태 머신 클래스입니다.
    /// </summary>
    public class PlayerStateMachine
    {
        #region 내부 변수
        private readonly Dictionary<Type, IPlayerState> m_states = new Dictionary<Type, IPlayerState>();
        private IPlayerState m_currentState;
        private PlayerActionHandler m_actionHandler;
        #endregion

        #region 공개 메서드
        public void SetActionHandler(PlayerActionHandler handler)
        {
            m_actionHandler = handler;
        }

        public void AddState<T>(T state) where T : IPlayerState
        {
            m_states[typeof(T)] = state;
        }

        public T GetState<T>() where T : class, IPlayerState
        {
            var type = typeof(T);
            if (m_states.TryGetValue(type, out var state))
            {
                return state as T;
            }
            return null;
        }

        public void ChangeState<T>() where T : IPlayerState
        {
            var type = typeof(T);
            if (!m_states.TryGetValue(type, out var nextState))
            {
                Debug.LogError($"[PlayerStateMachine] 상태를 찾을 수 없습니다: {type.Name}");
                return;
            }

            m_currentState?.OnExit();
            m_currentState = nextState;
            m_currentState.OnEnter();
            
#if UNITY_EDITOR
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[PlayerStateMachine] 상태 전환: {type.Name}");
            #endif
#endif
        }

        public void Tick()
        {
            m_currentState?.OnTick();
            m_actionHandler?.Tick();
        }

        public bool IsCurrentState<T>() where T : IPlayerState
        {
            return m_currentState is T;
        }
        #endregion
    }
}
