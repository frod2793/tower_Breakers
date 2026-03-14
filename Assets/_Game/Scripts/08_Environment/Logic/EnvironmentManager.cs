using System.Collections.Generic;
using TowerBreakers.Core.Events;
using TowerBreakers.Environment.View;
using TowerBreakers.Tower.Logic;
using TowerBreakers.Interactions.View;
using TowerBreakers.Interactions.ViewModel;
using TowerBreakers.Player.Data.SO;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace TowerBreakers.Environment.Logic
{
    /// <summary>
    /// [설명]: 층 진행에 따라 맵 세그먼트를 동적으로 생성하고 관리하는 환경 시스템입니다.
    /// 플레이어와 적의 스폰 위치 및 맵 경계를 관리합니다.
    /// </summary>
    public class EnvironmentManager : MonoBehaviour
    {
        #region 에디터 설정

        [Header("프리팹 및 부모 설정")] [SerializeField, Tooltip("사용할 맵 세그먼트 프리팹(기본 틀)")]
        private MapSegment m_segmentPrefab;

        [SerializeField, Tooltip("초기 생성 세그먼트 개수")]
        private int m_initialSegmentCount = 3;

        [SerializeField, Tooltip("생성된 세그먼트들이 배치될 부모 오브젝트")]
        private Transform m_segmentParent;

        [SerializeField, Tooltip("스폰된 적들이 배치될 부모 오브젝트")]
        private Transform m_enemyParent;

        [Header("보상 상자 설정")] [SerializeField, Tooltip("스폰할 보상 상자 프리팹")]
        private RewardChestView m_rewardChestPrefab;

        // 층별 보상 상자 스폰 오프셋(게임 플레이 중마다 다를 수 있는 경우 사용)
        #region 내부 로직 — FloorRewardSpawnConfig
        [System.Serializable]
        public class FloorRewardSpawnConfig
        {
            public int FloorIndex;
            public float OffsetX;
            public float OffsetY;
            public bool Enabled = true;
        }
        #endregion

        // Public API: Floor-specific reward spawn offset (runtime configurability)
        /// <summary>
        /// [설명]: 특정 층의 보상 상자 스폰 오프셋을 설정합니다. Inspector에서 관리하기 어려운 경우 런타임에 업데이트 가능합니다.
        /// </summary>
        public void SetRewardSpawnOffsetForFloor(int floorIndex, Vector2 offset)
        {
            if (m_floorRewardSpawnConfigs == null) m_floorRewardSpawnConfigs = new List<FloorRewardSpawnConfig>();
            var cfg = m_floorRewardSpawnConfigs.Find(c => c != null && c.FloorIndex == floorIndex);
            if (cfg != null)
            {
                cfg.OffsetX = offset.x;
                cfg.OffsetY = offset.y;
                cfg.Enabled = true;
            }
            else
            {
                m_floorRewardSpawnConfigs.Add(new FloorRewardSpawnConfig
                {
                    FloorIndex = floorIndex,
                    OffsetX = offset.x,
                    OffsetY = offset.y,
                    Enabled = true
                });
            }
        }

        [SerializeField, Tooltip("층별 보상 상자 스폰 오프셋 구성 (비어 있으면 기본 오프셋 사용)")]
        private List<FloorRewardSpawnConfig> m_floorRewardSpawnConfigs = new List<FloorRewardSpawnConfig>();

        [Header("좌표 설정")] [SerializeField, Tooltip("플레이어의 기본 착지(전투 시) Y 좌표 (화면 중심 기준)")]
        private float m_defaultLandingY = -1.3f;

        [SerializeField, Tooltip("플레이어 대시 시작 X 오프셋")]
        private float m_playerSpawnOffsetX = -12.0f;

        [SerializeField, Tooltip("플레이어 착지(전투 시작) X 오프셋")]
        private float m_playerLandingOffsetX = 2.0f;

        [SerializeField, Tooltip("백플립 기점 X 오프셋 (세그먼트 원점 기준)")]
        private float m_backflipThresholdOffsetX = 3.76f;

        [SerializeField, Tooltip("적 스폰 X 오프셋")]
        private float m_enemySpawnOffsetX = 9.0f;

        [Header("보상 상자 오프셋")] [SerializeField, Tooltip("보상 상자의 기본 X 오프셋 (세그먼트 원점 기준)")]
        private float m_rewardChestOffsetX = 0f;

        [SerializeField, Tooltip("보상 상자의 기본 Y 오프셋 (세그먼트 원점 기준)")]
        private float m_rewardChestOffsetY = 0f;

        [Header("크라켄 소환 설정")]
        [SerializeField, Tooltip("소환할 촉수 프리팹 (장애물)")]
        private GameObject m_tentaclePrefab;
        [SerializeField, Tooltip("낙하 촉수 프리팹 (공격)")]
        private GameObject m_fallingTentaclePrefab;
        [SerializeField, Tooltip("강타 촉수 프리팹 (공격)")]
        private GameObject m_strikeTentaclePrefab;

        #endregion

        // 공개 API: 보상 상자 층별 offset 설정은 아래 영역으로 이동했습니다.

        #region 내부 필드

        private IEventBus m_eventBus;
        private Effects.EffectManager m_effectManager;
        private TowerManager m_towerManager;
        private Player.Logic.PlayerPushReceiver m_playerReceiver;
        private Enemy.Logic.EnemySpawner m_enemySpawner;
        private IObjectResolver m_resolver;

        private readonly List<MapSegment> m_activeSegments = new List<MapSegment>();
        private readonly Dictionary<int, MapSegment> m_floorToSegmentMap = new Dictionary<int, MapSegment>();
        private float m_currentOffset = 0f;
        private int m_spawnedSegmentCount = 0;

        #endregion

        #region 프로퍼티

        /// <summary>
        /// [설명]: 현재 사용 중인 세그먼트 프리팹의 높이를 반환합니다.
        /// </summary>
        public float DefaultSegmentHeight => m_segmentPrefab != null ? m_segmentPrefab.SegmentHeight : 15.0f;

        /// <summary>
        /// [설명]: 스폰된 적들이 배치될 부모 오브젝트를 반환합니다.
        /// </summary>
        public Transform EnemyParent => m_enemyParent;

        /// <summary>
        /// [설명]: 플레이어의 기본 착지 Y 좌표를 반환합니다.
        /// </summary>
        public float DefaultLandingY => m_defaultLandingY;

        #endregion

        #region 초기화 및 바인딩 로직

        /// <summary>
        /// [설명]: 의존성을 주입받고 이벤트를 구독합니다.
        /// </summary>
        [Inject]
        public void Construct(
            IEventBus eventBus,
            Effects.EffectManager effectManager,
            TowerManager towerManager,
            Player.Logic.PlayerPushReceiver playerReceiver,
            Enemy.Logic.EnemySpawner enemySpawner,
            IObjectResolver resolver)
        {
            m_eventBus = eventBus;
            m_effectManager = effectManager;
            m_towerManager = towerManager;
            m_playerReceiver = playerReceiver;
            m_enemySpawner = enemySpawner;
            m_resolver = resolver;

            if (m_enemySpawner != null)
            {
                m_enemySpawner.SetEnemyParent(m_enemyParent);
            }

            m_eventBus.Subscribe<OnFloorCleared>(OnFloorCleared);
            m_eventBus.Subscribe<OnKrakenSummonRequested>(HandleKrakenSummonRequested);

            InitializeMap();
        }

        private void InitializeMap()
        {
            m_currentOffset = 0f;
            m_spawnedSegmentCount = 0;
            m_activeSegments.Clear();
            m_floorToSegmentMap.Clear();

            for (int i = 0; i < m_initialSegmentCount; i++)
            {
                CreateNextSegment();
            }

            UpdatePlayerBoundary();
        }

        #endregion

        #region 비즈니스 로직

        /// <summary>
        /// [설명]: 층 클리어 이벤트 핸들러입니다. 새로운 세그먼트를 생성하고 이전 세그먼트를 정리합니다.
        /// </summary>
        private void OnFloorCleared(OnFloorCleared evt)
        {
            // [수정]: 이미 해당 층의 세그먼트가 존재하면 중복 생성 방지
            if (!m_floorToSegmentMap.ContainsKey(m_spawnedSegmentCount))
            {
                CreateNextSegment();
            }

            UpdatePlayerBoundary();

            // 메모리 최적화: 시야에서 완전히 사라진 과거 세그먼트 삭제
            int oldestFloorToKeep = evt.FloorIndex - m_initialSegmentCount;
            while (m_activeSegments.Count > m_initialSegmentCount + 2 && m_activeSegments.Count > 0)
            {
                var oldSegment = m_activeSegments[0];
                m_activeSegments.RemoveAt(0);

                // Dictionary에서도 해당 세그먼트 제거
                int floorToRemove = -1;
                foreach (var kvp in m_floorToSegmentMap)
                {
                    if (kvp.Value == oldSegment)
                    {
                        floorToRemove = kvp.Key;
                        break;
                    }
                }
                if (floorToRemove >= 0)
                {
                    m_floorToSegmentMap.Remove(floorToRemove);
                }

                if (oldSegment != null)
                {
                    Destroy(oldSegment.gameObject);
                }

                break; // 한 번에 하나만 제거
            }
        }

        /// <summary>
        /// [설명]: 크라켄 소환 이벤트 핸들러입니다.
        /// </summary>
        private void HandleKrakenSummonRequested(OnKrakenSummonRequested evt)
        {
            UnityEngine.Debug.Log($"[KRAKEN_DIAGNOSTIC] 3. 이벤트 수신: OnKrakenSummonRequested (Type={evt.Type}, Floor={evt.FloorIndex})");

            switch (evt.Type)
            {
                case OnKrakenSummonRequested.SummonType.Tentacle:
                    SpawnTentacle(evt.FloorIndex, evt.Position); // Original call had position, keeping it for now as SpawnTentacle body is not provided.
                    break;
                case OnKrakenSummonRequested.SummonType.FallingTentacle:
                case OnKrakenSummonRequested.SummonType.StrikeTentacle:
                    SpawnAttackTentacle(evt.Type, evt.FloorIndex, evt.Position);
                    break;
            }
        }

        private void SpawnTentacle(int floorIndex, Vector3 position)
        {
            if (m_tentaclePrefab == null)
            {
                UnityEngine.Debug.LogError("[KRAKEN_DIAGNOSTIC] 에러: 'Tentacle' 프리팹이 EnvironmentManager에 할당되지 않았습니다!");
                return;
            }

            var segment = GetSegmentForFloor(floorIndex);
            if (segment == null)
            {
                UnityEngine.Debug.LogError($"[KRAKEN_DIAGNOSTIC] 에러: {floorIndex}층의 FloorSegment를 찾을 수 없습니다.");
                return;
            }

            UnityEngine.Debug.Log($"[KRAKEN_DIAGNOSTIC] 4. 촉수 인스턴스화 시작: {m_tentaclePrefab.name}, 부모={segment.name}, 타겟 위치={position}");
            GameObject tentacle = Instantiate(m_tentaclePrefab, segment.transform);
            tentacle.transform.localPosition = position;
            UnityEngine.Debug.Log($"[KRAKEN_DIAGNOSTIC] 5. 인스턴스화 완료: 이름={tentacle.name}, 최종 LocalPos={tentacle.transform.localPosition}");

            // [추가]: 촉수 컨트롤러 초기화 (보스로부터의 명령 수신용)
            if (!tentacle.TryGetComponent<TowerBreakers.Enemy.Logic.KrakenTentacleController>(out var controller))
            {
                UnityEngine.Debug.LogError($"[KRAKEN_DIAGNOSTIC] 에러: '{tentacle.name}'에 KrakenTentacleController가 없습니다!");
                return;
            }
            UnityEngine.Debug.Log($"[KRAKEN_DIAGNOSTIC] 6. 컨트롤러 초기화 시도 (Mode=Obstacle)");
            controller.Initialize(floorIndex, m_eventBus, m_effectManager, TowerBreakers.Enemy.Logic.KrakenTentacleController.ControllerMode.Obstacle);

            // 크라켄 소환물 이벤트 컴포넌트 초기화
            if (tentacle.TryGetComponent(out Enemy.Boss.KrakenSummonEvents summonEvents))
            {
                summonEvents.Initialize(OnKrakenSummonRequested.SummonType.Tentacle, floorIndex, m_eventBus);
            }

            UnityEngine.Debug.Log($"[KRAKEN_DIAGNOSTIC] 7. 촉수 소환 완료: 층={floorIndex}, 위치={position}");
        }

        private void SpawnAttackTentacle(OnKrakenSummonRequested.SummonType type, int floorIndex, Vector3 position)
        {
            GameObject prefab = (type == OnKrakenSummonRequested.SummonType.FallingTentacle) ? m_fallingTentaclePrefab : m_strikeTentaclePrefab;
            
            if (prefab == null)
            {
                return;
            }

            var segment = GetSegmentForFloor(floorIndex);
            if (segment == null) return;

            GameObject attackTentacle = Instantiate(prefab, segment.transform);
            
            // 월드 좌표를 직접 설정하여 플레이어의 X 위치를 정확히 맞춤
            attackTentacle.transform.position = position;

            if (!attackTentacle.TryGetComponent<TowerBreakers.Enemy.Logic.KrakenTentacleController>(out var controller))
            {
                controller = attackTentacle.AddComponent<TowerBreakers.Enemy.Logic.KrakenTentacleController>();
            }

            var mode = type == OnKrakenSummonRequested.SummonType.FallingTentacle 
                ? TowerBreakers.Enemy.Logic.KrakenTentacleController.ControllerMode.FallingAttack 
                : TowerBreakers.Enemy.Logic.KrakenTentacleController.ControllerMode.StrikeAttack;

            controller.Initialize(floorIndex, m_eventBus, m_effectManager, mode);

            controller.StartAttack(position);

            global::UnityEngine.Debug.Log($"[EnvironmentManager] 공격 촉수 소환 및 공격 시작: 타입={type}, 층={floorIndex}, 타겟 위치={position}");
        }


        /// <summary>
        /// [설명]: 현재 층에 맞춰 플레이어의 월드 경계(좌측 벽, 백플립 지점)를 갱신합니다.
        /// </summary>
        private void UpdatePlayerBoundary()
        {
            if (m_playerReceiver == null || m_towerManager == null) return;

            var currentSegment = GetSegmentForFloor(m_towerManager.CurrentFloorIndex);
            if (currentSegment != null)
            {
                m_playerReceiver.SetMapLimit(currentSegment.LeftBoundaryX);

                // 세그먼트 위치를 기준으로 백플립 기준점 X 좌표 계산
                float backflipWorldX = currentSegment.transform.position.x + m_backflipThresholdOffsetX;
                m_playerReceiver.SetBackflipThreshold(backflipWorldX);
            }
        }

        /// <summary>
        /// [설명]: 다음 세그먼트를 생성하고 수직 위치를 설정합니다.
        /// 세그먼트는 층 인덱스와 1:1로 매핑됩니다.
        /// </summary>
        private void CreateNextSegment()
        {
            if (m_segmentPrefab == null) return;

            Transform parent = (m_segmentParent != null) ? m_segmentParent : transform;
            MapSegment newSegment = Instantiate(m_segmentPrefab, parent);

            newSegment.SetPosition(new Vector2(0f, m_currentOffset));
            m_activeSegments.Add(newSegment);

            // [수정]: 층 인덱스와 세그먼트를 명시적으로 매핑
            int floorIndex = m_spawnedSegmentCount;
            m_floorToSegmentMap[floorIndex] = newSegment;

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[EnvironmentManager] 세그먼트 생성: 층={floorIndex}, Y={m_currentOffset}");
            #endif

            m_currentOffset += newSegment.SegmentHeight;
            m_spawnedSegmentCount++;
        }

        #endregion

        #region 공개 API

        /// <summary>
        /// [설명]: 특정 층의 플레이어 스폰(대시 시작) 위치를 반환합니다.
        /// </summary>
        public Vector2 GetPlayerSpawnPosition(int floorIndex)
        {
            var segment = GetSegmentForFloor(floorIndex);
            if (segment == null) return new Vector2(m_playerSpawnOffsetX, m_defaultLandingY);

            Vector3 segPos = segment.transform.position;
            return new Vector2(segPos.x + m_playerSpawnOffsetX, segPos.y + m_defaultLandingY);
        }

        /// <summary>
        /// [설명]: 특정 층의 플레이어 착지(전투 시작) 위치를 반환합니다.
        /// </summary>
        public Vector2 GetPlayerLandingPosition(int floorIndex)
        {
            var segment = GetSegmentForFloor(floorIndex);
            if (segment == null) return new Vector2(m_playerLandingOffsetX, m_defaultLandingY);

            Vector3 segPos = segment.transform.position;
            return new Vector2(segPos.x + m_playerLandingOffsetX, segPos.y + m_defaultLandingY);
        }

        /// <summary>
        /// [설명]: 특정 층의 세그먼트 Transform을 반환합니다.
        /// </summary>
        public Transform GetSegmentTransform(int floorIndex)
        {
            var segment = GetSegmentForFloor(floorIndex);
            return (segment != null) ? segment.transform : null;
        }

        /// <summary>
        /// [설명]: 특정 층의 적 스폰 위치를 반환합니다.
        /// </summary>
        public Vector2 GetSpawnPosition(int floorIndex)
        {
            var segment = GetSegmentForFloor(floorIndex);
            if (segment == null) return new Vector2(m_enemySpawnOffsetX, m_defaultLandingY);

            Vector3 segPos = segment.transform.position;
            return new Vector2(segPos.x + m_enemySpawnOffsetX, segPos.y + m_defaultLandingY);
        }

        #endregion

        #region 공개 API (보상 상자)

        /// <summary>
        /// [설명]: 특정 층에 보상 상자를 스폰합니다.
        /// </summary>
        /// <param name="floorIndex">스폰할 층 인덱스</param>
        /// <param name="rewardTable">사용할 보상 테이블</param>
        public void SpawnRewardChest(int floorIndex, RewardTableData rewardTable)
        {
            if (m_rewardChestPrefab == null)
            {
                global::UnityEngine.Debug.LogWarning("[EnvironmentManager] 보상 상자 프리팹이 설정되지 않았습니다.");
                return;
            }

            if (rewardTable == null)
            {
                global::UnityEngine.Debug.LogWarning("[EnvironmentManager] 보상 테이블이 설정되지 않았습니다. 층 " + floorIndex + "에 보상 상자를 스폰하지 않습니다.");
                return;
            }

            // 세그먼트 위치 가져오기
            var segment = GetSegmentForFloor(floorIndex);
            if (segment == null)
            {
                global::UnityEngine.Debug.LogWarning("[EnvironmentManager] 층 " + floorIndex + "에 해당하는 세그먼트를 찾을 수 없습니다.");
                return;
            }

            // 보상 상자 스폰 위치 계산 (세그먼트 기준)
            Vector2 perFloorOffset = GetRewardSpawnOffsetForFloor(floorIndex);
            Vector3 spawnPosition = new Vector3(
                segment.transform.position.x + perFloorOffset.x,
                segment.transform.position.y + perFloorOffset.y,
                0f
            );

            // 보상 상자 인스턴스 생성 (DI 컨테이너를 통해 개별 ViewModel 주입)
            RewardChestView chestInstance =
                m_resolver.Instantiate(m_rewardChestPrefab, spawnPosition, Quaternion.identity, segment.transform);
            chestInstance.name = $"RewardChest_Floor{floorIndex}";

            // 보상 테이블 및 기본 정보 설정
            chestInstance.SetRewardTable(rewardTable);
            chestInstance.Setup(floorIndex);

            // 개발 단계 확인용 로그 (필요 시 유지)
            // Debug.Log($"[EnvironmentManager] 층 {floorIndex} 보상 상자 생성");
        }

        #endregion

        #region 내부 로직

        /// <summary>
        /// [설명]: 층 인덱스에 해당하는 세그먼트를 Dictionary에서 직접 조회합니다.
        /// 매핑이 존재하지 않으면 필요한 세그먼트를 자동 생성합니다.
        /// </summary>
        /// <param name="floorIndex">조회할 층 인덱스</param>
        /// <returns>해당 층의 MapSegment (null 가능)</returns>
        private MapSegment GetSegmentForFloor(int floorIndex)
        {
            // [수정]: Dictionary 기반 직접 조회로 산술 매핑 오류 방지
            if (m_floorToSegmentMap.TryGetValue(floorIndex, out var segment))
            {
                return segment;
            }

            // 세그먼트가 아직 생성되지 않은 경우 자동 생성
            while (m_spawnedSegmentCount <= floorIndex)
            {
                CreateNextSegment();
            }

            if (m_floorToSegmentMap.TryGetValue(floorIndex, out segment))
            {
                return segment;
            }

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"[EnvironmentManager] 층 {floorIndex}에 해당하는 세그먼트를 찾을 수 없습니다.");
            #endif
            return null;
        }

        /// <summary>
        /// [설명]: 층별 커스텀 보상 상자 스폰 오프셋을 반환합니다. 설정이 없으면 기본 오프셋을 사용합니다.
        /// </summary>
        private Vector2 GetRewardSpawnOffsetForFloor(int floorIndex)
        {
            if (m_floorRewardSpawnConfigs == null) return new Vector2(m_rewardChestOffsetX, m_rewardChestOffsetY);

            foreach (var cfg in m_floorRewardSpawnConfigs)
            {
                if (cfg != null && cfg.Enabled && cfg.FloorIndex == floorIndex)
                {
                    return new Vector2(cfg.OffsetX, cfg.OffsetY);
                }
            }

            return new Vector2(m_rewardChestOffsetX, m_rewardChestOffsetY);
        }

        #endregion

        #region 유니티 생명주기

        private void OnDestroy()
        {
            if (m_eventBus != null)
            {
                m_eventBus.Unsubscribe<OnFloorCleared>(OnFloorCleared);
            }
        }

        #endregion
    }
}
