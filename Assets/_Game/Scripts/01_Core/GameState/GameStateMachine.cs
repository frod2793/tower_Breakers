using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TowerBreakers.Core.GameState
{
    /// <summary>
    /// [설명]: 게임의 전체 전역 상태를 관리하는 상태 머신입니다.
    /// </summary>
    public class GameStateMachine : IDisposable
    {
        #region 내부 필드
        private readonly Dictionary<Type, IGameState> m_states = new Dictionary<Type, IGameState>();
        private IGameState m_currentState;
        private bool m_isTransitioning;
        #endregion

        #region 공개 메서드
        /// <summary>
        /// [설명]: 상태를 추가합니다.
        /// </summary>
        public void AddState<T>(T state) where T : IGameState
        {
            m_states[typeof(T)] = state;
        }

        /// <summary>
        /// [설명]: 특정 상태로 전환합니다.
        /// </summary>
        public async UniTask ChangeState<T>() where T : IGameState
        {
            if (m_isTransitioning) return;
            
            var type = typeof(T);
            if (!m_states.TryGetValue(type, out var nextState))
            {
                Debug.LogError($"[GameStateMachine] 상태를 찾을 수 없습니다: {type.Name}");
                return;
            }

            m_isTransitioning = true;

            if (m_currentState != null)
            {
                await m_currentState.OnExit();
            }

            m_currentState = nextState;
            await m_currentState.OnEnter();

            m_isTransitioning = false;
        }

        /// <summary>
        /// [설명]: 라이프사이클 업데이트를 수행합니다.
        /// </summary>
        public void Tick()
        {
            if (m_isTransitioning) return;
            m_currentState?.OnUpdate();
        }

        public void Dispose()
        {
            m_states.Clear();
            m_currentState = null;
        }
        #endregion
    }
}
