using TowerBreakers.Player.Data;
using TowerBreakers.Player.Logic;
using TowerBreakers.Player.View;
using TowerBreakers.Enemy.Factory;
using TowerBreakers.Enemy.Logic;
using TowerBreakers.Combat.Logic;
using TowerBreakers.Combat.View;
using TowerBreakers.Tower.Logic;
using TowerBreakers.UI.HUD;
using TowerBreakers.UI.Screens;
using TowerBreakers.Environment.Logic;
using TowerBreakers.Environment.View;
using TowerBreakers.Input.Logic;
using TowerBreakers.Core.Events;
using TowerBreakers.Core.GameState;
using TowerBreakers.Core;
using TowerBreakers.Tower.Data;
using TowerBreakers.UI.Effects.View;
using TowerBreakers.UI.Effects.Logic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace TowerBreakers.Core.DI
{
    /// <summary>
    /// [설명]: 게임 전역의 의존성을 주입하고 관리하는 LifetimeScope입니다.
    /// </summary>
    public class GameLifetimeScope : LifetimeScope
    {
        #region 에디터 설정
        [Header("ScriptableObject 데이터")]
        [SerializeField, Tooltip("플레이어 기본 스탯 데이터 (ScriptableObject)")]
        private PlayerData m_playerData;

        [SerializeField, Tooltip("타워 구성 데이터 (ScriptableObject)")]
        private TowerData m_towerData;

        [Header("씬 컴포넌트 참조")]
        [SerializeField, Tooltip("플레이어 뷰 (씬에 배치 필수)")]
        private PlayerView m_playerView;

        [SerializeField, Tooltip("플레이어 장비 컴포넌트")]
        private PlayerEquipment m_playerEquipment;

        [SerializeField, Tooltip("플레이어 밀림 수신자")]
        private PlayerPushReceiver m_playerPushReceiver;

        [SerializeField, Tooltip("환경 매니저")]
        private EnvironmentManager m_environmentManager;

        [SerializeField, Tooltip("전투 연출 프리젠터")]
        private CombatEffectPresenter m_combatEffectPresenter;

        [SerializeField, Tooltip("장비 UI 뷰")]
        private TowerBreakers.UI.Equipment.EquipmentView m_equipmentView;

        [SerializeField, Tooltip("HUD UI 뷰")]
        private HUDView m_hudView;

        [SerializeField, Tooltip("층 전환 연출 프리젠터")]
        private TowerBreakers.Tower.View.TowerTransitionPresenter m_towerTransitionPresenter;

        [Header("데미지 텍스트 설정")]
        [SerializeField, Tooltip("데미지 텍스트 프리팹 (DamageTextView)")]
        private DamageTextView m_damageTextPrefab;

        [SerializeField, Tooltip("데미지 텍스트가 생성될 부모 트랜스폼 (World Space Canvas 등)")]
        private Transform m_damageTextParent;
        #endregion

        protected override void Configure(IContainerBuilder builder)
        {
            // Core 시스템 등록
            builder.Register<EventBus>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<GameStateMachine>(Lifetime.Singleton);
            builder.Register<CooldownSystem>(Lifetime.Singleton);
            
            // ── 씬 컴포넌트 등록 (인스펙터에서 할당) ──
            RegisterSceneComponent(builder, m_environmentManager, "EnvironmentManager");
            RegisterSceneComponent(builder, m_playerView, "PlayerView", isRequired: true);
            RegisterSceneComponent(builder, m_playerEquipment, "PlayerEquipment");
            RegisterSceneComponent(builder, m_combatEffectPresenter, "CombatEffectPresenter");
            RegisterSceneComponent(builder, m_towerTransitionPresenter, "TowerTransitionPresenter");

            // PlayerPushReceiver → EnemyFactory에 지연 주입 및 초기화
            if (m_playerPushReceiver != null)
            {
                builder.RegisterComponent(m_playerPushReceiver);
                builder.RegisterBuildCallback(resolver =>
                {
                    // 1. 적군 팩토리에 플레이어 참조 전달
                    var factory = resolver.Resolve<EnemyFactory>();
                    factory.SetPlayerPushReceiver(m_playerPushReceiver);

                    // 2. 플레이어 밀기 수신자 자체 초기화 (모델 및 이벤트 버스 주입)
                    var model = resolver.Resolve<PlayerModel>();
                    var eventBus = resolver.Resolve<Core.Events.IEventBus>();
                    m_playerPushReceiver.Initialize(model, eventBus);
                    
                    Debug.Log("[GameLifetimeScope] PlayerPushReceiver 초기화 완료");
                });
            }
            else
            {
                Debug.LogWarning("[GameLifetimeScope] PlayerPushReceiver가 설정되지 않았습니다. 적 밀기 로직이 동작하지 않습니다.");
            }

            // ── ScriptableObject 데이터 등록 ──
            if (m_playerData != null)
            {
                builder.RegisterInstance(m_playerData);
            }
            else
            {
                Debug.LogError("[GameLifetimeScope] PlayerData가 인스펙터에 설정되지 않았습니다!");
            }

            if (m_towerData != null)
            {
                builder.RegisterInstance(m_towerData);
            }
            else
            {
                Debug.LogError("[GameLifetimeScope] TowerData가 인스펙터에 설정되지 않았습니다!");
            }

            // ── Player 시스템 등록 ──
            builder.Register<PlayerModel>(Lifetime.Singleton);
            builder.Register<InventoryModel>(Lifetime.Singleton);
            builder.Register<PlayerStateMachine>(Lifetime.Singleton);
            builder.Register<PlayerActionHandler>(Lifetime.Singleton);

            // ── Enemy 시스템 등록 ──
            builder.Register<EnemyFactory>(Lifetime.Singleton);
            builder.Register<EnemySpawner>(Lifetime.Singleton);
            builder.Register<ProjectileFactory>(Lifetime.Singleton);

            // ── Tower 시스템 등록 ──
            builder.Register<TowerManager>(Lifetime.Singleton);

            // ── Combat 시스템 등록 ──
            builder.Register<CombatSystem>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();

            // ── UI 시스템 등록 (HUD & Screens) ──
            builder.Register<HUDViewModel>(Lifetime.Singleton);
            builder.Register<GameOverViewModel>(Lifetime.Singleton);
            builder.Register<TowerBreakers.UI.Equipment.EquipmentViewModel>(Lifetime.Singleton);

            // ── 데미지 텍스트 시스템 등록 ──
            if (m_damageTextPrefab != null)
            {
                builder.RegisterInstance(new DamageTextPool(m_damageTextPrefab, m_damageTextParent)).AsSelf();
                builder.RegisterEntryPoint<DamageTextPresenter>();
            }
            else
            {
                Debug.LogWarning("[GameLifetimeScope] DamageTextPrefab이 설정되지 않았습니다.");
            }

            // EquipmentView 등록 및 ViewModel 연결
            if (m_equipmentView != null)
            {
                builder.RegisterComponent(m_equipmentView);
                var cachedEquipmentView = m_equipmentView;
                builder.RegisterBuildCallback(resolver =>
                {
                    var vm = resolver.Resolve<TowerBreakers.UI.Equipment.EquipmentViewModel>();
                    cachedEquipmentView.Initialize(vm);
                });
            }

            // HUDView 등록 및 ViewModel 연결 (Part 6-1)
            if (m_hudView != null)
            {
                builder.RegisterComponent(m_hudView);
                var cachedHudView = m_hudView;
                builder.RegisterBuildCallback(resolver =>
                {
                    var vm = resolver.Resolve<HUDViewModel>();
                    cachedHudView.Initialize(vm);
                    Debug.Log("[GameLifetimeScope] HUDView 초기화 완료");
                });
            }
            else
            {
                Debug.LogWarning("[GameLifetimeScope] HUDView가 인스펙터에 설정되지 않았습니다.");
            }

            // ── Player States 등록 ──
            builder.Register<PlayerIdleState>(Lifetime.Singleton);
            builder.Register<PlayerAttackState>(Lifetime.Singleton);
            builder.Register<PlayerLeapState>(Lifetime.Singleton);
            builder.Register<PlayerDefendState>(Lifetime.Singleton);
            builder.Register<PlayerSkillState>(Lifetime.Singleton);

            // ── Game States 등록 ──
            builder.Register<LoadingState>(Lifetime.Singleton);
            builder.Register<PlayingState>(Lifetime.Singleton);

            // ── Entry Point ──
            builder.RegisterEntryPoint<GameController>();
        }

        #region 내부 메서드
        /// <summary>
        /// [설명]: 씬 컴포넌트를 방어적으로 등록합니다. null이면 경고 또는 에러를 출력합니다.
        /// </summary>
        /// <param name="builder">컨테이너 빌더</param>
        /// <param name="component">등록할 컴포넌트</param>
        /// <param name="name">컴포넌트 이름 (로그용)</param>
        /// <param name="isRequired">필수 컴포넌트 여부</param>
        private void RegisterSceneComponent<T>(IContainerBuilder builder, T component, string name, bool isRequired = false) where T : MonoBehaviour
        {
            if (component != null)
            {
                builder.RegisterComponent(component);
            }
            else if (isRequired)
            {
                Debug.LogError($"[GameLifetimeScope] {name}이(가) 설정되지 않았습니다! 관련 시스템이 동작하지 않습니다.");
            }
            else
            {
                Debug.LogWarning($"[GameLifetimeScope] {name}이(가) 설정되지 않았습니다.");
            }
        }
        #endregion
    }
}
