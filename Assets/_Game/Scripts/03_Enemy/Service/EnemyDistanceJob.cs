using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Burst;

namespace TowerBreakers.Enemy.Service
{
    /// <summary>
    /// [설명]: 적 군집의 위치 데이터를 병렬로 처리하여 플레이어와 가장 가까운(가장 왼쪽) 적의 인덱스를 찾는 Job입니다.
    /// Burst 컴파일러를 사용하여 최적화된 네이티브 코드로 실행됩니다.
    /// </summary>
    [BurstCompile]
    public struct EnemyDistanceJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float> EnemyXPositions;
        [ReadOnly] public float PlayerX;
        
        /// <summary>
        /// [설명]: 각 스레드에서 계산된 최소 X 좌표와 해당 인덱스를 저장합니다.
        /// </summary>
        public NativeArray<float> MinXResults;
        public NativeArray<int> MinIndexResults;

        /// <summary>
        /// [설명]: 각 적의 X 위치를 플레이어와 비교하여 **전방 실거리(Forward Distance)**를 계산합니다.
        /// 플레이어보다 뒤에 있는 적은 탐지에서 제외(최대값 부여)합니다.
        /// </summary>
        /// <param name="index">작업 인덱스</param>
        public void Execute(int index)
        {
            float enemyX = EnemyXPositions[index];
            float forwardDistance = enemyX - PlayerX;

            // [핵심 재설계]: 전방의 적만 유효한 거리로 취급 (좌표 기반이 아닌 전방 간격 기반)
            // 겹침 비허용 원칙에 따라, 플레이어보다 조금이라도 뒤에 있는 적은 제외합니다.
            if (forwardDistance < 0.0f)
            {
                MinXResults[index] = 1000f; // 탐지 대상 제외
            }
            else
            {
                MinXResults[index] = forwardDistance;
            }
            
            MinIndexResults[index] = index;
        }
    }
}
