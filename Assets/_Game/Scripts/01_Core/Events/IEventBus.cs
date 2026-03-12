using System;

namespace TowerBreakers.Core.Events
{
    /// <summary>
    /// [설명]: 이벤트 버스 인터페이스입니다.
    /// </summary>
    public interface IEventBus
    {
        void Publish<T>(T message) where T : struct;
        void Subscribe<T>(Action<T> action) where T : struct;
        void Unsubscribe<T>(Action<T> action) where T : struct;
    }
}
