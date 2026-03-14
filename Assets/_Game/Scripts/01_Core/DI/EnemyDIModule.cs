using TowerBreakers.Enemy.Factory;
using TowerBreakers.Enemy.Logic;
using VContainer;
using VContainer.Unity;

namespace TowerBreakers.Core.DI
{
    /// <summary>
    /// [설명]: Enemy 시스템 DI 모듈입니다.
    /// </summary>
    public static class EnemyDIModule
    {
        public static void Register(IContainerBuilder builder, Enemy.View.EnemyDeathEffect deathEffect)
        {
            builder.RegisterComponent(deathEffect);
            builder.Register<EnemyFactory>(Lifetime.Singleton);
            builder.Register<EnemySpawner>(Lifetime.Singleton);
            builder.Register<ProjectileFactory>(Lifetime.Singleton);
        }
    }
}
