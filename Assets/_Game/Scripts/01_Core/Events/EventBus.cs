using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerBreakers.Core.Events
{
    /// <summary>
    /// [클래스]: 타입 기반 Pub/Sub 메시징 시스템의 구현체입니다. 
    /// Dictionary를 활용하여 타입별 델리게이트 체인을 관리하며, 
    /// 개별 이벤트 핸들러의 예외가 전체 시스템에 영향을 주지 않도록 격리 처리합니다.
    /// </summary>
    public class EventBus : IEventBus
    {
        #region 내부 필드
        /// <summary>
        /// [필드]: 각 이벤트 타입별로 등록된 델리게이트(Action<T>)를 저장하는 딕셔너리입니다.
        /// </summary>
        private readonly Dictionary<Type, object> m_eventHandlers = new Dictionary<Type, object>();
        #endregion

        #region 공개 메서드
        /// <summary>
        /// [설명]: 특정 타입의 이벤트를 발행합니다. 등록된 모든 구독자에게 메시지가 순차적으로 전달됩니다.
        /// </summary>
        /// <typeparam name="T">이벤트 타입 (struct)</typeparam>
        /// <param name="message">이벤트 데이터</param>
        public void Publish<T>(T message) where T : struct
        {
            Type eventType = typeof(T);

            if (m_eventHandlers.TryGetValue(eventType, out object handlers))
            {
                if (handlers is Action<T> actionChain)
                {
                    // [설명]: 개별 핸들러의 예외가 발행 전체를 멈추지 않도록 InvocationList 순회
                    var invocationList = actionChain.GetInvocationList();
                    foreach (var handler in invocationList)
                    {
                        try
                        {
                            ((Action<T>)handler).Invoke(message);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[EventBus] '{eventType.Name}' 이벤트를 처리하는 도중 예외가 발생했습니다: {ex.Message}\n{ex.StackTrace}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// [설명]: 특정 타입의 이벤트를 구독합니다.
        /// </summary>
        /// <typeparam name="T">구독할 이벤트 타입 (struct)</typeparam>
        /// <param name="action">콜백 메서드</param>
        public void Subscribe<T>(Action<T> action) where T : struct
        {
            Type eventType = typeof(T);

            if (m_eventHandlers.TryGetValue(eventType, out object existingHandlers))
            {
                m_eventHandlers[eventType] = (Action<T>)existingHandlers + action;
            }
            else
            {
                m_eventHandlers[eventType] = action;
            }
        }

        /// <summary>
        /// [설명]: 특정 타입의 이벤트 구독을 해제합니다.
        /// </summary>
        /// <typeparam name="T">해제할 이벤트 타입 (struct)</typeparam>
        /// <param name="action">등록된 콜백 메서드</param>
        public void Unsubscribe<T>(Action<T> action) where T : struct
        {
            Type eventType = typeof(T);

            if (m_eventHandlers.TryGetValue(eventType, out object existingHandlers))
            {
                var newChain = (Action<T>)existingHandlers - action;

                if (newChain == null)
                {
                    m_eventHandlers.Remove(eventType);
                }
                else
                {
                    m_eventHandlers[eventType] = newChain;
                }
            }
        }
        #endregion
    }
}
