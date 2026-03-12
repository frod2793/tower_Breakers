using UnityEngine;
using TowerBreakers.Enemy.Factory;
using TowerBreakers.Enemy.Data;
using TowerBreakers.Tower.Data;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 게임 내 적의 스폰 타이밍과 수량을 제어하는 클래스입니다.
    /// </summary>
    public class EnemySpawner
    {
        #region 내부 필드
        private readonly EnemyFactory m_factory;
        private CancellationTokenSource m_cts;
        #endregion

        public EnemySpawner(EnemyFactory factory)
        {
            m_factory = factory;
        }

        #region 공개 메서드
        /// <summary>
        /// [설명]: 층 데이터를 기반으로 여러 그룹의 적을 순차적으로 스폰합니다.
        /// </summary>
        public async UniTask SpawnFloorEnemiesAsync(FloorData floorData, Vector2 basePos)
        {
            m_cts?.Cancel();
            m_cts = new CancellationTokenSource();

            // 첫 번째 적을 스폰 위치(가장 맵 기준 좌측/혹은 첫 타겟위치)에 두고,
            // 후속 적들은 그보다 화면 오른쪽(Vector2.right 분량 +)에 대기하도록 오프셋 조절
            float totalOffset = 0f;
            Logic.EnemyPushLogic previousEnemy = null; // 대열 유지를 위한 이전 생성 적 추적

            // 0. 즉시 스폰(Pre-spawn) 모드 처리
            if (floorData.PreSpawnEnemies)
            {
                if (floorData.SpawnPackets != null && floorData.SpawnPackets.Count > 0)
                {
                    foreach (var packet in floorData.SpawnPackets)
                    {
                        if (packet.EnemyPrefabData == null) continue;
                        for (int i = 0; i < packet.EnemyCount; i++)
                        {
                            Vector2 spawnPos = basePos + Vector2.right * totalOffset;
                            var view = m_factory.Create(packet.EnemyPrefabData, spawnPos);
                            
                            var logic = view.GetComponent<Logic.EnemyPushLogic>();
                            if (logic != null)
                            {
                                logic.TrainSpacing = floorData.TrainSpacing;
                                logic.SetAheadEnemy(previousEnemy);
                                previousEnemy = logic;
                            }
                            totalOffset += floorData.TrainSpacing;
                        }
                        totalOffset += 2.0f; // 패킷(웨이브) 간 추가 간격
                    }
                }
                else if (floorData.EnemyPrefabData != null && floorData.EnemyCount > 0)
                {
                    for (int i = 0; i < floorData.EnemyCount; i++)
                    {
                        Vector2 spawnPos = basePos + Vector2.right * totalOffset;
                        var view = m_factory.Create(floorData.EnemyPrefabData, spawnPos);

                        var logic = view.GetComponent<Logic.EnemyPushLogic>();
                        if (logic != null)
                        {
                            logic.TrainSpacing = floorData.TrainSpacing;
                            logic.SetAheadEnemy(previousEnemy);
                            previousEnemy = logic;
                        }
                        totalOffset += floorData.TrainSpacing;
                    }
                }
                return; // 즉시 생성 후 종료
            }

            // 1. 기존 순차 스폰 로직 (PreSpawnEnemies가 false일 때만 실행)
            if (floorData.SpawnPackets != null && floorData.SpawnPackets.Count > 0)
            {
                foreach (var packet in floorData.SpawnPackets)
                {
                    if (packet.EnemyPrefabData == null) continue;

                    for (int i = 0; i < packet.EnemyCount; i++)
                    {
                        Vector2 spawnPos = basePos + Vector2.right * totalOffset;
                        var view = m_factory.Create(packet.EnemyPrefabData, spawnPos);

                        var logic = view.GetComponent<Logic.EnemyPushLogic>();
                        if (logic != null)
                        {
                            logic.TrainSpacing = floorData.TrainSpacing;
                            logic.SetAheadEnemy(previousEnemy);
                            previousEnemy = logic;
                        }

                        if (i < packet.EnemyCount - 1)
                        {
                            await UniTask.Delay((int)(packet.SpawnInterval * 1000), cancellationToken: m_cts.Token);
                        }
                        totalOffset += floorData.TrainSpacing;
                    }
                    
                    // 패킷 사이의 간격을 위해 오프셋 누적
                    totalOffset += 2.0f;
                }
            }
            // 2. 패킷이 없고 단순 설정이 있는 경우 처리
            else if (floorData.EnemyPrefabData != null && floorData.EnemyCount > 0)
            {
                for (int i = 0; i < floorData.EnemyCount; i++)
                {
                    Vector2 spawnPos = basePos + Vector2.right * totalOffset;
                    var view = m_factory.Create(floorData.EnemyPrefabData, spawnPos);

                    var logic = view.GetComponent<Logic.EnemyPushLogic>();
                    if (logic != null)
                    {
                        logic.TrainSpacing = floorData.TrainSpacing;
                        logic.SetAheadEnemy(previousEnemy);
                        previousEnemy = logic;
                    }

                    if (i < floorData.EnemyCount - 1)
                    {
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
