using System;
using TowerBreakers.Core.Events;

namespace TowerBreakers.UI.Screens
{
    /// <summary>
    /// [설명]: 게임 오버 화면의 로직을 담당하는 뷰모델입니다.
    /// </summary>
    public class GameOverViewModel : IDisposable
    {
        private readonly IEventBus m_eventBus;

        public event Action OnShow;

        public GameOverViewModel(IEventBus eventBus)
        {
            m_eventBus = eventBus;
            m_eventBus.Subscribe<OnGameOver>(HandleGameOver);
        }

        private void HandleGameOver(OnGameOver evt)
        {
            OnShow?.Invoke();
        }

        public void RestartGame()
        {
            // 재시작 이벤트 발행
            m_eventBus.Publish(new OnGameStart());
        }

        public void Dispose()
        {
            m_eventBus.Unsubscribe<OnGameOver>(HandleGameOver);
        }
    }
}
