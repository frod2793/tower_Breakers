using TowerBreakers.Player.Data.SO;
using TowerBreakers.Player.Data.Models;
using TowerBreakers.Player.Logic;
using TowerBreakers.Player.Logic.Skills;
using TowerBreakers.Player.View;
using TowerBreakers.Input.Logic;
using VContainer;
using VContainer.Unity;
using TowerBreakers.Core.DI;
using TowerBreakers.Enemy.Factory;

namespace TowerBreakers.Core.DI
{
    /// <summary>
    /// [설명]: Player 시스템 DI 모듈입니다.
    /// </summary>
    public static class PlayerDIModule
    {
        public static void Register(IContainerBuilder builder, PlayerData playerData, PlayerView playerView,
            PlayerPushReceiver playerPushReceiver, PlayerDebugger playerDebugger, PlayerProjectileFactory projectileFactory)
        {
            if (playerData != null)
            {
                builder.RegisterInstance(playerData);
            }
            else
            {
                UnityEngine.Debug.LogError("[PlayerDIModule] PlayerData가 설정되지 않았습니다!");
            }

            builder.Register<TowerBreakers.Player.Data.UserSessionModel>(Lifetime.Singleton);
            builder.Register<PlayerModel>(Lifetime.Singleton);
            builder.Register<InventoryModel>(Lifetime.Singleton);
            builder.Register<PlayerStateMachine>(Lifetime.Singleton);
            builder.Register<PlayerActionHandler>(Lifetime.Singleton);

            builder.Register<PlayerIdleState>(Lifetime.Singleton);
            builder.Register<PlayerAttackState>(Lifetime.Singleton);
            builder.Register<PlayerLeapState>(Lifetime.Singleton);
            builder.Register<PlayerDefendState>(Lifetime.Singleton);

            // Skill Executor들을 Singleton으로 등록 (DIP 적용: 구체 클래스 대신 인터페이스로 주입)
            builder.Register<WindstormSkillExecutor>(Lifetime.Singleton).As<ISkillExecutor>();
            builder.Register<MissileSkillExecutor>(Lifetime.Singleton).As<ISkillExecutor>();
            builder.Register<SlashSkillExecutor>(Lifetime.Singleton).As<ISkillExecutor>();

            builder.Register<PlayerSkillState>(Lifetime.Singleton);

            if (playerView != null)
            {
                builder.RegisterComponent(playerView);
            }
            else
            {
                UnityEngine.Debug.LogError("[PlayerDIModule] PlayerView가 설정되지 않았습니다!");
            }

            if (projectileFactory != null)
            {
                builder.RegisterComponent(projectileFactory);
                builder.RegisterBuildCallback(resolver =>
                {
                    var skillState = resolver.Resolve<PlayerSkillState>();
                    var effectManager = resolver.Resolve<TowerBreakers.Effects.EffectManager>();
                    skillState.Initialize(projectileFactory, effectManager);
                    UnityEngine.Debug.Log("[PlayerDIModule] PlayerSkillState 초기화 완료");
                });
            }

            if (playerDebugger != null)
            {
                builder.RegisterComponent(playerDebugger);
            }

            if (playerPushReceiver != null)
            {
                builder.RegisterComponent(playerPushReceiver);
                builder.RegisterBuildCallback(resolver =>
                {
                    var factory = resolver.Resolve<EnemyFactory>();
                    factory.SetPlayerPushReceiver(playerPushReceiver);

                    var model = resolver.Resolve<PlayerModel>();
                    var eventBus = resolver.Resolve<Core.Events.IEventBus>();
                    playerPushReceiver.Initialize(model, eventBus);

                    UnityEngine.Debug.Log("[PlayerDIModule] PlayerPushReceiver 초기화 완료");
                });
            }
        }
    }
}
