using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using TowerBreakers.Player.Logic;
using VContainer;

namespace TowerBreakers.Enemy.Service
{
    /// <summary>
    /// [설명]: 적과의 거리를 멀티 스레드로 실시간 계산하는 서비스입니다.
    /// 스폰 위치가 아닌 현재 트랜스폼 좌표를 추적하여 가장 가까운 적을 찾습니다.
    /// </summary>
    public class EnemyDetectionService : IEnemyDetectionService
    {
        #region 내부 필드
        private IReadOnlyList<GameObject> m_normalEnemies;
        private IReadOnlyList<GameObject> m_eliteEnemies;
        private IReadOnlyList<GameObject> m_bossEnemies;
        private GameObject m_rewardChest;
        
        // [추가]: GC 0 원칙을 위한 캐싱 리스트 (Zero Allocation)
        private readonly List<GameObject> m_cachedActiveEnemies = new List<GameObject>(100);
        private readonly List<GameObject> m_cachedActiveNormal = new List<GameObject>(100);
        private readonly List<GameObject> m_cachedActiveElite = new List<GameObject>(50);

        private struct JobData
        {
            public NativeArray<float> EnemyXPositions;
            public NativeArray<float> MinXResults;
            public NativeArray<int> MinIndexResults;
            public JobHandle Handle;

            public void Dispose()
            {
                if (EnemyXPositions.IsCreated) EnemyXPositions.Dispose();
                if (MinXResults.IsCreated) MinXResults.Dispose();
                if (MinIndexResults.IsCreated) MinIndexResults.Dispose();
            }
        }
        #endregion

        #region 초기화 및 바인딩 로직
        /// <summary>
        /// [설명]: 탐지할 적 리스트를 업데이트합니다. (GameController.Tick에서 호출)
        /// </summary>
        public void UpdateEnemyLists(IReadOnlyList<GameObject> normal, IReadOnlyList<GameObject> elite, IReadOnlyList<GameObject> boss)
        {
            m_normalEnemies = normal;
            m_eliteEnemies = elite;
            m_bossEnemies = boss;
        }

        public void SetRewardChest(GameObject chest)
        {
            m_rewardChest = chest;
        }
        #endregion

        #region 공개 메서드
        /// <summary>
        /// [설명]: 군집/엘리트/보스 구분 없이 모든 적 중 월드 좌표 기준 가장 왼쪽에 있는 적을 반환합니다.
        /// </summary>
        public GameObject GetFrontEnemy(Vector2 playerPosition)
        {
            float playerX = playerPosition.x;
            float playerY = playerPosition.y;

            // 1. 모든 타입의 적을 하나의 풀로 수집 (Zero Allocation 캐시 활용)
            m_cachedActiveEnemies.Clear();
            AddActiveFromList(m_normalEnemies, m_cachedActiveEnemies, playerY);
            AddActiveFromList(m_eliteEnemies, m_cachedActiveEnemies, playerY);
            AddActiveFromList(m_bossEnemies, m_cachedActiveEnemies, playerY);
            
            if (m_rewardChest != null && m_rewardChest.activeInHierarchy)
            {
                if (Mathf.Abs(m_rewardChest.transform.position.y - playerY) < 2.0f)
                {
                    m_cachedActiveEnemies.Add(m_rewardChest);
                }
            }

            int count = m_cachedActiveEnemies.Count;
            if (count == 0) return null;

            // 2. 통합 리스트에 대해 병렬 연산 수행
            JobData totalJob = PrepareJob(m_cachedActiveEnemies, playerX);
            totalJob.Handle.Complete();

            // 3. 글로벌 최소 거리(가장 가까운)를 가진 적 선택
            GameObject target = GetResultFromJob(m_cachedActiveEnemies, totalJob);

            // 4. 메모리 해제
            totalJob.Dispose();

            return target;
        }


        public float GetDistanceToFrontEnemy(Vector2 playerPosition)
        {
            GameObject front = GetFrontEnemy(playerPosition);
            if (front == null) return float.MaxValue;
            return front.transform.position.x - playerPosition.x;
        }

