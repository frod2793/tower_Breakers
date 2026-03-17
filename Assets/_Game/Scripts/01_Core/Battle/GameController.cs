using UnityEngine;
using VContainer;
using VContainer.Unity;
using TowerBreakers.SPUM;
using TowerBreakers.Player.Model;
using TowerBreakers.Player.Service;
using TowerBreakers.Player.Stat;
using TowerBreakers.Player.Controller;
using TowerBreakers.Tower.Data;
using TowerBreakers.Tower.Service;
using TowerBreakers.Core.Scene;
using Cysharp.Threading.Tasks;

namespace TowerBreakers.Core.Battle
{
    /// <summary>
    /// [기능]: 인게임 전투 흐름 및 캐릭터 상태 제어기
    /// </summary>
    public class GameController : IStartable
    {
        #region 내부 변수
        private readonly CustomSPUMManager m_characterManager;
        private readonly UserSessionModel m_userSession;
        private readonly IEquipmentService m_equipmentService;
        private readonly IPlayerStatService m_playerStatService;
        private readonly BattleResultService m_battleResultService;
        private readonly TowerFloorService m_towerFloorService;
        private readonly EnemySpawnService m_enemySpawnService;
        private readonly FloorTransitionService m_floorTransitionService;
        private readonly PlatformPool m_platformPool;
        private readonly PlayerSpawnService m_playerSpawnService;
        
        [SerializeField] private PlayerAttackController m_playerAttackController;
        [SerializeField] private PlayerPushReceiver m_playerPushReceiver;
        #endregion

        #region 초기화
        [Inject]
        public GameController(
            CustomSPUMManager characterManager,
            UserSessionModel userSession,
            IEquipmentService equipmentService,
            IPlayerStatService playerStatService,
            BattleResultService battleResultService,
            TowerFloorService towerFloorService,
            TowerData towerData,
            EnemySpawnService enemySpawnService,
            FloorTransitionService floorTransitionService,
            PlatformPool platformPool,
            PlayerSpawnService playerSpawnService)
        {
            m_characterManager = characterManager;
            m_userSession = userSession;
            m_equipmentService = equipmentService;
            m_playerStatService = playerStatService;
            m_battleResultService = battleResultService;
            m_towerFloorService = towerFloorService;
            m_towerData = towerData;
            m_enemySpawnService = enemySpawnService;
            m_floorTransitionService = floorTransitionService;
            m_platformPool = platformPool;
            m_playerSpawnService = playerSpawnService;
        }

        /// <summary>
        /// [설명]: 씬 시작 시 호출되어 초기화 로직을 수행합니다.
        /// </summary>
        public async void Start()
        {
            InitializeSystems();
            InitializePlatform();
            
            Debug.Log("[3/5] 첫 층 설정 시작...");
            await SetupFirstFloorAsync();
            Debug.Log("[4/5] 적 스폰 완료 - 플레이어 대시 대기 중");
            
            if (m_playerSpawnService != null)
            {
                m_playerSpawnService.SetPlayerTransform(m_playerAttackController != null ? m_playerAttackController.transform : null);
                m_playerSpawnService.OnSpawnComplete += OnPlayerSpawnComplete;
                m_playerSpawnService.PlaySpawnAnimation();
            }
            else
            {
                OnPlayerSpawnComplete();
            }
            
            Debug.Log("[5/5] 인게임 전투 루프 시작");
        }

        private async UniTask SetupFirstFloorAsync()
        {
            var currentFloor = m_towerFloorService.GetCurrentFloorData();
            if (currentFloor != null)
            {
                if (m_floorTransitionService != null)
                {
                    m_floorTransitionService.SetCurrentPlatform(currentFloor.FloorNumber);
                    
                    if (currentFloor.FloorNumber == 1)
                    {
                        await m_floorTransitionService.ActivateFirstFloorPlatformAsync();
                    }
                    else
                    {
                        await m_floorTransitionService.PlayTransitionAsync();
                    }
                }
                
                await SpawnEnemiesForCurrentFloorAsync(currentFloor);
            }
        }

        private async UniTask SpawnEnemiesForCurrentFloorAsync(FloorData floor)
        {
            Transform platformTransform = null;
            if (m_floorTransitionService != null)
            {
                platformTransform = m_floorTransitionService.GetCurrentPlatformTransform();
            }
            
            await m_enemySpawnService.SpawnEnemies(floor, OnEnemyDeath, platformTransform);
            m_towerFloorService.SetEnemyCount(floor.GetTotalEnemyCount());
            
            SpawnNextFloorEnemies();
        }

        private void OnPlayerSpawnComplete()
        {
            StartEnemyAdvanceForCurrentFloor();
        }

        private void StartEnemyAdvanceForCurrentFloor()
        {
            var enemies = GameObject.FindGameObjectsWithTag("Enemy");
            
            foreach (var enemy in enemies)
            {
                var pushController = enemy.GetComponent<EnemyPushController>();
                if (pushController != null)
                {
                    pushController.StartMoving();
                }
            }
        }
        #endregion

        #region 내부 로직
        private void InitializeSystems()
        {
            InitializeTower();
            InitializeEnemySpawn();
            InitializeCharacter();
            InitializePlayerStats();
            InitializePlayerPush();
            InitializePlayerAttack();
            SubscribeToEvents();
        }

        private void InitializeTower()
        {
            if (m_towerFloorService != null && m_towerData != null)
            {
                m_towerFloorService.Initialize(m_towerData);
            }
        }

