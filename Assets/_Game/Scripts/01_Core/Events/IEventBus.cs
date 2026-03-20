using System;

namespace TowerBreakers.Core.Events
{
    /// <summary>
    /// [인터페이스]: 타입 기반 Pub/Sub 메시징 시스템을 위한 이벤트 버스 인터페이스입니다.
    /// 모든 이벤트 메시지는 가비지 컬렉션(GC) 부하를 줄이기 위해 struct로 제한됩니다.
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// [설명]: 특정 타입의 이벤트 메시지를 발행(Publish)합니다.
        /// 해당 이벤트를 구독 중인 모든 핸들러에게 메시지가 전달됩니다.
        /// </summary>
        /// <typeparam name="T">이벤트 타입 (struct)</typeparam>
        /// <param name="message">전달할 이벤트 데이터</param>
        void Publish<T>(T message) where T : struct;

        /// <summary>
        /// [설명]: 특정 타입의 이벤트를 구독(Subscribe)합니다.
        /// </summary>
        /// <typeparam name="T">구독할 이벤트 타입 (struct)</typeparam>
        /// <param name="action">이벤트 발생 시 실행할 콜백 메서드</param>
        void Subscribe<T>(Action<T> action) where T : struct;

        /// <summary>
        /// [설명]: 특정 타입의 이벤트 구독을 해제(Unsubscribe)합니다.
        /// </summary>
        /// <typeparam name="T">구독 해제할 이벤트 타입 (struct)</typeparam>
        /// <param name="action">등록했던 콜백 메서드</param>
        void Unsubscribe<T>(Action<T> action) where T : struct;
    }
}
