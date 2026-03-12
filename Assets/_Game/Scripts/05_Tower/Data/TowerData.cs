using UnityEngine;
using TowerBreakers.Enemy.Data;
using System.Collections.Generic;

namespace TowerBreakers.Tower.Data
{
    /// <summary>
    /// [설명]: 한 번에 스폰할 적의 묶음 데이터와 스폰 간격을 정의합니다.
    /// </summary>
    [System.Serializable]
    public class EnemySpawnPacket
    {
        [Tooltip("스폰할 적 데이터")]
        public EnemyData EnemyPrefabData;

        [Tooltip("스폰할 적의 수량")]
        public int EnemyCount = 5;

        [Tooltip("각 개체 간의 스폰 시간 간격")]
        public float SpawnInterval = 1.0f;
    }

    /// <summary>
    /// [설명]: 특정 층의 구성을 정의하는 데이터입니다.
    /// </summary>
    [System.Serializable]
    public class FloorData
    {
        [Header("단순 스폰 설정 (패킷 미사용 시)")]
        [Tooltip("스폰할 적 데이터")]
        public EnemyData EnemyPrefabData; 

        [Tooltip("스폰할 적의 수량")]
        public int EnemyCount = 5;

        [Tooltip("적들 사이의 소환 간격 (초)")]
        public float SpawnInterval = 1.0f;

        [Header("스폰 방식 설정")]
        [Tooltip("체크 시 게임 시작 전 적들을 미리 줄지어 배치합니다. (대기 시간 없음)")]
        public bool PreSpawnEnemies = true;

        [Tooltip("적 기차 대열 유지 간격 (기본 1.5)")]
        public float TrainSpacing = 1.5f;

        [Header("고급 스폰 설정 (여러 종류/웨이브)")]
        [Tooltip("순차적으로 소환될 적 패킷 목록 (입력 시 위 단순 설정은 무시됨)")]
        public List<EnemySpawnPacket> SpawnPackets = new List<EnemySpawnPacket>();
    }

    /// <summary>
    /// [설명]: 타워 전체의 설정을 저장하는 데이터 에셋입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewTowerData", menuName = "TowerBreakers/Tower Data")]
    public class TowerData : ScriptableObject
    {
        #region 에디터 설정
        [SerializeField, Tooltip("타워 이름")]
        private string m_towerName = "Standard Tower";

        [SerializeField, Tooltip("이 타워의 모든 층에서 공통으로 사용할 지면(Ground) 프리팹")]
        private GameObject m_defaultGroundPrefab;

        [SerializeField, Tooltip("층별 데이터 목록")]
        private List<FloorData> m_floors = new List<FloorData>();
        #endregion

        #region 프로퍼티
        public string TowerName => m_towerName;

        /// <summary>
        /// [설명]: 이 타워에서 공용으로 사용하는 지면 프리팹을 반환합니다.
        /// </summary>
        public GameObject GroundPrefab => m_defaultGroundPrefab;

        public IReadOnlyList<FloorData> Floors => m_floors;
        public int TotalFloors => m_floors.Count;
        #endregion
    }
}
