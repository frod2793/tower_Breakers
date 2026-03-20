using System.Collections.Generic;
using UnityEngine;

namespace TowerBreakers.Enemy.Service
{
    /// <summary>
    /// [설명]: 플레이어의 적 탐지 로직을 위한 인터페이스입니다.
    /// 멀티 스레드 거리 계산 및 타겟팅 로직을 추상화하며, Enemy 도메인에서 관리됩니다.
    /// </summary>
    public interface IEnemyDetectionService
    {
        /// <summary>
        /// [설명]: 탐지에 사용할 적 군집 리스트를 업데이트합니다.
        /// </summary>
        /// <param name="normal">일반 적 군집</param>
        /// <param name="elite">엘리트 적 리스트</param>
        /// <param name="boss">보스 적 리스트</param>
        void UpdateEnemyLists(IReadOnlyList<GameObject> normal, IReadOnlyList<GameObject> elite, IReadOnlyList<GameObject> boss);
        void SetRewardChest(GameObject chest);

        /// <summary>
        /// [설명]: 특정 좌표에서 같은 층(Y축 기준)에 있는 가장 가까운 적을 반환합니다.
        /// </summary>
        /// <param name="playerPosition">플레이어 현재 좌표</param>
        /// <returns>가장 가까운 적 오브젝트</returns>
        GameObject GetFrontEnemy(Vector2 playerPosition);

        /// <summary>
        /// [설명]: 같은 층의 최전방 적과의 X축 거리를 반환합니다.
        /// </summary>
        /// <param name="playerPosition">플레이어 현재 좌표</param>
        /// <returns>최소 X축 거리 값</returns>
        float GetDistanceToFrontEnemy(Vector2 playerPosition);

        /// <summary>
        /// [설명]: 같은 층의 일반 적 군집(Swarm) 중 가장 왼쪽 적과의 X축 거리를 반환합니다.
        /// </summary>
        /// <param name="playerPosition">플레이어 현재 좌표</param>
        /// <returns>군집 X축 최소 거리 값</returns>
        float GetDistanceToSwarm(Vector2 playerPosition);

        /// <summary>
        /// [설명]: 같은 층의 엘리트 적과의 X축 거리를 반환합니다.
        /// </summary>
        /// <param name="playerPosition">플레이어 현재 좌표</param>
        /// <returns>엘리트 X축 최소 거리 값</returns>
        float GetDistanceToElite(Vector2 playerPosition);
    }
}
