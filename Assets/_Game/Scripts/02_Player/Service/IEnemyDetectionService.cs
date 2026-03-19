using UnityEngine;

namespace TowerBreakers.Player.Service
{
    /// <summary>
    /// [설명]: 플레이어의 위치를 기준으로 적과의 거리를 계산하고 최전방 적을 탐지하는 서비스 인터페이스입니다.
    /// </summary>
    public interface IEnemyDetectionService
    {
        /// <summary>
        /// [설명]: 플레이어의 현재 위치에서 가장 가까운(최전방) 적을 반환합니다.
        /// </summary>
        /// <param name="playerPosition">플레이어 위치</param>
        /// <returns>최전방 적 오브젝트</returns>
        GameObject GetFrontEnemy(Vector2 playerPosition);

        /// <summary>
        /// [설명]: 플레이어 위치와 최전방 적 사이의 거리를 계산합니다.
        /// </summary>
        /// <param name="playerPosition">플레이어 위치</param>
        /// <returns>거리 (적 상호작용 가능 여부 판정용)</returns>
        float GetDistanceToFrontEnemy(Vector2 playerPosition);

        /// <summary>
        /// [설명]: 엘리트 또는 보스 적과의 거리를 계산합니다.
        /// </summary>
        /// <param name="playerPosition">플레이어 위치</param>
        /// <returns>가장 가까운 엘리트/보스와의 거리</returns>
        float GetDistanceToSpecialEnemy(Vector2 playerPosition);
    }
}
