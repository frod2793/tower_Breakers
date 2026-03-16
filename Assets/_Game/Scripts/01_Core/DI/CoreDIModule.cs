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
            builder.Register<EventBus>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<CooldownSystem>(Lifetime.Singleton);
            builder.Register<SceneLoader>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            
            // 씬 전환 데이터가 없는 경우를 위한 기본 DTO 등록
            builder.Register<SceneContextDTO>(Lifetime.Singleton);
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
