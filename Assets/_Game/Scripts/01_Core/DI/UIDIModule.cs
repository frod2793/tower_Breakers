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
                var cachedView = equipmentView;
                builder.RegisterBuildCallback(resolver =>
                {
                    var vm = resolver.Resolve<EquipmentViewModel>();
                    cachedView.Initialize(vm);
                });
            }

            if (hudView != null)
            {
                builder.RegisterComponent(hudView);
                var cachedView = hudView;
                builder.RegisterBuildCallback(resolver =>
                {
                    var vm = resolver.Resolve<HUDViewModel>();
                    cachedView.Initialize(vm);
                    UnityEngine.Debug.Log("[UIDIModule] HUDView 초기화 완료");
                });
            }

            if (gameOverView != null)
            {
                builder.RegisterComponent(gameOverView);
                var cachedView = gameOverView;
                builder.RegisterBuildCallback(resolver =>
                {
                    var vm = resolver.Resolve<GameOverViewModel>();
                    cachedView.Initialize(vm);
                    UnityEngine.Debug.Log("[UIDIModule] GameOverView 초기화 완료");
                });
            }

            if (inGameMenuView != null)
            {
                builder.RegisterComponent(inGameMenuView);
                var cachedView = inGameMenuView;
                builder.RegisterBuildCallback(resolver =>
                {
                    var vm = resolver.Resolve<InGameMenuViewModel>();
                    cachedView.Initialize(vm);
                    UnityEngine.Debug.Log("[UIDIModule] InGameMenuView 초기화 완료");
                });
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