        private TowerData m_towerData;

        private void InitializeEnemySpawn()
        {
        }

        private void InitializeCharacter()
        {
            if (m_characterManager != null)
            {
                m_characterManager.Initialize(m_userSession, m_equipmentService);
            }
        }

        private void InitializePlayerStats()
        {
            if (m_playerStatService != null)
            {
                m_playerStatService.ApplyEquipmentStats();
            }
        }

        private void InitializePlayerPush()
        {
            if (m_playerPushReceiver != null && m_playerStatService != null)
            {
                m_playerPushReceiver.Initialize(m_playerStatService.TotalHealth);
                m_playerPushReceiver.OnPlayerDeath += OnPlayerDeath;
            }
        }

        private void OnPlayerDeath()
        {
            EndBattle(false);
        }

        private void InitializePlayerAttack()
        {
            if (m_playerAttackController != null && m_playerStatService != null)
            {
                m_playerAttackController.Initialize(m_playerStatService);
            }
        }

        private void InitializePlatform()
        {
            if (m_floorTransitionService != null && m_platformPool != null)
            {
                m_floorTransitionService.SetPlatformPool(m_platformPool);
            }
        }

        private void SubscribeToEvents()
        {
            if (m_towerFloorService != null)
            {
                m_towerFloorService.OnAllEnemiesCleared += OnAllEnemiesCleared;
                m_towerFloorService.OnTowerCompleted += OnTowerCompleted;
            }

            if (m_floorTransitionService != null)
            {
                m_floorTransitionService.OnPlatformReady += OnPlatformReady;
            }
        }

        private void StartFirstFloor()
        {
            var currentFloor = m_towerFloorService.GetCurrentFloorData();
            if (currentFloor != null)
            {
                if (m_floorTransitionService != null)
                {
                    m_floorTransitionService.SetCurrentPlatform(currentFloor.FloorNumber);
                }
            }
        }

        private void OnPlatformReady(int floorNumber)
        {
            var currentFloor = m_towerFloorService.GetCurrentFloorData();
            if (currentFloor != null && currentFloor.FloorNumber == floorNumber)
            {
                SpawnEnemiesForCurrentFloor(currentFloor);
            }
        }

        private async void SpawnEnemiesForCurrentFloor(FloorData floor)
        {
            Transform platformTransform = null;
            if (m_floorTransitionService != null)
            {
                platformTransform = m_floorTransitionService.GetCurrentPlatformTransform();
            }
            
            await m_enemySpawnService.SpawnEnemies(floor, OnEnemyDeath, platformTransform);
            m_towerFloorService.SetEnemyCount(floor.GetTotalEnemyCount());

            SpawnNextFloorEnemies();
        }

        private async void SpawnNextFloorEnemies()
        {
            if (m_floorTransitionService != null && m_towerFloorService != null)
            {
                var nextFloorNumber = m_towerFloorService.CurrentFloor + 1;
                if (nextFloorNumber <= m_towerFloorService.TotalFloors)
                {
                    var nextFloor = m_towerFloorService.GetFloorData(nextFloorNumber);
                    if (nextFloor != null)
                    {
                        var nextPlatformTransform = m_floorTransitionService.GetNextPlatformTransform();
                        // [설명]: 다음 층의 적들을 미리 생성할 때도 해당 플랫폼을 부모로 지정합니다.
                        // UniTask를 리턴하게 변경되었으므로 await를 사용하여 생성이 완료될 때까지 대기할 수 있습니다.
                        await m_enemySpawnService.SpawnEnemies(nextFloor, null, nextPlatformTransform);
                    }
                }
            }
        }

        private void OnEnemyDeath(GameObject enemy)
        {
            m_towerFloorService.RegisterEnemyDeath();
        }

        private async void OnAllEnemiesCleared()
        {
            var reward = m_towerFloorService.GetCurrentFloorReward();
            if (reward != null)
            {
                ProcessFloorReward(reward);
            }

            if (m_towerFloorService.IsLastFloor())
            {
                return;
            }

            m_towerFloorService.MoveToNextFloor();

            var nextFloor = m_towerFloorService.GetCurrentFloorData();
            if (nextFloor != null && m_floorTransitionService != null)
            {
                m_floorTransitionService.SetCurrentPlatform(nextFloor.FloorNumber);

                await m_floorTransitionService.PlayTransitionAsync();
            }
        }

        private void OnTowerCompleted()
        {
            EndBattle(true);
        }

        private void ProcessFloorReward(FloorRewardData reward)
        {
            if (reward.Gold > 0)
            {
                m_userSession.Gold += reward.Gold;
            }

            if (reward.ItemIds != null && reward.ItemIds.Count > 0)
            {
                foreach (var itemId in reward.ItemIds)
                {
                    m_userSession.AddItem(itemId);
                }
            }
        }

        /// <summary>
        /// [설명]: 전투 종료 시 호출하여 결과를 처리합니다. (외부 UI 또는 트리거에서 호출)
        /// </summary>
        /// <param name="isVictory">승리 여부</param>
        public void EndBattle(bool isVictory)
        {
            var currentFloor = m_towerFloorService.CurrentFloor;
            var context = isVictory 
                ? m_battleResultService.CreateVictoryContext(currentFloor, 100, 50, null) 
                : m_battleResultService.CreateDefeatContext(currentFloor, 10, 5);

            m_battleResultService.ProcessBattleResult(context);
        }
        #endregion
    }
}
