    using System;
    using System.Collections.Generic;
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
    using TowerBreakers.UI.View;
    using TowerBreakers.UI.ViewModel;
    using TowerBreakers.UI.DTO;
    using TowerBreakers.Player.Logic;
    using TowerBreakers.Player.View;
    using TowerBreakers.Player.DTO;
    using Cysharp.Threading.Tasks;
    using TowerBreakers.Core.DI;
    using TowerBreakers.Enemy.DTO;
    using TowerBreakers.Battle;
    using TowerBreakers.Enemy.Service;
    using TowerBreakers.Player.Data;
    using TowerBreakers.Core.Service;
    using TowerBreakers.Core.Events;

    namespace TowerBreakers.Core.Battle
    {
        /// <summary>
        /// [기능]: 인게임 전투 흐름 및 캐릭터 상태 제어기
        /// </summary>
        public class GameController : IStartable, ITickable, IDisposable
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
        private readonly IEventBus m_eventBus;
        
        private PlayerPushReceiver m_playerPushReceiver;
        
        private readonly PlayerView m_playerView;
        private readonly BattleUIView m_battleUIView;
        private readonly PlayerLogic m_playerLogic;
        private readonly BattleUIViewModel m_uiViewModel;
        private readonly PlayerConfigDTO m_playerConfig;
        private readonly BattleUIDTO m_uiConfig;
        private readonly EnemyConfigDTO m_enemyConfig;
        private readonly RewardChestView m_rewardChestPrefab;
        private readonly EquipmentDatabase m_equipmentDatabase;
        private readonly Transform m_rewardChestSpawnPoint;
        private readonly CombatSystem m_combatSystem;
        private readonly Transform m_parryReference; // [추가]
        private readonly IEffectService m_effectService; // [추가: 이펙트 서비스]
        #endregion

        #region 유니티 생명주기
        /// <summary>
        /// [설명]: VContainer의 ITickable 인터페이스 구현체입니다. 
        /// MonoBehaviour의 Update와 같이 매 프레임 실행됩니다.
        /// </summary>
        public void Tick()
        {
            if (m_enemyDetectionService != null && m_enemySpawnService != null)
            {
                // [멀티 스레드 탐지용]: 현재 활성화된 적 군집 리스트 전달
                m_enemyDetectionService.UpdateEnemyLists(
                    m_enemySpawnService.NormalEnemies,
                    m_enemySpawnService.EliteEnemies,
                    m_enemySpawnService.BossEnemies
                );
            }
        }
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
            PlayerSpawnService playerSpawnService,
            PlayerView playerView,
            BattleUIView battleUIView,
            PlayerLogic playerLogic,
            BattleUIViewModel uiViewModel,
            PlayerConfigDTO playerConfig,
            BattleUIDTO uiConfig,
            EnemyConfigDTO enemyConfig,
            EquipmentDatabase equipmentDatabase,
            IEnemyDetectionService enemyDetectionService,
            CombatSystem combatSystem,
            IEventBus eventBus, // [추가]
            RewardChestView rewardChestPrefab = null,
            Transform rewardChestSpawnPoint = null,
            Transform parryReference = null,
            IEffectService effectService = null)
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
            
            m_playerView = playerView;
            m_battleUIView = battleUIView;
            m_playerLogic = playerLogic;
            m_uiViewModel = uiViewModel;
            m_playerConfig = playerConfig;
            m_uiConfig = uiConfig;
            m_enemyConfig = enemyConfig;
            m_equipmentDatabase = equipmentDatabase;
            m_enemyDetectionService = enemyDetectionService;
            m_combatSystem = combatSystem;
            m_eventBus = eventBus; // [추가]
            m_rewardChestPrefab = rewardChestPrefab;
            m_rewardChestSpawnPoint = rewardChestSpawnPoint;
            m_parryReference = parryReference;
            m_effectService = effectService;
        }

        private readonly IEnemyDetectionService m_enemyDetectionService; // [추가]

        /// <summary>
        /// [설명]: 씬 시작 시 호출되어 초기화 로직을 수행합니다.
        /// </summary>
        public async void Start()
        {
            InitializeSystems();
            InitializePlatform();
            
            await SetupFirstFloorAsync();
            
            if (m_playerSpawnService != null)
            {
                m_playerSpawnService.SetPlayerTransform(m_playerView != null ? m_playerView.transform : null);
                m_playerSpawnService.Initialize(m_playerLogic);
                m_playerSpawnService.OnSpawnComplete += OnPlayerSpawnComplete;
                m_playerSpawnService.PlaySpawnAnimation();
            }
            else
            {
                OnPlayerSpawnComplete();
            }
            
            // 전투 루프 시작
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
                
                // [수정]: OnPlatformReady 이벤트가 이미 적 생성을 트리거하므로 명시적인 호출을 제거하여 중복 스폰 방지
            }
        }

        private void OnPlayerSpawnComplete()
        {
            DelayedEnemyAdvance().Forget();
        }

        private async Cysharp.Threading.Tasks.UniTask DelayedEnemyAdvance()
        {
            await Cysharp.Threading.Tasks.UniTask.Delay(1000);
            StartEnemyAdvanceForCurrentFloor();
        }

        private void StartEnemyAdvanceForCurrentFloor()
        {
            Transform currentPlatform = m_floorTransitionService?.GetCurrentPlatformTransform();
            if (currentPlatform == null) return;

            StartAdvanceForList(m_enemySpawnService.NormalEnemies, currentPlatform);
            StartAdvanceForList(m_enemySpawnService.EliteEnemies, currentPlatform);
            StartAdvanceForList(m_enemySpawnService.BossEnemies, currentPlatform);
        }

        private void StartAdvanceForList(IReadOnlyList<GameObject> enemies, Transform platform)
        {
            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                
                // [핵심 수정]: 해당 적이 현재 플랫폼(층)에 속해 있는지 확인하여, 선스폰된 다른 층의 적이 움직이는 것을 방지
                if (enemy.transform.parent == platform)
                {
                    var pushController = enemy.GetComponent<EnemyPushController>();
                    if (pushController != null)
                    {
                        pushController.SetCanAdvance(true);
                        pushController.StartMoving();
                    }
                }
            }
        }

        private void StopAdvanceForList(IReadOnlyList<GameObject> enemies, Transform platform)
        {
            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                if (enemy.transform.parent == platform)
                {
                    var pushController = enemy.GetComponent<EnemyPushController>();
                    if (pushController != null)
                    {
                        pushController.StopMoving();
                    }
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
            InitializeUI();
            SubscribeToEvents();
        }

        private void InitializeUI()
        {
            if (m_battleUIView != null)
            {
                m_battleUIView.Initialize(m_uiViewModel, m_uiConfig);
            }
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
            if (m_enemySpawnService != null)
            {
                m_enemySpawnService.ClearEnemies();
            }
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
            if (m_playerView != null && m_playerPushReceiver == null)
            {
                m_playerPushReceiver = m_playerView.GetComponent<PlayerPushReceiver>();
            }

            if (m_playerPushReceiver != null && m_playerStatService != null)
            {
                // [설명]: DTO와 로직, 그리고 전투 시스템을 주입하여 초기화
                m_playerPushReceiver.Initialize(m_playerStatService.TotalHealth, m_playerConfig, m_playerLogic, m_combatSystem);
                m_playerPushReceiver.OnPlayerDeath += OnPlayerDeath;
            }
        }

        private void OnPlayerDeath()
        {
            EndBattle(false);
        }

        private void InitializePlayerAttack()
        {
            if (m_playerView != null)
            {
                m_playerView.Initialize(m_playerLogic, m_uiViewModel, m_playerStatService, m_equipmentService, m_parryReference, m_effectService);
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

            if (m_eventBus != null)
            {
                m_eventBus.Subscribe<OnParryPerformed>(HandleParryPerformed);
                m_eventBus.Subscribe<OnWallCrushOccurred>(HandleWallCrushOccurred);
            }
        }

        private void UnsubscribeToEvents()
        {
            if (m_towerFloorService != null)
            {
                m_towerFloorService.OnAllEnemiesCleared -= OnAllEnemiesCleared;
                m_towerFloorService.OnTowerCompleted -= OnTowerCompleted;
            }

            if (m_floorTransitionService != null)
            {
                m_floorTransitionService.OnPlatformReady -= OnPlatformReady;
            }

            if (m_eventBus != null)
            {
                m_eventBus.Unsubscribe<OnParryPerformed>(HandleParryPerformed);
                m_eventBus.Unsubscribe<OnWallCrushOccurred>(HandleWallCrushOccurred);
            }
        }

        public void Dispose()
        {
            UnsubscribeToEvents();
        }

        private void HandleParryPerformed(OnParryPerformed evt)
        {
            // [규칙]: 패링 성공 시 적들이 다시 진격을 시작함
            Debug.Log("[GameController] 패링 성공 - 적 진격 재개");
            StartEnemyAdvanceForCurrentFloor();
        }

        private void HandleWallCrushOccurred(OnWallCrushOccurred evt)
        {
            // [규칙]: 압착 데미지 발생 시 적들이 진격을 멈춤
            Debug.Log("[GameController] 압착 데미지 발생 - 적 진격 일시 정지");
            StopEnemyAdvanceForCurrentFloor();
        }

        private void StopEnemyAdvanceForCurrentFloor()
        {
            Transform currentPlatform = m_floorTransitionService?.GetCurrentPlatformTransform();
            if (currentPlatform == null) return;

            StopAdvanceForList(m_enemySpawnService.NormalEnemies, currentPlatform);
            StopAdvanceForList(m_enemySpawnService.EliteEnemies, currentPlatform);
            StopAdvanceForList(m_enemySpawnService.BossEnemies, currentPlatform);
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
                // [수정]: 여기서 즉시 활성화하지 않고, 적들이 전진을 시작할 때 활성화하도록 변경
                
                // [수정]: 동기 메서드에서 비동기 호출 시 적절한 처리
                SpawnEnemiesForCurrentFloor(currentFloor).Forget();
            }
        }

        private async UniTaskVoid SpawnEnemiesForCurrentFloor(FloorData floor)
        {
            Transform platformTransform = null;
            if (m_floorTransitionService != null)
            {
                platformTransform = m_floorTransitionService.GetCurrentPlatformTransform();
            }
            
            // [추가]: UI용 카운트 정보 설정
            if (m_towerFloorService != null)
            {
                m_towerFloorService.SetupFloorEnemies(floor);
            }

            // [핵심 리팩토링]: 자식 개수(childCount)는 환경 오브젝트 등에 의해 부정확할 수 있으므로, 서비스 내부의 스폰 상태를 확인합니다.
            bool alreadySpawned = m_enemySpawnService.IsFloorSpawned(floor.FloorNumber);

            if (!alreadySpawned)
            {
                Debug.Log($"[GameController] 현재 층({floor.FloorNumber}) 적이 없어 새로 스폰합니다.");
                await m_enemySpawnService.SpawnEnemies(floor, OnEnemyDeath, platformTransform, true);
            }
            else
            {
                Debug.Log($"[GameController] 현재 층({floor.FloorNumber})에 이미 선스폰된 적이 존재하여 콜백만 연결합니다.");
                // 이미 스폰되어 있다면 사망 콜백만 연결
                m_enemySpawnService.SetOnEnemyDeathCallback(OnEnemyDeath);
            }

            await SpawnNextFloorEnemies();
        }

        private async UniTask SpawnNextFloorEnemies()
        {
            if (m_floorTransitionService == null || m_towerFloorService == null) return;

            int currentFloor = m_towerFloorService.CurrentFloor;

            // 1. n+1 층 적 선스폰
            int nextFloor1 = currentFloor + 1;
            if (nextFloor1 <= m_towerFloorService.TotalFloors)
            {
                var floorData = m_towerFloorService.GetFloorData(nextFloor1);
                var platform = m_floorTransitionService.GetNextPlatformTransform();
                
                if (floorData != null && platform != null)
                {
                    if (!m_enemySpawnService.IsFloorSpawned(nextFloor1))
                    {
                        await m_enemySpawnService.SpawnEnemies(floorData, OnEnemyDeath, platform, false);
                    }
                }
            }

            // 2. n+2 층 적 선스폰
            int nextFloor2 = currentFloor + 2;
            if (nextFloor2 <= m_towerFloorService.TotalFloors)
            {
                var floorData = m_towerFloorService.GetFloorData(nextFloor2);
                var platform = m_floorTransitionService.GetThirdPlatformTransform();
                
                if (floorData != null && platform != null)
                {
                    if (!m_enemySpawnService.IsFloorSpawned(nextFloor2))
                    {
                        await m_enemySpawnService.SpawnEnemies(floorData, OnEnemyDeath, platform, false);
                    }
                }
            }
        }

        private void OnEnemyDeath(GameObject enemy)
        {
            // [리팩토링]: TowerFloorService가 OnEnemyKilled 이벤트를 직접 구독하므로 여기서의 수동 호출은 제거함
        }

        private void OnAllEnemiesCleared()
        {
            Debug.Log("[GameController] 모든 적 처치 완료 - 보상 상자 스폰 대기");
            SpawnRewardChest();
        }

        private void SpawnRewardChest()
        {
            if (m_rewardChestPrefab == null)
            {
                Debug.LogWarning("[GameController] 보상 상자 프리팹이 설정되지 않았습니다. 즉시 보상 처리합니다.");
                CompleteFloorSequence();
                return;
            }

            // [추가]: 상자를 생성하기 전에 보상을 미리 결정함 (연출용 아이콘 확보)
            var selectedItem = GetRandomSpumReward();
            if (selectedItem == null)
            {
                Debug.LogWarning("[GameController] 보상 리스트가 비어있어 상자를 스폰하지 않고 다음 층으로 넘어갑니다. (EquipmentDatabase 확인 필요)");
                CompleteFloorSequence();
                return;
            }

            Transform platformTransform = m_floorTransitionService != null ? m_floorTransitionService.GetCurrentPlatformTransform() : null;
            
            Vector3 spawnPos;
            if (m_rewardChestSpawnPoint != null)
            {
                spawnPos = m_rewardChestSpawnPoint.position;
                Debug.Log($"[GameController] 지정된 SpawnPoint 사용: {spawnPos}");
            }
            else
            {
                float spawnX = m_enemyConfig != null ? m_enemyConfig.RewardChestSpawnX : -2f;
                float spawnY = m_enemyConfig != null ? m_enemyConfig.SpawnYOffset : 0f;
                spawnPos = new Vector3(spawnX, spawnY, 0);
                if (platformTransform != null) spawnPos += platformTransform.position;
                Debug.Log($"[GameController] 기본 좌표 계산 사용: {spawnPos} (Platform: {platformTransform?.name})");
            }

            // [수정]: 스폰 좌표의 Z축을 0으로 강제하여 카메라 클리핑 및 가려짐 방지
            spawnPos.z = 0;

            var chest = GameObject.Instantiate(m_rewardChestPrefab, spawnPos, Quaternion.identity, platformTransform);
            chest.gameObject.SetActive(true);
            
            // [추가]: 보상 상자를 적 탐지 서비스에 등록하여 플레이어가 대시 대상으로 인식하게 함
            m_enemyDetectionService?.SetRewardChest(chest.gameObject);
            
            // [수정]: 보상 아이콘을 전달하며 초기화 (실제 지급은 개봉 콜백에서 수행)
            chest.Initialize(selectedItem.Icon, () =>
            {
                Debug.Log($"[GameController] 상자 개봉 확인 - 아이템 지급: {selectedItem.ItemName}");
                SubmitReward(selectedItem);
                m_enemyDetectionService?.SetRewardChest(null);
                CompleteFloorSequence();
            });

            Debug.Log($"[GameController] 보상 상자 스폰 완료: {spawnPos}, 보상 대상: {selectedItem.ItemName}");
        }

        private async void CompleteFloorSequence()
        {
            // 1. 고정 보상 처리
            var reward = m_towerFloorService.GetCurrentFloorReward();
            if (reward != null)
            {
                ProcessFloorReward(reward);
            }

            // [참고]: SPUM 장비 보상은 이제 상자 개봉 콜백에서 직접 처리됨 (SubmitReward 호출됨)

            if (m_towerFloorService.IsLastFloor())
            {
                EndBattle(true);
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

        private EquipmentData GetRandomSpumReward()
        {
            if (m_equipmentDatabase == null) return null;

            var candidates = new List<EquipmentData>();
            candidates.AddRange(m_equipmentDatabase.Weapons);
            candidates.AddRange(m_equipmentDatabase.Armors);
            candidates.AddRange(m_equipmentDatabase.Helmets);

            if (candidates.Count == 0) return null;

            int randomIndex = UnityEngine.Random.Range(0, candidates.Count);
            return candidates[randomIndex];
        }

        private void SubmitReward(EquipmentData selectedItem)
        {
            if (selectedItem != null)
            {
                m_userSession.AddItem(selectedItem.ID);
                m_uiViewModel.ShowRewardMessage($"보상 획득: {selectedItem.ItemName}");
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
