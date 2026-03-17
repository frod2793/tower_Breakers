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

        [Header("장비")]
        [SerializeField, Tooltip("장비 데이터베이스")]
        private EquipmentDatabase m_equipmentDatabase;

        [Header("적")]
        [SerializeField, Tooltip("적 프리팹")]
        private GameObject m_enemyPrefab;

        [SerializeField, Tooltip("적 스폰 포인트")]
        private Transform[] m_spawnPoints;

        [SerializeField, Tooltip("적 스폰 Y 오프셋")]
        private float m_spawnYOffset;

        [Header("트랜지션")]
        [SerializeField, Tooltip("층 이동 트랜지션 시간")]
        private float m_transitionDuration = 2f;

        [SerializeField, Tooltip("플랫폼 풀")]
        private PlatformPool m_platformPool;

        [Header("플레이어/카메라")]
        [SerializeField, Tooltip("플레이어 트랜스폼")]
        private Transform m_playerTransform;

        [Header("플레이어 스폰")]
        [SerializeField, Tooltip("플레이어 스폰 포인트")]
        private Transform m_playerSpawnPoint;

        [SerializeField, Tooltip("플레이어 도착 포인트")]
        private Transform m_playerArrivalPoint;

        [SerializeField, Tooltip("플레이어 스폰 서비스")]
        private PlayerSpawnService m_playerSpawnService;

        [SerializeField, Tooltip("카메라 트랜스폼")]
        private Transform m_cameraTransform;

        [SerializeField, Tooltip("타겟 플랜폼")]
        private Transform m_targetPlatform;

        [SerializeField, Tooltip("GO 이미지")]
        private UnityEngine.UI.Image m_goImage;

        [Header("리팩토링 컴포넌트")]
        [SerializeField] private PlayerConfigDTO m_playerConfig;
        [SerializeField] private BattleUIDTO m_battleUIConfig;
        [SerializeField] private BattleUIView m_battleUIView;
        [SerializeField] private PlayerView m_playerView;
        #endregion

        #region 초기화 및 바인딩 로직
        protected override void Configure(IContainerBuilder builder)
        {
            RegisterData(builder);
            RegisterServices(builder);
            RegisterControllers(builder);
        }

        private void RegisterData(IContainerBuilder builder)
        {
            if (m_playerStatsData != null)
            {
                builder.RegisterInstance(m_playerStatsData);
            }

            if (m_towerData != null)
            {
                builder.RegisterInstance(m_towerData);
            }

            if (m_equipmentDatabase != null)
            {
                builder.RegisterInstance(m_equipmentDatabase);
            }

            builder.Register<UserSessionModel>(Lifetime.Singleton);
            builder.Register<IEquipmentService, EquipmentService>(Lifetime.Singleton);
        }

        private void RegisterServices(IContainerBuilder builder)
        {
            builder.Register<SceneTransitionService>(Lifetime.Scoped);
            builder.Register<BattleResultService>(Lifetime.Scoped);

            builder.Register<TowerFloorService>(Lifetime.Scoped);

            builder.Register<FloorTransitionService>(Lifetime.Scoped)
                .WithParameter(m_playerTransform)
                .WithParameter(m_cameraTransform)
                .WithParameter(m_goImage);

            builder.Register<IPlayerStatService, PlayerStatService>(Lifetime.Scoped);

            if (m_spawnPoints == null || m_spawnPoints.Length == 0)
            {
                Debug.LogError($"[BattleLifetimeScope] {gameObject.name}의 m_spawnPoints가 인스펙터에서 할당되지 않았습니다!");
            }

            // [수정]: EnemySpawnService 생성자에 필요한 Transform(parentTransform)을 m_targetPlatform으로 주입
            builder.Register<EnemySpawnService>(Lifetime.Scoped)
                .WithParameter(m_enemyPrefab)
                .WithParameter(m_spawnPoints)
                .WithParameter(m_targetPlatform)
                .WithParameter(m_spawnYOffset);
        }

        private void RegisterControllers(IContainerBuilder builder)
        {
            if (m_customCharacterManager != null)
            {
                builder.RegisterComponent(m_customCharacterManager);
            }

            if (m_playerSpawnService != null)
            {
                m_playerSpawnService.SetReferences(m_playerSpawnPoint, m_playerArrivalPoint, m_playerTransform);
                builder.RegisterComponent(m_playerSpawnService);
            }

            if (m_platformPool != null)
            {
                builder.RegisterComponent(m_platformPool);
            }

            // 리팩토링된 클래스 등록
            builder.RegisterInstance(m_playerConfig ?? new PlayerConfigDTO());
            builder.RegisterInstance(m_battleUIConfig ?? new BattleUIDTO());
            builder.Register<BattleUIViewModel>(Lifetime.Scoped);
            builder.Register<PlayerLogic>(Lifetime.Scoped);

            if (m_battleUIView != null) builder.RegisterComponent(m_battleUIView);
            if (m_playerView != null) builder.RegisterComponent(m_playerView);

            builder.RegisterEntryPoint<GameController>();
        }
        #endregion
    }
}
