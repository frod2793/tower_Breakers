using System;
using System.Collections.Generic;

namespace TowerBreakers.Core.Events
{
    /// <summary>
    /// [설명]: 시스템 간 결합도를 낮추기 위한 이벤트 버스 클래스입니다.
    /// </summary>
    public class EventBus : IEventBus
    {
        #region 내부 필드
        /// <summary>
        /// [설명]: 타입별 이벤트 핸들러를 저장하는 딕셔너리입니다.
        /// </summary>
        private readonly Dictionary<Type, object> m_handlers = new Dictionary<Type, object>();
        #endregion

        #region 공개 메서드
        /// <summary>
        /// [설명]: 특정 타입의 이벤트를 발행합니다.
        /// </summary>
        /// <typeparam name="T">이벤트 타입 (struct)</typeparam>
        /// <param name="message">이벤트 데이터</param>
        public void Publish<T>(T message) where T : struct
        {
            var type = typeof(T);
            if (m_handlers.TryGetValue(type, out var handler))
            {
                ((Action<T>)handler)?.Invoke(message);
            }
        }

        /// <summary>
        /// [설명]: 특정 타입의 이벤트를 구독합니다.
        /// </summary>
        /// <typeparam name="T">이벤트 타입 (struct)</typeparam>
        /// <param name="action">콜백 메서드</param>
        public void Subscribe<T>(Action<T> action) where T : struct
        {
            var type = typeof(T);
            if (!m_handlers.ContainsKey(type))
            {
                m_handlers[type] = action;
            }
            else
            {
                m_handlers[type] = (Action<T>)m_handlers[type] + action;
            }
        }

        /// <summary>
        /// [설명]: 특정 타입의 이벤트를 구독 해제합니다.
        /// </summary>
        /// <typeparam name="T">이벤트 타입 (struct)</typeparam>
        /// <param name="action">콜백 메서드</param>
        public void Unsubscribe<T>(Action<T> action) where T : struct
        {
            var type = typeof(T);
            if (m_handlers.ContainsKey(type))
            {
                m_handlers[type] = (Action<T>)m_handlers[type] - action;
            }
        }
        #endregion
    }
}
