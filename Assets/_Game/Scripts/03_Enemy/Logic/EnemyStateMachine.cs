using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 적의 상태를 관리하는 상태 머신입니다.
    /// </summary>
    public class EnemyStateMachine
    {
        #region 내부 변수
        private readonly Dictionary<Type, IEnemyState> m_states = new Dictionary<Type, IEnemyState>();
        private IEnemyState m_currentState;
        #endregion

        #region 공개 메서드
        public void AddState<T>(T state) where T : IEnemyState
        {
            m_states[typeof(T)] = state;
        }

        /// <summary>
        /// [설명]: 등록된 모든 상태를 제거합니다. 재사용 시 호출됩니다.
        /// </summary>
        public void ClearStates()
        {
            m_currentState?.OnExit();
            m_currentState = null;
            m_states.Clear();
        }

        /// <summary>
        /// [설명]: 특정 타입의 상태를 반환합니다.
        /// </summary>
        public T GetState<T>() where T : class, IEnemyState
        {
            if (m_states.TryGetValue(typeof(T), out var state))
            {
                return state as T;
            }
            return null;
        }

        public void ChangeState<T>() where T : IEnemyState
        {
            ChangeState(typeof(T));
        }

        public void ChangeState(Type type)
        {
            if (!m_states.TryGetValue(type, out var nextState))
            {
                Debug.LogError($"[EnemyStateMachine] 상태를 찾을 수 없습니다: {type.Name}");
                return;
            }

            m_currentState?.OnExit();
            m_currentState = nextState;
            m_currentState.OnEnter();
        }

        public void Tick()
        {
            m_currentState?.OnTick();
        }

        /// <summary>
        /// [설명]: 현재 상태가 특정 타입인지 확인합니다.
        /// </summary>
        public bool IsCurrentState<T>() where T : IEnemyState
        {
            return m_currentState is T;
        }
        #endregion
    }
}
