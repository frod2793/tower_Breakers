using TowerBreakers.Player.Data.SO;
using TowerBreakers.Player.Data.Models;
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
using TowerBreakers.Interactions.Logic;
using TowerBreakers.Sound.Data;
using TowerBreakers.Sound.Logic;
using TowerBreakers.Sound.View;
using TowerBreakers.DevTools;
using TowerBreakers.Player.Data;
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

        [SerializeField]
        private PlayerDebugger m_playerDebugger;

        [Header("씬 컴포넌트 참조")]
        [SerializeField, Tooltip("플레이어 뷰 (씬에 배치 필수)")]
        private PlayerView m_playerView;

        [SerializeField, Tooltip("플레이어 밀림 수신자")]
        private PlayerPushReceiver m_playerPushReceiver;

        [SerializeField, Tooltip("환경 매니저")]
        private EnvironmentManager m_environmentManager;

        [SerializeField, Tooltip("전투 연출 프리젠터")]
        private CombatEffectPresenter m_combatEffectPresenter;

        [SerializeField, Tooltip("플레이어 이펙트 뷰 (하트/타격 연출 등)")]
        private PlayerEffectView m_playerEffectView;

        [SerializeField, Tooltip("장비 UI 뷰")]
        private TowerBreakers.UI.Equipment.EquipmentView m_equipmentView;

        [SerializeField, Tooltip("발사체 팩토리")]
        private PlayerProjectileFactory m_projectileFactory;

        [SerializeField, Tooltip("이펙트 매니저")]
        private TowerBreakers.Effects.EffectManager m_effectManager;

        [SerializeField, Tooltip("적 사망 연출 매니저")]
        private TowerBreakers.Enemy.View.EnemyDeathEffect m_enemyDeathEffect;

        [SerializeField, Tooltip("HUD UI 뷰")]
        private HUDView m_hudView;

        [SerializeField, Tooltip("게임 오버 UI 뷰")]
        private GameOverView m_gameOverView;

        [SerializeField, Tooltip("인게임 메뉴(일시정지) 뷰")]
        private InGameMenuView m_inGameMenuView;

        [SerializeField, Tooltip("층 전환 연출 프리젠터")]
        private TowerBreakers.Tower.View.TowerTransitionPresenter m_towerTransitionPresenter;

        [Header("씬 초기화")]
        [SerializeField, Tooltip("게임 씬 초기화 컴포넌트")]
        private GameSceneInitializer m_initializer;

        [Header("데미지 텍스트 설정")]
        [SerializeField, Tooltip("데미지 텍스트 프리팹 (DamageTextView)")]
        private DamageTextView m_damageTextPrefab;

        [SerializeField, Tooltip("데미지 텍스트가 생성될 부모 트랜스폼 (World Space Canvas 등)")]
        private Transform m_damageTextParent;

        [Header("보물상자 시스템 설정")]
        [SerializeField, Tooltip("기본 보상 테이블")]
        private RewardTableData m_rewardTable;

        [Header("사운드 시스템")]
        [SerializeField, Tooltip("사운드 데이터베이스 (ScriptableObject)")]
        private SoundDatabase m_soundDatabase;

        [SerializeField, Tooltip("사운드 플레이어 (씬에 배치 필수)")]
        private SoundPlayer m_soundPlayer;

        [Header("영속성 및 데이터베이스")]
        [SerializeField, Tooltip("장비 데이터베이스 (ID 변환용)")]
        private TowerBreakers.Player.Data.EquipmentDatabase m_equipmentDatabase;

        [Header("디버그 도구")]
        [SerializeField, Tooltip("아이템 치트 뷰")]
        private ItemCheatView m_itemCheatView;
        #endregion

        protected override void Configure(IContainerBuilder builder)
        {
            CoreDIModule.Register(builder);
            TowerDIModule.Register(builder, m_towerData, m_equipmentDatabase);
            PlayerDIModule.Register(builder, m_playerData, m_playerView, m_playerPushReceiver, m_playerDebugger, m_projectileFactory);
            EnemyDIModule.Register(builder, m_enemyDeathEffect);
            CombatDIModule.Register(builder, m_combatEffectPresenter, m_effectManager);
            EnvironmentDIModule.Register(builder, m_environmentManager);
            UIDIModule.Register(builder, m_hudView, m_gameOverView, m_equipmentView, m_inGameMenuView, m_damageTextPrefab, m_damageTextParent, m_playerEffectView, m_towerTransitionPresenter);
            RewardDIModule.Register(builder, m_rewardTable);
            SoundDIModule.Register(builder, m_soundDatabase, m_soundPlayer);

            if (m_initializer != null)
            {
                builder.RegisterComponent(m_initializer);
            }

            builder.Register<ItemCheatModel>(Lifetime.Singleton);
            builder.Register<ItemCheatViewModel>(Lifetime.Singleton);

            if (m_itemCheatView != null)
            {
                builder.RegisterComponent(m_itemCheatView);
                builder.RegisterBuildCallback(resolver =>
                {
                    var viewModel = resolver.Resolve<ItemCheatViewModel>();
                    m_itemCheatView.SetViewModel(viewModel);
                    UnityEngine.Debug.Log("[GameLifetimeScope] ItemCheatView 초기화 완료");
                });
            }
        }
    }
}
