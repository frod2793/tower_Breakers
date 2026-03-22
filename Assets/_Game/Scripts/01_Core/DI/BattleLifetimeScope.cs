using EasyTransition;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using TowerBreakers.SPUM;
using TowerBreakers.Core.Scene;
using TowerBreakers.Core.Battle;
using TowerBreakers.Tower.Data;
using TowerBreakers.Tower.Service;
using TowerBreakers.Enemy.Service;
using TowerBreakers.Player.Data;
using TowerBreakers.Player.Model;
using TowerBreakers.Player.Service;
using TowerBreakers.Player.Stat;
using TowerBreakers.UI.View;
using TowerBreakers.UI.ViewModel;
using TowerBreakers.UI.DTO;
using TowerBreakers.Player.DTO;
using TowerBreakers.Player.Logic;
using TowerBreakers.Player.View;
using TowerBreakers.Enemy.DTO;
using TowerBreakers.Battle;
using TowerBreakers.Core.Service;

namespace TowerBreakers.Core.DI
{
    /// <summary>
    /// [기능]: 인게임 전투 씬 전용 의존성 주입 컨테이너
    /// </summary>
    public class BattleLifetimeScope : LifetimeScope
    {
        #region 에디터 설정
        [Header("SPUM")]
        [SerializeField, Tooltip("커스텀 SPUM 캐릭터 관리자")]
        private CustomSPUMManager m_customCharacterManager;

        [SerializeField, Tooltip("SPUM 프리팹 (스프라이트 로드용)")]
        private SPUM_Prefabs m_spumPrefabs;

        [Header("타워 데이터")]
        [SerializeField, Tooltip("타워 데이터")]
        private TowerData m_towerData;

        [SerializeField, Tooltip("플레이어 기본 스탯")]
        private PlayerStatsData m_playerStatsData;

        #region 데이터베이스
        [Header("장비")]
        [SerializeField, Tooltip("장비 데이터베이스")]
        private EquipmentDatabase m_equipmentDatabase;
        #endregion

        #region 적 설정
        [Header("적 스폰")]
        [SerializeField, Tooltip("적 스폰 포인트")]
        private Transform[] m_spawnPoints;

        [Header("전투 설정 DTO")]
        [SerializeField, Tooltip("적 전체 설정 (이동, 밀기 등)")]
        private EnemyConfigDTO m_enemyConfig = new EnemyConfigDTO();

        [SerializeField, Tooltip("플레이어 설정 (밀림 저항, 벽 데미지 등)")]
        private PlayerConfigDTO m_playerConfig = new PlayerConfigDTO();

        [Header("보상 상자")]
        [SerializeField, Tooltip("보상 상자 프리팹")]
        private RewardChestView m_rewardChestPrefab;

        [SerializeField, Tooltip("보상 상자 스폰 위치")]
        private Transform m_rewardChestSpawnPoint;
        #endregion

        #region 환경 및 트랜지션
        [Header("트랜지션/플랫폼")]
        [SerializeField, Tooltip("층 이동 트랜지션 시간")]
        private float m_transitionDuration = 2f;

        [SerializeField, Tooltip("플랫폼 풀")]
        private PlatformPool m_platformPool;

        [SerializeField, Tooltip("타겟 플랜폼")]
        private Transform m_targetPlatform;

        [SerializeField, Tooltip("GO 이미지")]
        private UnityEngine.UI.Image m_goImage;

        [SerializeField, Tooltip("씬 전환 트랜지션 설정")]
        private TransitionSettings m_transitionSettings;

        [Header("이펙트")]
        [SerializeField, Tooltip("이펙트 매니저")]
        private EffectManager m_effectManager;
        #endregion

        #region 플레이어/카메라 참조
        [Header("플레이어 참조")]
        [SerializeField, Tooltip("플레이어 트랜스폼")]
        private Transform m_playerTransform;

        [SerializeField, Tooltip("플레이어 뷰 컴포넌트")]
        private PlayerView m_playerView;

        [Header("플레이어 스폰")]
        [SerializeField, Tooltip("플레이어 스폰 포인트")]
        private Transform m_playerSpawnPoint;

        [SerializeField, Tooltip("플레이어 도착 포인트")]
        private Transform m_playerArrivalPoint;

        [SerializeField, Tooltip("플레이어 퇴장 포인트 (대시 목표)")]
        private Transform m_playerExitPoint;

        [SerializeField, Tooltip("플레이어 스폰 서비스")]
        private PlayerSpawnService m_playerSpawnService;

        [Header("카메라")]
        [SerializeField, Tooltip("카메라 트랜스폼")]
        private Transform m_cameraTransform;

        [SerializeField, Tooltip("패링 백덤블링 기준점")]
        private Transform m_parryReference;
        #endregion

        #region UI
        [Header("UI 참조")]
        [SerializeField, Tooltip("전투 UI 설정")]
        private BattleUIDTO m_battleUIConfig;

        [SerializeField, Tooltip("전투 UI 뷰")]
        private BattleUIView m_battleUIView;

        [SerializeField, Tooltip("일시 정지 UI 뷰")]
        private TowerBreakers.UI.View.PauseUIView m_pauseUIView;

        
        #endregion
        #endregion

