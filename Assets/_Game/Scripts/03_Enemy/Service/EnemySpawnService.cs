using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using TowerBreakers.Tower.Data;

namespace TowerBreakers.Tower.Service
{
    /// <summary>
    /// [기능]: 적 생성 서비스
    /// </summary>
    public class EnemySpawnService
    {
        private readonly Transform[] m_spawnPoints;
        private readonly GameObject m_enemyPrefab;

        private Transform m_parentTransform;
        
        private readonly List<GameObject> m_normalEnemies = new List<GameObject>();
        private readonly List<GameObject> m_eliteEnemies = new List<GameObject>();
        private readonly List<GameObject> m_bossEnemies = new List<GameObject>();
        private Action<GameObject> m_onEnemyDeath;

        public int SpawnedEnemyCount => m_normalEnemies.Count + m_eliteEnemies.Count + m_bossEnemies.Count;

        public EnemySpawnService(GameObject enemyPrefab, Transform[] spawnPoints, Transform parentTransform)
        {
            m_enemyPrefab = enemyPrefab;
            m_spawnPoints = spawnPoints;
            m_parentTransform = parentTransform;
        }

        /// <summary>
        /// [설명]: 적이 생성될 부모 트랜스폼을 설정합니다. 
        /// 전투 중 층(플랫폼) 이동 시 호출되어 스폰 위치의 기준점을 변경합니다.
        /// </summary>
        /// <param name="parent">새로운 부모 트랜스폼</param>
        public void SetParent(Transform parent)
        {
            m_parentTransform = parent;
        }

        public void SetSpawnPoints(Transform[] spawnPoints)
        {
        }

        public void SetEnemyPrefab(GameObject prefab)
        {
        }

        public void SetOnEnemyDeathCallback(Action<GameObject> callback)
        {
            m_onEnemyDeath = callback;
        }

        public async UniTask SpawnEnemies(FloorData floor, Action<GameObject> onEnemyDeath, Transform parentTransform = null)
        {
            if (floor == null || floor.Enemies == null)
            {
                Debug.LogWarning("[EnemySpawnService] 층 데이터가 null이거나 적 목록이 없습니다.");
                return;
            }

            // [설명]: 명시적인 부모가 전달되지 않았다면 기본 부모(생성자 주입된 값)를 사용합니다.
            Transform targetParent = parentTransform != null ? parentTransform : m_parentTransform;

            ClearEnemies();

            m_onEnemyDeath = onEnemyDeath;

            bool hasBoss = floor.HasBoss();
            int totalEnemyCount = floor.GetTotalEnemyCount();
            Debug.Log($"[EnemySpawnService] 총 {totalEnemyCount}마리 생성 예정 (보스 있음: {hasBoss})");

            if (hasBoss)
            {
                await SpawnBossFloorAsync(floor, targetParent);
            }
            else
            {
                await SpawnNormalFloorAsync(floor, targetParent);
            }

            // [설명]: 모든 적 생성이 완료된 직후 애니메이션 싱크를 맞춥니다.
            SyncAllAnimations();
        }

        /// <summary>
        /// [설명]: 현재 활성화된 모든 적의 애니메이션을 동시에 시작하도록 동기화합니다.
        /// </summary>
        private void SyncAllAnimations()
        {
            SyncEnemyListAnimations(m_normalEnemies);
            SyncEnemyListAnimations(m_eliteEnemies);
            SyncEnemyListAnimations(m_bossEnemies);
            
            Debug.Log("[EnemySpawnService] 모든 적 애니메이션 동기화 완료");
        }

        private void SyncEnemyListAnimations(List<GameObject> enemies)
        {
            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                
                var controller = enemy.GetComponent<IEnemyController>();
                if (controller != null)
                {
                    controller.SyncAnimation();
                }
            }
        }

        private async UniTask SpawnBossFloorAsync(FloorData floor, Transform parent)
        {
            foreach (var spawnInfo in floor.Enemies)
            {
                if (spawnInfo.Enemy == null || spawnInfo.EnemyType != EnemyType.Boss)
                {
                    continue;
                }

                if (spawnInfo.SpawnDelay > 0)
                {
                    await UniTask.Delay((int)(spawnInfo.SpawnDelay * 1000));
                }

                for (int i = 0; i < spawnInfo.Count; i++)
                {
                    SpawnSingleEnemy(spawnInfo, EnemyType.Boss, parent);
                }
            }
        }

