using TowerBreakers.Core.Events;
using TowerBreakers.Core.GameState;
using TowerBreakers.Core;
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
            builder.Register<EventBus>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<GameStateMachine>(Lifetime.Singleton);
            builder.Register<CooldownSystem>(Lifetime.Singleton);

            builder.Register<LoadingState>(Lifetime.Singleton);
            builder.Register<PlayingState>(Lifetime.Singleton);
            builder.Register<GameOverState>(Lifetime.Singleton);

            builder.RegisterEntryPoint<GameController>();
        }
    }
}