        public float GetDistanceToSwarm(Vector2 playerPosition)
        {
            if (m_normalEnemies == null || m_normalEnemies.Count == 0) return float.MaxValue;
            
            m_cachedActiveEnemies.Clear();
            AddActiveFromList(m_normalEnemies, m_cachedActiveEnemies, playerPosition.y);
            
            if (m_cachedActiveEnemies.Count == 0) return float.MaxValue;

            JobData data = PrepareJob(m_cachedActiveEnemies, playerPosition.x);
            data.Handle.Complete();
            
            GameObject result = GetResultFromJob(m_cachedActiveEnemies, data);
            float dist = (result != null) ? (result.transform.position.x - playerPosition.x) : float.MaxValue;
            data.Dispose();

            return dist;
        }

        #region 엘리트/보스 특화 탐색 (VContainer 사용 시 [Inject] 등 활용 가능)
        public float GetDistanceToElite(Vector2 playerPosition)
        {
            if (m_eliteEnemies == null || m_eliteEnemies.Count == 0) return float.MaxValue;

            m_cachedActiveEnemies.Clear();
            AddActiveFromList(m_eliteEnemies, m_cachedActiveEnemies, playerPosition.y);
            
            if (m_cachedActiveEnemies.Count == 0) return float.MaxValue;

            JobData data = PrepareJob(m_cachedActiveEnemies, playerPosition.x);
            data.Handle.Complete();
            
            GameObject result = GetResultFromJob(m_cachedActiveEnemies, data);
            float dist = (result != null) ? (result.transform.position.x - playerPosition.x) : float.MaxValue;
            data.Dispose();

            return dist;
        }
        #endregion
        #endregion

        #region 내부 로직

        private void AddActiveFromList(IReadOnlyList<GameObject> source, List<GameObject> target, float playerY)
        {
            if (source == null) return;
            int count = source.Count;
            for (int i = 0; i < count; i++)
            {
                var e = source[i];
                if (e != null && e.activeInHierarchy)
                {
                    // [핵심 해결]: 같은 층(Y축 기준 2.0m 이내)에 있는 적만 탐지 대상에 포함
                    if (Mathf.Abs(e.transform.position.y - playerY) < 2.0f)
                    {
                        target.Add(e);
                    }
                }
            }
        }


        private JobData PrepareJob(List<GameObject> enemies, float playerX)
        {
            int count = enemies.Count;
            JobData data = new JobData();
            if (count == 0) return data;

            data.EnemyXPositions = new NativeArray<float>(count, Allocator.TempJob);
            data.MinXResults = new NativeArray<float>(count, Allocator.TempJob);
            data.MinIndexResults = new NativeArray<int>(count, Allocator.TempJob);

            for (int i = 0; i < count; i++)
            {
                data.EnemyXPositions[i] = enemies[i].transform.position.x;
            }

            var job = new EnemyDistanceJob
            {
                EnemyXPositions = data.EnemyXPositions,
                PlayerX = playerX,
                MinXResults = data.MinXResults,
                MinIndexResults = data.MinIndexResults
            };

            data.Handle = job.Schedule(count, 64);
            return data;
        }

        private GameObject GetResultFromJob(List<GameObject> enemies, JobData data)
        {
            int count = enemies.Count;
            if (count == 0 || !data.MinIndexResults.IsCreated) return null;

            float finalMinDistance = float.MaxValue;
            int finalIndex = -1;

            for (int i = 0; i < count; i++)
            {
                // [핵심 재설계]: 좌표가 아닌 '전방 거리'가 최소인 적을 선택
                // 1000f는 하단 시퀀스에서 제외된 적(후방)을 의미함
                float forwardDistance = data.MinXResults[i];
                if (forwardDistance < 999f && forwardDistance < finalMinDistance)
                {
                    finalMinDistance = forwardDistance;
                    finalIndex = data.MinIndexResults[i];
                }
            }

            return (finalIndex != -1) ? enemies[finalIndex] : null;
        }
        #endregion
    }
}
