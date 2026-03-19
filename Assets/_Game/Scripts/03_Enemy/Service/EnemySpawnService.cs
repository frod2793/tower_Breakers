using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using TowerBreakers.Tower.Data;
using TowerBreakers.Enemy.DTO;

namespace TowerBreakers.Tower.Service
{
    /// <summary>
    /// [기능]: 적 생성 서비스
    /// </summary>
    public class EnemySpawnService : IEnemyProvider
    {
        #region 내부 필드
        private readonly Transform[] m_spawnPoints;
        private readonly GameObject m_enemyPrefab;
        private readonly EnemyConfigDTO m_config;
        private Transform m_parentTransform;
        
        private readonly List<GameObject> m_normalEnemies = new List<GameObject>();
        private readonly List<EnemyPushController> m_normalPushControllers = new List<EnemyPushController>();
        private readonly List<GameObject> m_eliteEnemies = new List<GameObject>();
        private readonly List<GameObject> m_bossEnemies = new List<GameObject>();
        
        // [추가]: 스폰된 층을 추적하여 중복 생성을 방지함
        private readonly HashSet<int> m_spawnedFloors = new HashSet<int>();
        private Action<GameObject> m_onEnemyDeath;
        #endregion

        #region 프로퍼티 (IEnemyProvider)
        public IReadOnlyList<GameObject> NormalEnemies => m_normalEnemies;
        public IReadOnlyList<GameObject> EliteEnemies => m_eliteEnemies;
        public IReadOnlyList<GameObject> BossEnemies => m_bossEnemies;

        /// <summary>
        /// [설명]: 특정 층이 이미 스폰되었는지 확인합니다.
        /// </summary>
        public bool IsFloorSpawned(int floorNumber) => m_spawnedFloors.Contains(floorNumber);
        #endregion

        #region 초기화
        /// <summary>
        /// [설명]: 적 생성 서비스를 초기화합니다.
        /// BattleLifetimeScope를 통해 의존성을 주입받습니다.
        /// </summary>
        public EnemySpawnService(
            GameObject enemyPrefab, 
            Transform[] spawnPoints, 
            Transform parentTransform, 
            EnemyConfigDTO config)
        {
            m_enemyPrefab = enemyPrefab;
            m_spawnPoints = spawnPoints;
            m_parentTransform = parentTransform;
            m_config = config ?? new EnemyConfigDTO();
        }
        #endregion

        #region 공개 API
        /// <summary>
        /// [설명]: 적이 생성될 부모 트랜스폼을 설정합니다. 
        /// 전투 중 층(플랫폼) 이동 시 호출되어 스폰 위치의 기준점을 변경합니다.
        /// </summary>
        /// <param name="parent">새로운 부모 트랜스폼</param>
        public void SetParent(Transform parent)
        {
            m_parentTransform = parent;
        }

        /// <summary>
        /// [설명]: 사망 시 콜백을 설정합니다.
        /// </summary>
        public void SetOnEnemyDeathCallback(Action<GameObject> callback)
        {
            m_onEnemyDeath = callback;
        }

        /// <summary>
        /// [설명]: 지정된 층 데이터를 기반으로 적을 스폰합니다.
        /// </summary>
        /// <param name="clearExisting">기존에 스폰된 적들을 모두 제거할지 여부</param>
        public async UniTask SpawnEnemies(FloorData floor, Action<GameObject> onEnemyDeath, Transform parentTransform = null, bool clearExisting = true)
        {
            if (floor == null || floor.Enemies == null) return;

            // [추가]: 이미 스폰된 층이라면 중복 생성을 방지함 (선스폰 로직과의 충돌 해결)
            if (!clearExisting && m_spawnedFloors.Contains(floor.FloorNumber))
            {
                Debug.Log($"[EnemySpawnService] {floor.FloorNumber}층은 이미 스폰되어 있어 생략합니다. (ClearExisting: {clearExisting})");
                return;
            }

            Debug.Log($"[EnemySpawnService] {floor.FloorNumber}층 적 스폰 시작 (ClearExisting: {clearExisting}, Parent: {parentTransform?.name})");

            Transform targetParent = parentTransform != null ? parentTransform : m_parentTransform;
            
            if (clearExisting)
            {
                ClearEnemies();
            }

            m_onEnemyDeath = onEnemyDeath;

            if (floor.HasBoss())
            {
                await SpawnBossFloorAsync(floor, targetParent, clearExisting);
            }
            else
            {
                await SpawnNormalFloorAsync(floor, targetParent, clearExisting);
            }

            // [추가]: 스폰 완료 상태 기록
            m_spawnedFloors.Add(floor.FloorNumber);
            Debug.Log($"[EnemySpawnService] {floor.FloorNumber}층 적 스폰 완료 (Total: {floor.GetTotalEnemyCount()})");

            SyncAllAnimations();
        }

