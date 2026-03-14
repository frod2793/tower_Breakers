using UnityEngine;
using TowerBreakers.Enemy.Factory;
using TowerBreakers.Enemy.Data;
using TowerBreakers.Tower.Data;
using Cysharp.Threading.Tasks;
using System.Threading;
using TowerBreakers.Tower.Logic;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 게임 내 적의 스폰 타이밍과 수량을 제어하는 클래스입니다.
    /// </summary>
    public class EnemySpawner
    {
        #region 내부 필드
        private readonly EnemyFactory m_factory;
        private readonly TowerManager m_towerManager;
        private readonly Core.Events.IEventBus m_eventBus;
        private readonly ProjectileFactory m_projectileFactory;
        private CancellationTokenSource m_cts;
        private Transform m_enemyParent;

        // [최적화]: GC 할당 방지를 위한 스폰용 풀 리스트
        private readonly System.Collections.Generic.List<(EnemyData data, float interval)> m_spawnMixedPool = new();
        #endregion

        public EnemySpawner(EnemyFactory factory, TowerManager towerManager, Core.Events.IEventBus eventBus, ProjectileFactory projectileFactory)
        {
            m_factory = factory;
            m_towerManager = towerManager;
            m_eventBus = eventBus;
            m_projectileFactory = projectileFactory;
        }

        /// <summary>
        /// [설명]: 스폰되는 적들의 부모 트랜스폼을 설정합니다.
        /// </summary>
        public void SetEnemyParent(Transform parent)
        {
            m_enemyParent = parent;
        }

        #region 공개 메서드
        /// <summary>
        /// [설명]: 층 데이터를 기반으로 여러 그룹의 적을 순차적으로 스폰합니다.
        /// </summary>
        public async UniTask SpawnFloorEnemiesAsync(FloorData floorData, Vector2 basePos, int floorIndex, Transform parent = null, bool isPreSpawn = false)
        {
            m_cts?.Cancel();
            m_cts = new CancellationTokenSource();

            float totalOffset = 0f;
            Logic.EnemyPushLogic previousEnemy = null;

            // 스폰 시 사용할 부모 결정 (매개변수가 없으면 기본 m_enemyParent 사용)
            Transform actualParent = (parent != null) ? parent : m_enemyParent;

            // 0. 즉시 스폰 또는 패킷 순차 스폰 처리
            if (floorData.SpawnPackets != null && floorData.SpawnPackets.Count > 0)
            {
                // [신규]: 패킷이 2개 이상이면 무작위로 섞어서 스폰
                if (floorData.SpawnPackets.Count >= 2)
                {
                    m_spawnMixedPool.Clear();
                    foreach (var packet in floorData.SpawnPackets)
                    {
                        if (packet.EnemyPrefabData == null) continue;
                        for (int i = 0; i < packet.EnemyCount; i++)
                        {
                            m_spawnMixedPool.Add((packet.EnemyPrefabData, packet.SpawnInterval));
                        }
                    }

                    // Fisher-Yates Shuffle
                    for (int i = m_spawnMixedPool.Count - 1; i > 0; i--)
                    {
                        int rnd = Random.Range(0, i + 1);
                        var temp = m_spawnMixedPool[i];
                        m_spawnMixedPool[i] = m_spawnMixedPool[rnd];
                        m_spawnMixedPool[rnd] = temp;
                    }

                    // 섞인 리스트로 스폰 실행
                    for (int i = 0; i < m_spawnMixedPool.Count; i++)
                    {
                        var entry = m_spawnMixedPool[i];
                        Vector2 spawnPos = basePos + Vector2.right * totalOffset;
                        var view = m_factory.Create(entry.data, spawnPos, floorIndex, actualParent);
                        
                        // [최적화]: GetComponent 호출 최소화
                        var logic = view.GetComponent<Logic.EnemyPushLogic>();
                        var controller = view.GetComponent<Logic.EnemyController>();

                        m_towerManager.RegisterEnemies(floorIndex, entry.data.Type);

                        if (logic != null)
                        {
                            logic.TrainSpacing = floorData.TrainSpacing;
                            logic.SetAheadEnemy(previousEnemy);
                            previousEnemy = logic;
                        }

                        if (controller != null && isPreSpawn)
                        {
                            controller.InitializeAsWaiting(entry.data, view, logic, m_eventBus, floorIndex, m_towerManager, m_projectileFactory);
                        }

                        if (i < m_spawnMixedPool.Count - 1)
                        {
                            if (isPreSpawn)
                                await UniTask.Yield(m_cts.Token);
                            else
                                await UniTask.Delay((int)(entry.interval * 1000), cancellationToken: m_cts.Token);
                        }
                        totalOffset += floorData.TrainSpacing;
                    }
                }
                else
                {
                    // 기존 순차 스폰 로직 (패킷 1개일 때)
                    foreach (var packet in floorData.SpawnPackets)
                    {
                        if (packet.EnemyPrefabData == null) continue;
                        for (int i = 0; i < packet.EnemyCount; i++)
                        {
                            Vector2 spawnPos = basePos + Vector2.right * totalOffset;
                            var view = m_factory.Create(packet.EnemyPrefabData, spawnPos, floorIndex, actualParent);
                            
                            // [최적화]: GetComponent 호출 최소화 (향후 Factory에서 주입 권장)
                            var logic = view.GetComponent<Logic.EnemyPushLogic>();
                            var controller = view.GetComponent<Logic.EnemyController>();

                            m_towerManager.RegisterEnemies(floorIndex, packet.EnemyPrefabData.Type);

                            if (logic != null)
                            {
                                logic.TrainSpacing = floorData.TrainSpacing;
                                logic.SetAheadEnemy(previousEnemy);
                                previousEnemy = logic;
                            }

                            if (controller != null && isPreSpawn)
                            {
                                controller.InitializeAsWaiting(packet.EnemyPrefabData, view, logic, m_eventBus, floorIndex, m_towerManager, m_projectileFactory);
                            }

                            if (i < packet.EnemyCount - 1)
                            {
                                if (isPreSpawn)
                                    await UniTask.Yield(m_cts.Token);
                                else
                                    await UniTask.Delay((int)(packet.SpawnInterval * 1000), cancellationToken: m_cts.Token);
                            }
                            totalOffset += floorData.TrainSpacing;
                        }
                        totalOffset += 2.0f; // 패킷 간의 추가 여유분 (순차 스폰 시에만 적용)
                    }
                }
            }
            else if (floorData.EnemyPrefabData != null && floorData.EnemyCount > 0)
            {
                for (int i = 0; i < floorData.EnemyCount; i++)
                {
                    Vector2 spawnPos = basePos + Vector2.right * totalOffset;
                    var view = m_factory.Create(floorData.EnemyPrefabData, spawnPos, floorIndex, actualParent);

                    // [최적화]: GetComponent 호출 최소화 (향후 Factory에서 주입 권장)
                    var logic = view.GetComponent<Logic.EnemyPushLogic>();
                    var controller = view.GetComponent<Logic.EnemyController>();

                    m_towerManager.RegisterEnemies(floorIndex, floorData.EnemyPrefabData.Type);

                    if (logic != null)
                    {
                        logic.TrainSpacing = floorData.TrainSpacing;
                        logic.SetAheadEnemy(previousEnemy);
                        previousEnemy = logic;
                    }

                    if (controller != null && isPreSpawn)
                    {
                        controller.InitializeAsWaiting(floorData.EnemyPrefabData, view, logic, m_eventBus, floorIndex, m_towerManager, m_projectileFactory);
                    }

                    if (i < floorData.EnemyCount - 1)
                    {
                        if (isPreSpawn)
                            await UniTask.Yield(m_cts.Token);
                        else
                            await UniTask.Delay((int)(floorData.SpawnInterval * 1000), cancellationToken: m_cts.Token);
                    }
                    totalOffset += floorData.TrainSpacing;
                }
            }
        }

        public void StopSpawning()
        {
            m_cts?.Cancel();
        }
        #endregion
    }
}
