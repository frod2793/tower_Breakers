using UnityEngine;
using System.Collections.Generic;

namespace TowerBreakers.Tower.Data
{
    /// <summary>
    /// [기능]: 층별 적 생성 정보
    /// </summary>
    [System.Serializable]
    public class EnemySpawnInfo
    {
        [Tooltip("적 타입")]
        [SerializeField] private EnemyType m_enemyType = EnemyType.Normal;

        [Tooltip("적 데이터")]
        [SerializeField] private EnemyData m_enemy;

        [Tooltip("생성 수량")]
        [SerializeField] private int m_count = 1;

        [Tooltip("생성 지연 시간 (초)")]
        [SerializeField] private float m_spawnDelay = 0f;

        [Tooltip("생성 위치 오프셋 (X)")]
        [SerializeField] private float m_positionOffsetX;

        [Tooltip("기차 행렬에서의 순서 (일반 몹만 사용)")]
        [SerializeField] private int m_trainIndex;

        [Tooltip("행렬 간격 (일반 몹만 사용)")]
        [SerializeField] private float m_trainSpacing = 1.5f;

        public EnemyType EnemyType
        {
            get
            {
                if (m_enemyType == EnemyType.Normal && m_enemy != null)
                {
                    return (EnemyType)m_enemy.Grade;
                }
                return m_enemyType;
            }
        }
        public EnemyData Enemy => m_enemy;
        public int Count => m_count;
        public float SpawnDelay => m_spawnDelay;
        public float PositionOffsetX => m_positionOffsetX;
        public int TrainIndex => m_trainIndex;
        public float TrainSpacing => m_trainSpacing;
    }

    /// <summary>
    /// [기능]: 층 클리어 보상 데이터
    /// </summary>
    [System.Serializable]
    public class FloorRewardData
    {
        [Tooltip("보상 골드")]
        [SerializeField] private int m_gold;

        [Tooltip("보상 아이템 ID 리스트")]
        [SerializeField] private List<string> m_itemIds;

        [Tooltip("보상 경험치")]
        [SerializeField] private int m_experience;

        public int Gold => m_gold;
        public List<string> ItemIds => m_itemIds;
        public int Experience => m_experience;
    }

    /// <summary>
    /// [기능]: 층 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "Floor_", menuName = "Data/Tower/Floor")]
    public class FloorData : ScriptableObject
    {
        [Header("층 정보")]
        [Tooltip("층 번호")]
        [SerializeField] private int m_floorNumber = 1;

        [Tooltip("층 표시 이름")]
        [SerializeField] private string m_floorName;

        [Header("적 구성")]
        [Tooltip("적 생성 정보 리스트")]
        [SerializeField] private List<EnemySpawnInfo> m_enemies;

        [Header("보상")]
        [Tooltip("클리어 보상")]
        [SerializeField] private FloorRewardData m_clearReward;

        [Header("설정")]
        [Tooltip("층 클리어 후 자동 진행 여부")]
        [SerializeField] private bool m_autoProceed = true;

        public int FloorNumber => m_floorNumber;
        public string FloorName => m_floorName;
        public List<EnemySpawnInfo> Enemies => m_enemies;
        public FloorRewardData ClearReward => m_clearReward;
        public bool AutoProceed => m_autoProceed;

        public int GetTotalEnemyCount()
        {
            int total = 0;
            if (m_enemies != null)
            {
                bool hasBoss = false;
                foreach (var enemy in m_enemies)
                {
                    if (enemy.EnemyType == EnemyType.Boss)
                    {
                        hasBoss = true;
                        break;
                    }
                }

                if (hasBoss)
                {
                    foreach (var enemy in m_enemies)
                    {
                        if (enemy.EnemyType == EnemyType.Boss)
                        {
                            total += enemy.Count;
                        }
                    }
                }
                else
                {
                    foreach (var enemy in m_enemies)
                    {
                        if (enemy.EnemyType != EnemyType.Boss)
                        {
                            total += enemy.Count;
                        }
                    }
                }
            }
            return total;
        }

        public bool HasBoss()
        {
            if (m_enemies == null) return false;
            
            foreach (var enemy in m_enemies)
            {
                if (enemy.EnemyType == EnemyType.Boss)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
