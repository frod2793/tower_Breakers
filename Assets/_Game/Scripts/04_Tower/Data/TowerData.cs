using UnityEngine;
using System.Collections.Generic;

namespace TowerBreakers.Tower.Data
{
    /// <summary>
    /// [기능]: 타워 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "TowerData", menuName = "Data/Tower/Tower")]
    public class TowerData : ScriptableObject
    {
        [Header("타워 정보")]
        [Tooltip("타워 표시 이름")]
        [SerializeField] private string m_towerName;

        [Tooltip("타워 설명")]
        [SerializeField] [TextArea] private string m_description;

        [Header("층 구성")]
        [Tooltip("모든 층 데이터 리스트")]
        [SerializeField] private List<FloorData> m_floors;

        [Header("설정")]
        [Tooltip("기본 시작 층 번호")]
        [SerializeField] private int m_startFloor = 1;

        public string TowerName => m_towerName;
        public string Description => m_description;
        public List<FloorData> Floors => m_floors;
        public int StartFloor => m_startFloor;

        public int TotalFloors => m_floors != null ? m_floors.Count : 0;

        public FloorData GetFloor(int floorNumber)
        {
            if (m_floors == null || floorNumber < 1)
            {
                return null;
            }

            return m_floors.Find(f => f.FloorNumber == floorNumber);
        }

        public FloorData GetNextFloor(int currentFloor)
        {
            return GetFloor(currentFloor + 1);
        }

        public bool IsLastFloor(int currentFloor)
        {
            return currentFloor >= TotalFloors;
        }
    }
}
