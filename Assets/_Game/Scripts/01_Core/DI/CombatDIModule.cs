using TowerBreakers.Combat.Logic;
using TowerBreakers.Combat.View;
using TowerBreakers.Effects;
using VContainer;
using VContainer.Unity;

namespace TowerBreakers.Core.DI
{
    /// <summary>
    /// [설명]: Combat 시스템 DI 모듈입니다.
    /// </summary>
    public static class CombatDIModule
    {
        public static void Register(IContainerBuilder builder, CombatEffectPresenter combatEffectPresenter,
            EffectManager effectManager)
        {
            builder.Register<CombatSystem>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();

            if (combatEffectPresenter != null)
            {
                builder.RegisterComponent(combatEffectPresenter);
            }

            if (effectManager != null)
            {
                builder.RegisterComponent(effectManager);
            }
        }
    }
}
