using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerBreakers.Tower.Service
{
    /// <summary>
    /// [기능]: 플랫폼 오브젝트 풀
    /// </summary>
    public class PlatformPool : MonoBehaviour
    {
        [Header("플랫폼 프리팹")]
        [SerializeField] private GameObject m_platformPrefab;

        [Header("설정")]
        [SerializeField] private int m_poolSize = 3;

        [SerializeField, Tooltip("[설명]: 층과 층 사이의 수직 간격입니다."), UnityEngine.Serialization.FormerlySerializedAs("m_floorHeight")]
        private float m_floorSpacing = 10f;

        #region 프로퍼티
        /// <summary>
        /// [설명]: 층간 간격 값입니다.
        /// </summary>
        public float FloorSpacing => m_floorSpacing;
        #endregion
        
        [Header("생성 설정")]
        [SerializeField, Tooltip("플랫폼이 생성될 부모 트랜스폼")]
        private Transform m_platformParent;

        private readonly Queue<GameObject> m_platformPool = new Queue<GameObject>();
        private readonly List<GameObject> m_activePlatforms = new List<GameObject>();

        public event Action<int> OnPlatformActivated;

        private void Awake()
        {
            InitializePool();
        }

        private void InitializePool()
        {
            if (m_platformPrefab == null)
            {
                Debug.LogWarning("[PlatformPool] 플랫폼 프리팹이 설정되지 않았습니다.");
                return;
            }

            for (int i = 0; i < m_poolSize; i++)
            {
                var platform = CreatePlatform(i);
                m_platformPool.Enqueue(platform);
            }

            Debug.Log($"[PlatformPool] 풀 초기화 완료 - 크기: {m_poolSize}");
        }

        private GameObject CreatePlatform(int index)
        {
            // [설명]: 설정된 부모(m_platformParent)가 있으면 해당 트랜스폼 하위에 생성하고, 없으면 컴포넌트 본인의 하트에 생성합니다.
            Transform parent = m_platformParent != null ? m_platformParent : transform;
            var platform = Instantiate(m_platformPrefab, parent);
            platform.name = $"Platform_{index}";
            platform.SetActive(false);
            return platform;
        }

        public GameObject GetPlatform(int floorNumber)
        {
            GameObject platform;

            if (m_platformPool.Count > 0)
            {
                platform = m_platformPool.Dequeue();
            }
            else
            {
                platform = CreatePlatform(m_activePlatforms.Count);
            }

            SetPlatformPosition(platform, floorNumber);
            platform.SetActive(true);
            m_activePlatforms.Add(platform);

            Debug.Log($"[PlatformPool] 플랫폼 활성화 - 층: {floorNumber}");
            
            OnPlatformActivated?.Invoke(floorNumber);
            
            return platform;
        }

        private void SetPlatformPosition(GameObject platform, int floorNumber)
        {
            // [설명]: 1층(floorNumber == 1)일 때 Y 좌표가 0이 되도록 (floorNumber - 1)을 곱합니다.
            float targetY = (floorNumber - 1) * m_floorSpacing;
            platform.transform.position = new Vector3(platform.transform.position.x, targetY, platform.transform.position.z);
        }

        public void ReturnPlatform(GameObject platform)
        {
            if (platform == null)
            {
                return;
            }

            platform.SetActive(false);
            m_activePlatforms.Remove(platform);
            m_platformPool.Enqueue(platform);

            Debug.Log("[PlatformPool] 플랫폼 반환 완료");
        }

        public void ReturnAllPlatforms()
        {
            foreach (var platform in m_activePlatforms)
            {
                if (platform != null)
                {
                    platform.SetActive(false);
                    m_platformPool.Enqueue(platform);
                }
            }

            m_activePlatforms.Clear();
            Debug.Log("[PlatformPool] 모든 플랫폼 반환 완료");
        }

        public void Clear()
        {
            ReturnAllPlatforms();

            foreach (var platform in m_platformPool)
            {
                if (platform != null)
                {
                    Destroy(platform);
                }
            }

            m_platformPool.Clear();
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}
