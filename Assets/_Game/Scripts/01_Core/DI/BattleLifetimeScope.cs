using UnityEngine;
using VContainer;
using VContainer.Unity;
using TowerBreakers.SPUM;
using TowerBreakers.Core.Scene;
using TowerBreakers.Core.Battle;
using TowerBreakers.Tower.Data;
using TowerBreakers.Tower.Service;
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
        [Header("적 프리팹 및 스폰")]
        [SerializeField, Tooltip("적 프리팹")]
        private GameObject m_enemyPrefab;

        [SerializeField, Tooltip("적 스폰 포인트")]
        private Transform[] m_spawnPoints;

        [Header("전투 설정 DTO")]
        [SerializeField, Tooltip("적 전체 설정 (이동, 밀기 등)")]
        private EnemyConfigDTO m_enemyConfig = new EnemyConfigDTO();

        [SerializeField, Tooltip("플레이어 설정 (밀림 저항, 벽 데미지 등)")]
        private PlayerConfigDTO m_playerConfig = new PlayerConfigDTO();
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

        [SerializeField, Tooltip("플레이어 스폰 서비스")]
        private PlayerSpawnService m_playerSpawnService;

        [Header("카메라")]
        [SerializeField, Tooltip("카메라 트랜스폼")]
        private Transform m_cameraTransform;
        #endregion

        #region UI
        [Header("UI 참조")]
        [SerializeField, Tooltip("전투 UI 설정")]
        private BattleUIDTO m_battleUIConfig;

        [SerializeField, Tooltip("전투 UI 뷰")]
        private BattleUIView m_battleUIView;
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
        /// </summary>
        private void RegisterData(IContainerBuilder builder)
        {
            builder.RegisterInstance(m_towerData);
            builder.RegisterInstance(m_playerStatsData);
            builder.RegisterInstance(m_equipmentDatabase);

            // DTO 등록 (생성자 주입용)
            builder.RegisterInstance(m_enemyConfig);
            builder.RegisterInstance(m_playerConfig);
            builder.RegisterInstance(m_battleUIConfig ?? new BattleUIDTO());

            builder.Register<UserSessionModel>(Lifetime.Singleton);
            builder.Register<IEquipmentService, EquipmentService>(Lifetime.Singleton);
        }

        /// <summary>
        /// [설명]: 인게임 비즈니스 로직(POCO 서비스)을 등록합니다.
        /// </summary>
        private void RegisterServices(IContainerBuilder builder)
        {
            builder.Register<SceneTransitionService>(Lifetime.Scoped);
            builder.Register<BattleResultService>(Lifetime.Scoped);
            builder.Register<TowerFloorService>(Lifetime.Scoped);

            builder.Register<FloorTransitionService>(Lifetime.Scoped)
                .WithParameter("playerTransform", m_playerTransform)
                .WithParameter("cameraTransform", m_cameraTransform)
                .WithParameter("goImage", m_goImage)
                .WithParameter("transitionDuration", m_transitionDuration); // [개선]: 파라미터 이름을 명시하여 모호성 제거

            builder.Register<IPlayerStatService, PlayerStatService>(Lifetime.Scoped);

            // EnemySpawnService 등록 (DTO 자동 주입)
            builder.Register<EnemySpawnService>(Lifetime.Scoped)
                .WithParameter(m_enemyPrefab)
                .WithParameter(m_spawnPoints)
                .WithParameter(m_targetPlatform);
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
            builder.Register<BattleUIViewModel>(Lifetime.Scoped);
            builder.Register<PlayerLogic>(Lifetime.Scoped);

            // View 컴포넌트
            if (m_battleUIView != null) builder.RegisterComponent(m_battleUIView);
            if (m_playerView != null) builder.RegisterComponent(m_playerView);

            // 게임 전체 흐름 컨트롤러
            builder.RegisterEntryPoint<GameController>();
        }
        #endregion

        #region 에디터 지원
        private void OnDrawGizmosSelected()
        {
            if (m_playerConfig == null) return;

            Gizmos.color = Color.red;
            float x = m_playerConfig.LeftWallX;
            Gizmos.DrawLine(new Vector3(x, -10f, 0), new Vector3(x, 10f, 0));
        }
        #endregion
    }
}
