using TowerBreakers.Core.Events;
using TowerBreakers.Core.GameState;
using TowerBreakers.Core;
using TowerBreakers.Core.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace TowerBreakers.Core.DI
{
    /// <summary>
    /// [설명]: Core 시스템 DI 모듈입니다.
    /// </summary>
    public static class CoreDIModule
    {
        public static void Register(IContainerBuilder builder)
        {
            RegisterBaseSystems(builder);
            RegisterGameplaySystems(builder);
        }

        public static void RegisterBaseSystems(IContainerBuilder builder)
        {
            // [설명]: 전역 시스템(EventBus, SceneLoader 등)은 ProjectLifetimeScope에서 관리됩니다.
            // 각 씬의 LifetimeScope는 상위 스코프의 인스턴스를 자동으로 Resolve 합니다.
        }

        public static void RegisterGameplaySystems(IContainerBuilder builder)
        {
            builder.Register<GameStateMachine>(Lifetime.Singleton);
            builder.Register<LoadingState>(Lifetime.Singleton);
            builder.Register<PlayingState>(Lifetime.Singleton);
            builder.Register<GameOverState>(Lifetime.Singleton);
            builder.RegisterEntryPoint<GameController>();
        }
    }
}
