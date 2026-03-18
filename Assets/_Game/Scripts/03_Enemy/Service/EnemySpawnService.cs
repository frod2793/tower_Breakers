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
    public class EnemySpawnService
    {
        #region 내부 필드
        private readonly Transform[] m_spawnPoints;
        private readonly GameObject m_enemyPrefab;
        private readonly EnemyConfigDTO m_config;
        private Transform m_parentTransform;
        
        private readonly List<GameObject> m_normalEnemies = new List<GameObject>();
        private readonly List<GameObject> m_eliteEnemies = new List<GameObject>();
        private readonly List<GameObject> m_bossEnemies = new List<GameObject>();
        private Action<GameObject> m_onEnemyDeath;
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
        public async UniTask SpawnEnemies(FloorData floor, Action<GameObject> onEnemyDeath, Transform parentTransform = null)
        {
            if (floor == null || floor.Enemies == null) return;

            Transform targetParent = parentTransform != null ? parentTransform : m_parentTransform;
            ClearEnemies();
            m_onEnemyDeath = onEnemyDeath;

            if (floor.HasBoss())
            {
                await SpawnBossFloorAsync(floor, targetParent);
            }
            else
            {
                await SpawnNormalFloorAsync(floor, targetParent);
            }

            SyncAllAnimations();
        }

        /// <summary>
        /// [설명]: 현재 활성화된 모든 적을 제거합니다.
        /// </summary>
        public void ClearEnemies()
        {
            ClearEnemyList(m_normalEnemies);
            ClearEnemyList(m_eliteEnemies);
            ClearEnemyList(m_bossEnemies);
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

        private async UniTask SpawnBossFloorAsync(FloorData floor, Transform parent)
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

        private async UniTask SpawnNormalFloorAsync(FloorData floor, Transform parent)
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
                pushController.SetPushSettings(m_config.MoveSpeed, m_config.PushForce, m_config.PushRange);
                
                List<EnemyPushController> trainControllers = (type == EnemyType.Normal) ? GetNormalPushControllers() : null;
                pushController.Initialize(spawnInfo.Enemy, trainControllers, type, trainIndex, m_config.TrainSpacing);
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

            float baseY = (parent != null) ? parent.position.y : basePosition.y;
            float finalY = baseY + m_config.SpawnYOffset;

            return new Vector3(basePosition.x + offsetX, finalY, basePosition.z);
        }

        private List<EnemyPushController> GetNormalPushControllers()
        {
            var controllers = new List<EnemyPushController>();
            foreach (var enemy in m_normalEnemies)
            {
                if (enemy == null) continue;
                var controller = enemy.GetComponent<EnemyPushController>();
                if (controller != null) controllers.Add(controller);
            }
            return controllers;
        }

        private void OnEnemyDeathCallback(GameObject enemy)
        {
            if (enemy == null) return;

            var enemyController = enemy.GetComponent<IEnemyController>();
            if (enemyController != null) enemyController.OnDeath -= OnEnemyDeathCallback;

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