        private async UniTask SpawnNormalFloorAsync(FloorData floor, Transform parent)
        {
            var normalSpawns = new List<EnemySpawnInfo>();
            var eliteSpawns = new List<EnemySpawnInfo>();

            foreach (var spawnInfo in floor.Enemies)
            {
                if (spawnInfo.Enemy == null)
                {
                    continue;
                }

                if (spawnInfo.EnemyType == EnemyType.Normal)
                {
                    normalSpawns.Add(spawnInfo);
                }
                else if (spawnInfo.EnemyType == EnemyType.Elite)
                {
                    eliteSpawns.Add(spawnInfo);
                }
            }

            foreach (var spawnInfo in eliteSpawns)
            {
                if (spawnInfo.SpawnDelay > 0)
                {
                    await UniTask.Delay((int)(spawnInfo.SpawnDelay * 1000));
                }

                for (int i = 0; i < spawnInfo.Count; i++)
                {
                    SpawnSingleEnemy(spawnInfo, EnemyType.Elite, parent);
                }
            }

            int trainIndex = 0;
            foreach (var spawnInfo in normalSpawns)
            {
                if (spawnInfo.SpawnDelay > 0)
                {
                    await UniTask.Delay((int)(spawnInfo.SpawnDelay * 1000));
                }

                for (int i = 0; i < spawnInfo.Count; i++)
                {
                    SpawnSingleEnemy(spawnInfo, EnemyType.Normal, parent, trainIndex);
                    trainIndex++;
                }
            }
        }

        private void SpawnSingleEnemy(EnemySpawnInfo spawnInfo, EnemyType type, Transform parent, int trainIndex = -1)
        {
            if (m_enemyPrefab == null)
            {
                Debug.LogWarning("[EnemySpawnService] 적 프리팹이 설정되지 않았습니다.");
                return;
            }

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
                List<EnemyPushController> trainControllers = null;
                if (type == EnemyType.Normal)
                {
                    trainControllers = GetNormalPushControllers();
                }
                pushController.Initialize(spawnInfo.Enemy, trainControllers, type, trainIndex, spawnInfo.TrainSpacing);
            }

            switch (type)
            {
                case EnemyType.Normal:
                    m_normalEnemies.Add(enemy);
                    break;
                case EnemyType.Elite:
                    m_eliteEnemies.Add(enemy);
                    break;
                case EnemyType.Boss:
                    m_bossEnemies.Add(enemy);
                    break;
            }

            Debug.Log($"[EnemySpawnService] 적 생성: {type} - {spawnInfo.Enemy.EnemyName}");
        }

        private Vector3 GetSpawnPosition(EnemySpawnInfo spawnInfo, EnemyType type, Transform parent, int trainIndex)
        {
            // [설명]: 군집 내에서의 상대적 오프셋 계산 (오른쪽으로 스폰)
            float offsetX = spawnInfo.PositionOffsetX;
            if (type == EnemyType.Normal && trainIndex >= 0)
            {
                offsetX += trainIndex * spawnInfo.TrainSpacing;
            }

            Vector3 basePosition = Vector3.zero;

            // 1. 스폰 포인트에서 기준점(군집의 가장 왼쪽) 가져오기
            if (m_spawnPoints != null && m_spawnPoints.Length > 0)
            {
                // 여러 포인트 중 하나를 선택 (여기서는 첫 번째 포인트를 기본으로 사용)
                var point = m_spawnPoints[0];
                if (point != null)
                {
                    basePosition = point.position;
                }
            }
            else if (parent != null)
            {
                // 스폰 포인트가 없을 경우 플랫폼 중앙을 기준으로 함
                basePosition = parent.position;
            }

            // 2. Y축 보정 (플랫폼이 있다면 플랫폼 높이에 맞춤)
            float finalY = (parent != null) ? parent.position.y : basePosition.y;

            // 3. 최종 위치: 기준점(가장 왼쪽 적) + X 오프셋
            return new Vector3(basePosition.x + offsetX, finalY, basePosition.z);
        }

        private List<EnemyPushController> GetNormalPushControllers()
        {
            var controllers = new List<EnemyPushController>();
            foreach (var enemy in m_normalEnemies)
            {
                if (enemy != null)
                {
                    var controller = enemy.GetComponent<EnemyPushController>();
                    if (controller != null)
                    {
                        controllers.Add(controller);
                    }
                }
            }
            return controllers;
        }

        private void OnEnemyDeathCallback(GameObject enemy)
        {
            if (enemy == null)
            {
                return;
            }

            var enemyController = enemy.GetComponent<IEnemyController>();
            if (enemyController != null)
            {
                enemyController.OnDeath -= OnEnemyDeathCallback;
            }

            m_normalEnemies.Remove(enemy);
            m_eliteEnemies.Remove(enemy);
            m_bossEnemies.Remove(enemy);

            m_onEnemyDeath?.Invoke(enemy);

            GameObject.Destroy(enemy, 1f);
        }

        public void ClearEnemies()
        {
            ClearEnemyList(m_normalEnemies);
            ClearEnemyList(m_eliteEnemies);
            ClearEnemyList(m_bossEnemies);
            Debug.Log("[EnemySpawnService] 모든 적 제거 완료");
        }

        private void ClearEnemyList(List<GameObject> enemies)
        {
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    var enemyController = enemy.GetComponent<IEnemyController>();
                    if (enemyController != null)
                    {
                        enemyController.OnDeath -= OnEnemyDeathCallback;
                    }

                    GameObject.Destroy(enemy);
                }
            }
            enemies.Clear();
        }
    }

    /// <summary>
    /// [기능]: 적 컨트롤러 인터페이스
    /// </summary>
    public interface IEnemyController
    {
        void Initialize(EnemyData data);
        void TakeDamage(float damage);
        void SyncAnimation();
        event Action<GameObject> OnDeath;
    }
}