        #region 유니티 생명주기
        protected override void Configure(IContainerBuilder builder)
        {
            // [설명]: 데이터/DTO 등록
            RegisterData(builder);
            // [설명]: 비즈니스 로직 서비스 등록
            RegisterServices(builder);
            // [설명]: 컨트롤러 및 뷰 등록
            RegisterControllers(builder);
        }
        #endregion

        #region 초기화 및 바인딩 로직
        /// <summary>
        /// [설명]: 순수 데이터 및 설정 DTO를 컨테이너에 등록합니다.
        /// 인스펙터의 '플레이어 직접 설정' 섹션에 값이 입력된 경우, DTO의 기본값을 오버라이드합니다.
        /// </summary>
        private void RegisterData(IContainerBuilder builder)
        {
            builder.RegisterInstance(m_towerData);
            builder.RegisterInstance(m_playerStatsData);
            builder.RegisterInstance(m_equipmentDatabase);

            // [핵심]: 인스펙터에서 설정한 DTO 인스턴스를 직접 등록하여 값을 유지함
            if (m_playerConfig != null)
            {
                // 플레이어 설정 등록
            }
            
            builder.RegisterInstance(m_enemyConfig);
            builder.RegisterInstance(m_playerConfig);
            builder.RegisterInstance(m_battleUIConfig ?? new BattleUIDTO());
            
            // [추가]: PlayerLogic에 필요한 실시간 상태 객체 등록
            builder.Register<PlayerStateDTO>(Lifetime.Scoped);
            
            if (m_transitionSettings != null)
            {
                builder.RegisterInstance(m_transitionSettings);
            }

            if (m_rewardChestPrefab != null)
            {
                builder.RegisterComponentInNewPrefab(m_rewardChestPrefab, Lifetime.Scoped);
            }
        }

        /// <summary>
        /// [설명]: 인게임 비즈니스 로직(POCO 서비스)을 등록합니다.
        /// </summary>
        private void RegisterServices(IContainerBuilder builder)
        {
            builder.Register<SceneTransitionService>(Lifetime.Scoped);
            builder.Register<BattleResultService>(Lifetime.Scoped);
            builder.Register<TowerFloorService>(Lifetime.Scoped);
            
            // [추가]: 전투 시스템 등록 (IInitializable 기반 자동 초기화)
            builder.Register<CombatSystem>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();

            // [추가]: 적 팩토리 및 풀링 시스템 등록
            builder.Register<EnemyFactory>(Lifetime.Scoped);

            builder.Register<FloorTransitionService>(Lifetime.Scoped)
                .WithParameter("playerTransform", m_playerTransform)
                .WithParameter("cameraTransform", m_cameraTransform)
                .WithParameter("exitTransform", m_playerExitPoint)
                .WithParameter("transitionDuration", m_transitionDuration);

            builder.Register<IPlayerStatService, PlayerStatService>(Lifetime.Scoped);

            // EnemySpawnService 등록 (IEnemyProvider 인터페이스로도 노출)
            builder.Register<EnemySpawnService>(Lifetime.Scoped)
                .As<EnemySpawnService>()
                .As<IEnemyProvider>()
                .WithParameter(m_spawnPoints)
                .WithParameter(m_targetPlatform);

            // 적 탐지 서비스 등록
            builder.Register<IEnemyDetectionService, EnemyDetectionService>(Lifetime.Scoped);

            // 이펙트 서비스 등록 (MonoBehaviour 기반)
            if (m_effectManager != null)
            {
                builder.RegisterComponent(m_effectManager).As<IEffectService>();
            }
        }

        /// <summary>
        /// [설명]: 씬 내의 MonoBehaviour 및 진입점 컨트롤러를 등록합니다.
        /// </summary>
        private void RegisterControllers(IContainerBuilder builder)
        {
            if (m_customCharacterManager != null)
            {
                builder.RegisterComponent(m_customCharacterManager);
            }

            if (m_playerSpawnService != null)
            {
                // [개선]: 수동 설정 대신 DI 주입(builder.Inject)을 사용하도록 등록
                m_playerSpawnService.SetReferences(m_playerSpawnPoint, m_playerArrivalPoint, m_playerTransform);
                builder.RegisterComponent(m_playerSpawnService);
            }

            if (m_platformPool != null)
            {
                builder.RegisterComponent(m_platformPool);
            }

            // 핵심 로직 POCO
            builder.Register<BattleUIViewModel>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
            builder.Register<PauseUIViewModel>(Lifetime.Scoped);
            builder.Register<PlayerLogic>(Lifetime.Scoped);

            // View 컴포넌트
            if (m_battleUIView != null) builder.RegisterComponent(m_battleUIView);
            if (m_pauseUIView != null) builder.RegisterComponent(m_pauseUIView);
            if (m_playerView != null) builder.RegisterComponent(m_playerView);

            // 게임 전체 흐름 컨트롤러
            builder.RegisterEntryPoint<GameController>()
                .WithParameter("rewardChestSpawnPoint", m_rewardChestSpawnPoint)
                .WithParameter("parryReference", m_parryReference);
        }
        #endregion

        #region 에디터 지원
        private void OnDrawGizmosSelected()
        {
            if (m_playerConfig == null) return;

            Gizmos.color = new Color(1f, 0f, 0f, 0.5f); // 반투명 빨간색
            float x = m_playerConfig.LeftWallX;
            Gizmos.DrawLine(new Vector3(x, -10f, 0), new Vector3(x, 10f, 0));
        }
        #endregion
    }
}
