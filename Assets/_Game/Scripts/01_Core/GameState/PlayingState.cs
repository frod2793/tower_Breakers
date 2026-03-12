using Cysharp.Threading.Tasks;
using UnityEngine;
using TowerBreakers.Enemy.Logic;
using TowerBreakers.Tower.Logic;
using TowerBreakers.Enemy.Data;

namespace TowerBreakers.Core.GameState
{
    /// <summary>
    /// [설명]: 게임 플레이 중인 상태입니다.
    /// </summary>
    public class PlayingState : IGameState
    {
        private readonly EnemySpawner m_enemySpawner;
        private readonly TowerManager m_towerManager;
        private readonly Environment.EnvironmentManager m_envManager;

        public PlayingState(EnemySpawner enemySpawner, TowerManager towerManager, Environment.EnvironmentManager envManager)
        {
            m_enemySpawner = enemySpawner;
            m_towerManager = towerManager;
            m_envManager = envManager;
        }

        public async UniTask OnEnter()
        {
            Debug.Log("[PlayingState] 진입: 게임 시작 및 적 스폰");
            
            // 현재 층 데이터 가져오기
            var currentFloor = m_towerManager.CurrentFloorData;
            if (currentFloor != null)
            {
                // 환경 매니저에서 현재 층의 정확한 스폰 위치(Y축 위치 반영)를 가져옴
                Vector2 spawnPos = m_envManager.GetCurrentSpawnPosition();
                
                // 적 스폰 시작
                m_enemySpawner.SpawnFloorEnemiesAsync(currentFloor, spawnPos).Forget();
                
                // TODO: 보스 층일 경우 특수 UI 및 배경음악 변경 트리거 추가
                bool hasBoss = false;
                foreach(var packet in currentFloor.SpawnPackets)
                {
                    if (packet.EnemyPrefabData != null && packet.EnemyPrefabData.Type == EnemyType.Boss)
                    {
                        hasBoss = true;
                        break;
                    }
                }

                if (hasBoss)
                {
                    Debug.Log("⚠️ 보스 등장! 긴장하세요!");
                }
            }
        }

        public UniTask OnExit()
        {
            Debug.Log("[PlayingState] 퇴출: 스폰 중단");
            m_enemySpawner.StopSpawning();
            return UniTask.CompletedTask;
        }

        public void OnUpdate()
        {
        }
    }
}
