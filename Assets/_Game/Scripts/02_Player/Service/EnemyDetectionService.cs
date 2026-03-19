using System.Collections.Generic;
using UnityEngine;
using TowerBreakers.Tower.Service;

namespace TowerBreakers.Player.Service
{
    /// <summary>
    /// [설명]: IEnemyProvider를 통해 적 리스트를 조회하고 거리 및 타겟팅 로직을 수행하는 구현체입니다.
    /// </summary>
    public class EnemyDetectionService : IEnemyDetectionService
    {
        #region 내부 필드
        private readonly IEnemyProvider m_enemyProvider;
        #endregion

        #region 초기화
        public EnemyDetectionService(IEnemyProvider enemyProvider)
        {
            m_enemyProvider = enemyProvider;
        }
        #endregion

        #region 공개 메서드
        /// <summary>
        /// [설명]: 플레이어 기준 최전방(가장 왼쪽/플레이어에게 가장 가까운 오른쪽) 적을 찾습니다.
        /// 사용자의 요청에 따라 '군집(Normal)' 적을 우선적으로 고려합니다.
        /// </summary>
        public GameObject GetFrontEnemy(Vector2 playerPosition)
        {
            GameObject frontEnemy = null;
            float minDistance = float.MaxValue;

            // 1. 일반 몹(군집) 먼저 체크
            frontEnemy = FindNearestFromList(playerPosition, m_enemyProvider.NormalEnemies, ref minDistance);

            // 2. 특수 몹(엘리트/보스) 체크 - 일반 몹보다 앞에 있다면 교체
            GameObject specialEnemy = FindNearestFromList(playerPosition, m_enemyProvider.EliteEnemies, ref minDistance);
            if (specialEnemy != null) frontEnemy = specialEnemy;

            specialEnemy = FindNearestFromList(playerPosition, m_enemyProvider.BossEnemies, ref minDistance);
            if (specialEnemy != null) frontEnemy = specialEnemy;

            return frontEnemy;
        }

        public float GetDistanceToFrontEnemy(Vector2 playerPosition)
        {
            var frontEnemy = GetFrontEnemy(playerPosition);
            if (frontEnemy == null) return float.MaxValue;

            // X축 거리만 계산 (오른쪽에서만 오므로)
            return Mathf.Abs(frontEnemy.transform.position.x - playerPosition.x);
        }

        public float GetDistanceToSpecialEnemy(Vector2 playerPosition)
        {
            float minDistance = float.MaxValue;

            FindNearestFromList(playerPosition, m_enemyProvider.EliteEnemies, ref minDistance);
            FindNearestFromList(playerPosition, m_enemyProvider.BossEnemies, ref minDistance);

            return minDistance;
        }
        #endregion

        #region 내부 로직
        private GameObject FindNearestFromList(Vector2 playerPosition, IReadOnlyList<GameObject> enemies, ref float minDistance)
        {
            GameObject nearest = null;
            
            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.activeInHierarchy) continue;

                float targetX = enemy.transform.position.x;
                // 플레이어보다 오른쪽에 있는 적만 대상
                if (targetX > playerPosition.x)
                {
                    float dist = targetX - playerPosition.x;
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nearest = enemy;
                    }
                }
            }

            return nearest;
        }
        #endregion
    }
}