        /// <summary>
        /// [설명]: 현재 활성화된 모든 적을 제거합니다.
        /// </summary>
        public void ClearEnemies()
        {
            ClearEnemyList(m_normalEnemies);
            m_normalPushControllers.Clear();
            ClearEnemyList(m_eliteEnemies);
            ClearEnemyList(m_bossEnemies);
            m_spawnedFloors.Clear(); // [추가]: 스폰 상태 초기화
        }
        #endregion

        #region 내부 로직
        private void SyncAllAnimations()
        {
            SyncEnemyListAnimations(m_normalEnemies);
            SyncEnemyListAnimations(m_eliteEnemies);
            SyncEnemyListAnimations(m_bossEnemies);
        }

        private void SyncEnemyListAnimations(List<GameObject> enemies)
        {
            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                var controller = enemy.GetComponent<IEnemyController>();
                if (controller != null) controller.SyncAnimation();
            }
        }

        private async UniTask SpawnBossFloorAsync(FloorData floor, Transform parent, bool clearExisting)
        {
            foreach (var spawnInfo in floor.Enemies)
            {
                if (spawnInfo.Enemy == null || spawnInfo.EnemyType != EnemyType.Boss) continue;
                if (spawnInfo.SpawnDelay > 0) await UniTask.Delay((int)(spawnInfo.SpawnDelay * 1000));

                for (int i = 0; i < spawnInfo.Count; i++)
                {
                    SpawnSingleEnemy(spawnInfo, EnemyType.Boss, parent);
                }
            }
        }

        private async UniTask SpawnNormalFloorAsync(FloorData floor, Transform parent, bool clearExisting)
        {
            var normalSpawns = new List<EnemySpawnInfo>();
            var eliteSpawns = new List<EnemySpawnInfo>();

            foreach (var spawnInfo in floor.Enemies)
            {
                if (spawnInfo.Enemy == null) continue;
                if (spawnInfo.EnemyType == EnemyType.Normal) normalSpawns.Add(spawnInfo);
                else if (spawnInfo.EnemyType == EnemyType.Elite) eliteSpawns.Add(spawnInfo);
            }

            // 엘리트 먼저 스폰
            foreach (var spawnInfo in eliteSpawns)
            {
                if (spawnInfo.SpawnDelay > 0) await UniTask.Delay((int)(spawnInfo.SpawnDelay * 1000));
                for (int i = 0; i < spawnInfo.Count; i++) SpawnSingleEnemy(spawnInfo, EnemyType.Elite, parent);
            }

            // 일반 몹 스폰 (기차 형태 지탱)
            int trainIndex = 0;
            // [수정]: 선스폰(clearExisting == false) 시에는 기존 푸시 컨트롤러 리스트를 유지하여 대열을 유지함
            if (clearExisting)
            {
                m_normalPushControllers.Clear();
            }
            
            foreach (var spawnInfo in normalSpawns)
            {
                if (spawnInfo.SpawnDelay > 0) await UniTask.Delay((int)(spawnInfo.SpawnDelay * 1000));
                for (int i = 0; i < spawnInfo.Count; i++)
                {
                    SpawnSingleEnemy(spawnInfo, EnemyType.Normal, parent, trainIndex);
                    trainIndex++;
                }
            }
        }

