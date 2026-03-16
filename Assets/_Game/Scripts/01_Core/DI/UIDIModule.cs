using UnityEngine;
using TowerBreakers.UI.HUD;
using TowerBreakers.UI.Screens;
using TowerBreakers.UI.Equipment;
using TowerBreakers.UI.Effects.View;
using TowerBreakers.UI.Effects.Logic;
using TowerBreakers.Tower.View;
using TowerBreakers.Player.View;
using VContainer;
using VContainer.Unity;

namespace TowerBreakers.Core.DI
{
    /// <summary>
    /// [설명]: UI 시스템 DI 모듈입니다.
    /// </summary>
    public static class UIDIModule
    {
        public static void Register(IContainerBuilder builder,
            HUDView hudView, GameOverView gameOverView, EquipmentView equipmentView,
            InGameMenuView inGameMenuView,
            DamageTextView damageTextPrefab, Transform damageTextParent,
            PlayerEffectView playerEffectView, TowerTransitionPresenter towerTransitionPresenter)
        {
            builder.Register<HUDViewModel>(Lifetime.Singleton);
            builder.Register<GameOverViewModel>(Lifetime.Singleton);
            builder.Register<EquipmentViewModel>(Lifetime.Singleton);
            builder.Register<InGameMenuViewModel>(Lifetime.Singleton);

            if (damageTextPrefab != null)
            {
                builder.RegisterInstance(new DamageTextPool(damageTextPrefab, damageTextParent)).AsSelf();
                builder.RegisterEntryPoint<DamageTextPresenter>();
            }

            if (equipmentView != null)
            {
                builder.RegisterComponent(equipmentView);
                builder.RegisterBuildCallback(resolver => resolver.Inject(equipmentView));
            }

            if (hudView != null)
            {
                builder.RegisterComponent(hudView);
                builder.RegisterBuildCallback(resolver => resolver.Inject(hudView));
            }

            if (gameOverView != null)
            {
                builder.RegisterComponent(gameOverView);
                builder.RegisterBuildCallback(resolver => resolver.Inject(gameOverView));
            }

            if (inGameMenuView != null)
            {
                builder.RegisterComponent(inGameMenuView);
                builder.RegisterBuildCallback(resolver => resolver.Inject(inGameMenuView));
            }

            if (playerEffectView != null)
            {
                builder.RegisterComponent(playerEffectView);
            }

            if (towerTransitionPresenter != null)
            {
                builder.RegisterComponent(towerTransitionPresenter);
                var cachedPresenter = towerTransitionPresenter;
                builder.RegisterBuildCallback(resolver =>
                {
                    resolver.Inject(cachedPresenter);
                    UnityEngine.Debug.Log("[UIDIModule] TowerTransitionPresenter 인젝션 완료");
                });
            }
        }
    }
}