        private void SpawnSingleEnemy(EnemySpawnInfo spawnInfo, EnemyType type, Transform parent, int trainIndex = -1)
        {
            if (m_enemyPrefab == null) return;

            Vector3 spawnPosition = GetSpawnPosition(spawnInfo, type, parent, trainIndex);
            var enemy = GameObject.Instantiate(m_enemyPrefab, spawnPosition, Quaternion.identity, parent);
            enemy.name = $"{spawnInfo.Enemy.EnemyName}_{type}_{trainIndex}";

            var enemyController = enemy.GetComponent<IEnemyController>();
            if (enemyController != null)
            {
                enemyController.Initialize(spawnInfo.Enemy);
                enemyController.OnDeath += OnEnemyDeathCallback;
            }

            var pushController = enemy.GetComponent<EnemyPushController>();
            if (pushController != null)
            {
                pushController.SetPushSettings(m_config.MoveSpeed, m_config.PushRange);
                
                // [개선]: 개별 리스트 스냅샷 대신 공유 리스트(m_normalPushControllers)를 직접 전달
                List<EnemyPushController> trainControllers = (type == EnemyType.Normal) ? m_normalPushControllers : null;
                pushController.Initialize(spawnInfo.Enemy, trainControllers, type, trainIndex, m_config.TrainSpacing);
                
                // [추가]: 스폰 시에는 진격을 막아둠 (플레이어 도착 후 1초 뒤 허용)
                pushController.SetCanAdvance(false);
            }

            switch (type)
            {
                case EnemyType.Normal: m_normalEnemies.Add(enemy); break;
                case EnemyType.Elite: m_eliteEnemies.Add(enemy); break;
                case EnemyType.Boss: m_bossEnemies.Add(enemy); break;
            }
        }

        private Vector3 GetSpawnPosition(EnemySpawnInfo spawnInfo, EnemyType type, Transform parent, int trainIndex)
        {
            float offsetX = spawnInfo.PositionOffsetX;
            if (type == EnemyType.Normal && trainIndex >= 0)
            {
                offsetX += trainIndex * m_config.TrainSpacing;
            }

            Vector3 basePosition = Vector3.zero;
            if (m_spawnPoints != null && m_spawnPoints.Length > 0)
            {
                if (m_spawnPoints[0] != null) basePosition = m_spawnPoints[0].position;
            }
            else if (parent != null)
            {
                basePosition = parent.position;
            }

            // [수정]: 플랫폼 기반 Y 위치 + 공통 오프셋 + 개별 오프셋 적용
            float baseY = (parent != null) ? parent.position.y : basePosition.y;
            float finalY = baseY + m_config.SpawnYOffset + spawnInfo.PositionOffsetY;

            Debug.Log($"[EnemySpawnService] 스폰 위치 계산 - {spawnInfo.Enemy.EnemyName}: PlatformY={baseY}, ConfigY={m_config.SpawnYOffset}, InfoY={spawnInfo.PositionOffsetY} -> FinalY={finalY}");

            return new Vector3(basePosition.x + offsetX, finalY, basePosition.z);
        }

        private void OnEnemyDeathCallback(GameObject enemy)
        {
            if (enemy == null) return;

            var enemyController = enemy.GetComponent<IEnemyController>();
            if (enemyController != null) enemyController.OnDeath -= OnEnemyDeathCallback;

            // [추가]: 푸시 컨트롤러 리스트에서도 제거 (공유 리스트이므로 참조 유지 필요)
            var pushController = enemy.GetComponent<EnemyPushController>();
            if (pushController != null) m_normalPushControllers.Remove(pushController);

            m_normalEnemies.Remove(enemy);
            m_eliteEnemies.Remove(enemy);
            m_bossEnemies.Remove(enemy);

            m_onEnemyDeath?.Invoke(enemy);
            GameObject.Destroy(enemy, 1f);
        }

        private void ClearEnemyList(List<GameObject> enemies)
        {
            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                var enemyController = enemy.GetComponent<IEnemyController>();
                if (enemyController != null) enemyController.OnDeath -= OnEnemyDeathCallback;
                GameObject.Destroy(enemy);
            }
            enemies.Clear();
        }
        #endregion
    }

    /// <summary>
    /// [기능]: 적 컨트롤러 인터페이스
    /// </summary>
    public interface IEnemyController
    {
        void Initialize(EnemyData data);
        void TakeDamage(float damage);
        void SyncAnimation();
        void PlayAnimation(PlayerState state);
        event Action<GameObject> OnDeath;
    }
}
